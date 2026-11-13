using Silk.NET.OpenGL;

namespace Visualizations.App.Rendering;

/// <summary>
/// Řada čísel (např. pásma spektra) jako textura N×1 ve formátu R32F – vstup pro shadery.
/// Vytvoření i <see cref="Upload"/> nechají texturu navázanou na právě aktivní texturovací jednotce.
/// </summary>
internal sealed unsafe class DataTexture : IDisposable
{
    private readonly GL _gl;

    public DataTexture(GL gl, int length)
    {
        _gl = gl;
        Length = length;
        Texture = gl.GenTexture();
        gl.BindTexture(TextureTarget.Texture2D, Texture);
        gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.R32f, (uint)length, 1, 0, PixelFormat.Red, PixelType.Float, null);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
    }

    public uint Texture { get; }

    public int Length { get; }

    public void Upload(ReadOnlySpan<float> data)
    {
        if (data.Length != Length)
        {
            throw new ArgumentException($"Očekáváno {Length} hodnot, přišlo {data.Length}.", nameof(data));
        }

        _gl.BindTexture(TextureTarget.Texture2D, Texture);
        fixed (float* pointer = data)
        {
            _gl.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, (uint)Length, 1, PixelFormat.Red, PixelType.Float, pointer);
        }
    }

    public void Dispose() => _gl.DeleteTexture(Texture);
}
