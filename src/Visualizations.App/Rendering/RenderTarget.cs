using Silk.NET.OpenGL;

namespace Visualizations.App.Rendering;

/// <summary>Framebuffer s jednou barevnou texturou – plátno, do kterého kreslí jedna vizualizace.</summary>
internal sealed unsafe class RenderTarget : IDisposable
{
    private readonly GL _gl;
    private readonly InternalFormat _format;

    public RenderTarget(GL gl, int width, int height, InternalFormat format = InternalFormat.Rgba8)
    {
        _gl = gl;
        _format = format;
        Framebuffer = gl.GenFramebuffer();
        Texture = gl.GenTexture();
        Resize(width, height);
    }

    public uint Framebuffer { get; }

    public uint Texture { get; }

    public int Width { get; private set; }

    public int Height { get; private set; }

    public void Resize(int width, int height)
    {
        width = Math.Max(width, 1);
        height = Math.Max(height, 1);
        if (width == Width && height == Height)
        {
            return;
        }

        Width = width;
        Height = height;

        _gl.BindTexture(TextureTarget.Texture2D, Texture);
        _gl.TexImage2D(TextureTarget.Texture2D, 0, _format, (uint)width, (uint)height, 0, PixelFormat.Rgba, PixelType.Float, null);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        _gl.BindTexture(TextureTarget.Texture2D, 0);

        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, Framebuffer);
        _gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, Texture, 0);
        var status = _gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        if (status != GLEnum.FramebufferComplete)
        {
            throw new InvalidOperationException($"Framebuffer není kompletní: {status}");
        }
    }

    /// <summary>Nastaví tento cíl pro kreslení včetně viewportu.</summary>
    public void Bind()
    {
        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, Framebuffer);
        _gl.Viewport(0, 0, (uint)Width, (uint)Height);
    }

    public void Dispose()
    {
        _gl.DeleteFramebuffer(Framebuffer);
        _gl.DeleteTexture(Texture);
    }
}
