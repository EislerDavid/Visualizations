using Silk.NET.OpenGL;
using Visualizations.App.Rendering;

namespace Visualizations.App.Effects;

/// <summary>
/// Animace se zpětnou vazbou – princip většiny efektů z WMP (Ambience, Battery, Fire Storm).
/// Každý snímek:
/// <list type="number">
/// <item><b>Warp</b> – shader spočítá nový obraz z předchozího (<c>uPrevious</c>): posun, otočení, přiblížení, útlum.</item>
/// <item><b>Inject</b> – do nového obrazu se přikreslí čerstvý obsah podle zvuku (čára, body…).</item>
/// <item><b>Present</b> – shader převede obsah zpětné vazby (<c>uFrame</c>) na výsledné barvy na plátně.</item>
/// </list>
/// Oba shadery dostávají <c>vUv</c> a společné vstupy z <see cref="AudioUniforms"/>.
/// </summary>
internal abstract class FeedbackVisualization : IVisualization
{
    /// <summary>Výchozí převod: jemná expozice, aby se nasčítané světlo nepřepalovalo do bílé.</summary>
    protected const string ExposurePresentShader = """
        #version 330 core
        in vec2 vUv;
        out vec4 fragColor;
        uniform sampler2D uFrame;
        void main()
        {
            vec3 c = texture(uFrame, vUv).rgb;
            fragColor = vec4(1.0 - exp(-c * 1.6), 1.0);
        }
        """;

    private ShaderProgram? _warp;
    private ShaderProgram? _present;
    private AudioUniforms? _audioUniforms;

    protected GL Gl { get; private set; } = null!;

    protected FullscreenPass Pass { get; private set; } = null!;

    protected FeedbackBuffer Feedback { get; private set; } = null!;

    /// <summary>Fragment shader kroku Warp (vstup <c>uniform sampler2D uPrevious</c>).</summary>
    protected abstract string WarpShader { get; }

    /// <summary>Fragment shader kroku Present (vstup <c>uniform sampler2D uFrame</c>).</summary>
    protected virtual string PresentShader => ExposurePresentShader;

    public virtual void Load(GL gl)
    {
        Gl = gl;
        Pass = new FullscreenPass(gl);
        Feedback = new FeedbackBuffer(gl, 1, 1);
        _warp = new ShaderProgram(gl, FullscreenPass.VertexShader, WarpShader);
        _present = new ShaderProgram(gl, FullscreenPass.VertexShader, PresentShader);
        _audioUniforms = new AudioUniforms(gl);
    }

    public virtual void Resize(int width, int height) => Feedback.Resize(width, height);

    public void Render(in FrameContext frame)
    {
        if (_warp is null || _present is null || _audioUniforms is null)
        {
            return;
        }

        Feedback.Swap();
        Feedback.Current.Bind();
        _warp.Use();
        _audioUniforms.Apply(_warp, frame);
        SetWarpUniforms(_warp, frame);
        BindTexture(Feedback.Previous.Texture);
        _warp.Set("uPrevious", 0);
        Pass.Draw();

        Inject(frame);

        frame.Target.Bind();
        _present.Use();
        _audioUniforms.Apply(_present, frame);
        SetPresentUniforms(_present, frame);
        BindTexture(Feedback.Current.Texture);
        _present.Set("uFrame", 0);
        Pass.Draw();
    }

    public virtual void Dispose()
    {
        _audioUniforms?.Dispose();
        _warp?.Dispose();
        _present?.Dispose();
        Feedback?.Dispose();
        Pass?.Dispose();
    }

    /// <summary>Uniformy kroku Warp specifické pro animaci (program je už aktivní).</summary>
    protected virtual void SetWarpUniforms(ShaderProgram warp, in FrameContext frame)
    {
    }

    /// <summary>Přikreslí čerstvý obsah do <see cref="FeedbackBuffer.Current"/> (je nastavený jako cíl).</summary>
    protected virtual void Inject(in FrameContext frame)
    {
    }

    /// <summary>Uniformy kroku Present specifické pro animaci (program je už aktivní).</summary>
    protected virtual void SetPresentUniforms(ShaderProgram present, in FrameContext frame)
    {
    }

    private void BindTexture(uint texture)
    {
        Gl.ActiveTexture(TextureUnit.Texture0);
        Gl.BindTexture(TextureTarget.Texture2D, texture);
    }
}
