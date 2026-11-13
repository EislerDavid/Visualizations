using Silk.NET.OpenGL;

namespace Visualizations.App.Rendering;

/// <summary>
/// Dvojice plátek pro zpětnou vazbu („ping-pong“): nový snímek se počítá z předchozího.
/// Textury jsou v 16bitové plovoucí čárce, aby pomalé doznívání nekončilo schody v barvách.
/// </summary>
internal sealed class FeedbackBuffer : IDisposable
{
    private readonly GL _gl;
    private RenderTarget _current;
    private RenderTarget _previous;

    public FeedbackBuffer(GL gl, int width, int height)
    {
        _gl = gl;
        _current = new RenderTarget(gl, width, height, InternalFormat.Rgba16f);
        _previous = new RenderTarget(gl, width, height, InternalFormat.Rgba16f);
        Clear();
    }

    /// <summary>Plátno, do kterého se kreslí aktuální snímek.</summary>
    public RenderTarget Current => _current;

    /// <summary>Výsledek předchozího snímku.</summary>
    public RenderTarget Previous => _previous;

    /// <summary>Z aktuálního snímku se stane předchozí; nový aktuální se pak celý přepíše.</summary>
    public void Swap() => (_current, _previous) = (_previous, _current);

    /// <summary>Změní velikost a smaže obsah (stará stopa by v jiném rozlišení nedávala smysl).</summary>
    public void Resize(int width, int height)
    {
        _current.Resize(width, height);
        _previous.Resize(width, height);
        Clear();
    }

    public void Clear()
    {
        foreach (var target in new[] { _current, _previous })
        {
            target.Bind();
            _gl.ClearColor(0f, 0f, 0f, 1f);
            _gl.Clear(ClearBufferMask.ColorBufferBit);
        }

        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }

    public void Dispose()
    {
        _current.Dispose();
        _previous.Dispose();
    }
}
