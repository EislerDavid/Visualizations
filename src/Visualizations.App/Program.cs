// Použití: Visualizations.App [--process <název>] [--system]
//   --process <název>  zachytávat zvuk dané aplikace (výchozí: Spotify)
//   --system           zachytávat celý výstup výchozího zvukového zařízení
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using Visualizations.App.Effects;
using Visualizations.Core.Analysis;
using Visualizations.Core.Audio;

Console.OutputEncoding = System.Text.Encoding.UTF8;

using var capture = StartCapture(ParseTarget(args));
Console.WriteLine($"Zachytávám: {capture.Description}, {capture.SampleRate} Hz");
capture.Stopped += error => Console.WriteLine(error is null
    ? "Zachytávání skončilo."
    : $"Zachytávání selhalo: {error.Message}");

var analyzer = new SpectrumAnalyzer(capture.SampleRate);
var samples = new float[analyzer.FftSize];
IVisualization visualization = new BarsVisualization();
GL? gl = null;

var options = WindowOptions.Default with
{
    Size = new Vector2D<int>(1024, 576),
    Title = $"Visualizations – {visualization.Name} – {capture.Description}",
    API = new GraphicsAPI(ContextAPI.OpenGL, ContextProfile.Core, ContextFlags.ForwardCompatible, new APIVersion(3, 3)),
    VSync = true,
};

using var window = Window.Create(options);

window.Load += () =>
{
    gl = GL.GetApi(window);
    visualization.Load(gl);
    visualization.Resize(window.FramebufferSize.X, window.FramebufferSize.Y);
};

window.FramebufferResize += size =>
{
    gl?.Viewport(size);
    visualization.Resize(size.X, size.Y);
};

window.Render += deltaTime =>
{
    if (gl is null)
    {
        return;
    }

    capture.ReadLatest(samples);
    analyzer.Process(samples, (float)deltaTime);

    gl.ClearColor(0f, 0f, 0f, 1f);
    gl.Clear(ClearBufferMask.ColorBufferBit);
    visualization.Render(analyzer, (float)deltaTime);
};

window.Closing += () =>
{
    visualization.Dispose();
    gl?.Dispose();
};

window.Run();

static CaptureTarget ParseTarget(string[] args)
{
    if (args.Contains("--system"))
    {
        return CaptureTarget.SystemOutput;
    }

    int index = Array.IndexOf(args, "--process");
    return index >= 0 && index + 1 < args.Length
        ? CaptureTarget.ForProcess(args[index + 1])
        : CaptureTarget.ForProcess("Spotify");
}

static AudioCapture StartCapture(CaptureTarget target)
{
    // Aktivace process loopbacku je asynchronní (dokončí se na vlákně COM), proto lze bezpečně čekat.
    try
    {
        return AudioCapture.StartAsync(target).GetAwaiter().GetResult();
    }
    catch (Exception e) when (target.ProcessName is not null)
    {
        Console.WriteLine($"Nelze zachytit „{target.ProcessName}“ ({e.Message}) – použiju celý výstup.");
        return AudioCapture.StartAsync(CaptureTarget.SystemOutput).GetAwaiter().GetResult();
    }
}
