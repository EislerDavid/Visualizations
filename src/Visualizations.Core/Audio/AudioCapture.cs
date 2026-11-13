using System.Runtime.InteropServices;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace Visualizations.Core.Audio;

/// <summary>
/// Zachytává přehrávaný zvuk přes WASAPI loopback (celý výstup, nebo jen jeden proces)
/// a průběžně ho ukládá jako mono float vzorky do kruhového bufferu.
/// </summary>
public sealed class AudioCapture : IDisposable
{
    // Sdílený režim WASAPI převede do tohoto formátu cokoli, co se přehrává.
    private static readonly WaveFormat RequestedFormat = WaveFormat.CreateIeeeFloatWaveFormat(48000, 2);

    // ~0,7 s historie při 48 kHz – víc, než potřebuje jakékoli FFT okno.
    private const int HistoryLength = 1 << 15;

    private readonly WasapiRecorder _recorder;
    private readonly SampleRingBuffer _samples = new(HistoryLength);
    private readonly int _channels;
    private float[] _mono = new float[4096];

    private AudioCapture(WasapiRecorder recorder, string description)
    {
        var format = recorder.WaveFormat;
        bool isFloat = format.Encoding == WaveFormatEncoding.IeeeFloat
            || (format is WaveFormatExtensible ext && ext.SubFormat == AudioMediaSubtypes.MEDIASUBTYPE_IEEE_FLOAT);
        if (!isFloat || format.BitsPerSample != 32)
        {
            throw new NotSupportedException($"Nepodporovaný formát zachytávání: {format}");
        }

        _recorder = recorder;
        _channels = format.Channels;
        SampleRate = format.SampleRate;
        Description = description;

        _recorder.DataAvailable += OnDataAvailable;
        _recorder.RecordingStopped += (_, e) => Stopped?.Invoke(e.Exception);
        _recorder.StartRecording();
    }

    public int SampleRate { get; }

    /// <summary>Lidsky čitelný popis zdroje, např. „Spotify (PID 15656)“.</summary>
    public string Description { get; }

    /// <summary>Zachytávání skončilo; parametr je chyba, nebo <c>null</c> při běžném ukončení.</summary>
    public event Action<Exception?>? Stopped;

    /// <summary>
    /// Spustí zachytávání. Pro zvuk konkrétní aplikace vyhodí <see cref="InvalidOperationException"/>,
    /// pokud aplikace neběží.
    /// </summary>
    public static async Task<AudioCapture> StartAsync(CaptureTarget target)
    {
        WasapiRecorder recorder;
        string description;

        if (target.ProcessName is { } processName)
        {
            int pid = ProcessTree.FindRootProcessId(processName)
                ?? throw new InvalidOperationException($"Aplikace „{processName}“ neběží.");
            recorder = await new WasapiRecorderBuilder()
                .WithProcessLoopback((uint)pid, ProcessLoopbackMode.IncludeTargetProcessTree)
                .WithFormat(RequestedFormat)
                .BuildAsync();
            description = $"{processName} (PID {pid})";
        }
        else
        {
            recorder = new WasapiRecorderBuilder()
                .WithLoopbackCapture()
                .WithFormat(RequestedFormat)
                .Build();
            description = $"celý výstup – {recorder.DeviceFriendlyName ?? "výchozí zařízení"}";
        }

        try
        {
            return new AudioCapture(recorder, description);
        }
        catch
        {
            recorder.Dispose();
            throw;
        }
    }

    /// <summary>Zkopíruje posledních <c>destination.Length</c> mono vzorků (nejstarší první).</summary>
    public void ReadLatest(Span<float> destination) => _samples.ReadLatest(destination);

    public void Dispose()
    {
        _recorder.DataAvailable -= OnDataAvailable;
        _recorder.Dispose();
    }

    // Běží na vlákně zachytávání; buffer platí jen po dobu volání.
    private void OnDataAvailable(ReadOnlySpan<byte> buffer, AudioClientBufferFlags flags, long devicePosition, long qpcPosition)
    {
        int frames = buffer.Length / (sizeof(float) * _channels);
        if (frames == 0)
        {
            return;
        }

        if (_mono.Length < frames)
        {
            _mono = new float[frames];
        }

        var mono = _mono.AsSpan(0, frames);
        if ((flags & AudioClientBufferFlags.Silent) != 0)
        {
            mono.Clear();
        }
        else
        {
            var interleaved = MemoryMarshal.Cast<byte, float>(buffer);
            float scale = 1f / _channels;
            for (int i = 0; i < frames; i++)
            {
                float sum = 0;
                for (int c = 0; c < _channels; c++)
                {
                    sum += interleaved[i * _channels + c];
                }

                mono[i] = sum * scale;
            }
        }

        _samples.Write(mono);
    }
}
