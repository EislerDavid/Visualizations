using Silk.NET.OpenGL;
using Visualizations.App.Rendering;

namespace Visualizations.App.Effects;

/// <summary>
/// Animace, která je celá ve fragment shaderu nakresleném přes celé plátno.
/// Shader dostává <c>vUv</c> (0–1) a společné vstupy z <see cref="AudioUniforms"/>.
/// </summary>
internal abstract class ShaderVisualization : IVisualization
{
    private GL? _gl;
    private FullscreenPass? _pass;
    private ShaderProgram? _program;
    private AudioUniforms? _audioUniforms;

    /// <summary>Zdroj fragment shaderu (GLSL 330 core, vstup <c>in vec2 vUv</c>, výstup <c>out vec4 fragColor</c>).</summary>
    protected abstract string FragmentShader { get; }

    public void Load(GL gl)
    {
        _gl = gl;
        _pass = new FullscreenPass(gl);
        _program = new ShaderProgram(gl, FullscreenPass.VertexShader, FragmentShader);
        _audioUniforms = new AudioUniforms(gl);
    }

    public void Resize(int width, int height)
    {
    }

    public void Render(in FrameContext frame)
    {
        if (_program is null || _pass is null || _audioUniforms is null)
        {
            return;
        }

        _program.Use();
        _audioUniforms.Apply(_program, frame);
        SetUniforms(_program, frame);
        _pass.Draw();
    }

    public void Dispose()
    {
        _audioUniforms?.Dispose();
        _program?.Dispose();
        _pass?.Dispose();
    }

    /// <summary>Nastaví uniformy specifické pro animaci (program je už aktivní).</summary>
    protected virtual void SetUniforms(ShaderProgram program, in FrameContext frame)
    {
    }
}
