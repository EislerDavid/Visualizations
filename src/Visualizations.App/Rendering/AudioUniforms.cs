using System.Numerics;
using Silk.NET.OpenGL;
using Visualizations.App.Effects;

namespace Visualizations.App.Rendering;

/// <summary>
/// Společné vstupy shaderů. Každý shader animace může používat:
/// <code>
/// uniform vec2  uResolution;  // velikost plátna v pixelech
/// uniform float uTime;        // čas v sekundách
/// uniform float uDeltaTime;   // délka snímku v sekundách
/// uniform float uBass, uMid, uTreble;  // úrovně pásem 0–1
/// uniform float uBeat;        // úder: 1 → 0 během ~0,2 s
/// uniform float uLevel;       // hlasitost (RMS)
/// uniform sampler2D uSpectrum; // pásma spektra (0–1) podél osy x, jednotka 1
/// </code>
/// </summary>
internal sealed class AudioUniforms : IDisposable
{
    public const int SpectrumTextureUnit = 1;

    private readonly GL _gl;
    private DataTexture? _spectrum;

    public AudioUniforms(GL gl) => _gl = gl;

    /// <summary>
    /// Nastaví vstupy do aktivního programu. Mění jen texturovací jednotku <see cref="SpectrumTextureUnit"/>,
    /// jednotka 0 zůstane, jak byla (bez toho by nahrání spektra přepsalo texturu předchozího snímku).
    /// </summary>
    public void Apply(ShaderProgram program, in FrameContext frame)
    {
        var audio = frame.Audio;
        _gl.ActiveTexture(TextureUnit.Texture0 + SpectrumTextureUnit);
        if (_spectrum is null || _spectrum.Length != audio.Bands.Length)
        {
            _spectrum?.Dispose();
            _spectrum = new DataTexture(_gl, audio.Bands.Length);
        }

        _spectrum.Upload(audio.Bands);
        _gl.ActiveTexture(TextureUnit.Texture0);

        program.Set("uSpectrum", SpectrumTextureUnit);
        program.Set("uResolution", new Vector2(frame.Target.Width, frame.Target.Height));
        program.Set("uTime", (float)frame.Time);
        program.Set("uDeltaTime", frame.DeltaTime);
        program.Set("uBass", audio.Bass);
        program.Set("uMid", audio.Mid);
        program.Set("uTreble", audio.Treble);
        program.Set("uBeat", audio.Beat);
        program.Set("uLevel", audio.Level);
    }

    public void Dispose() => _spectrum?.Dispose();
}
