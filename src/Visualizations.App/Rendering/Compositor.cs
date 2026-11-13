using Silk.NET.OpenGL;
using Visualizations.App.Mixing;

namespace Visualizations.App.Rendering;

/// <summary>Složí dvě textury do aktuálně nastaveného cíle podle crossfaderu a režimu míchání.</summary>
internal sealed class Compositor : IDisposable
{
    private const string FragmentShader = """
        #version 330 core
        in vec2 vUv;
        out vec4 fragColor;
        uniform sampler2D uA;
        uniform sampler2D uB;
        uniform float uMix;
        uniform int uMode;
        uniform float uAspect;
        uniform float uTime;
        void main()
        {
            vec3 a = texture(uA, vUv).rgb;
            vec3 b = texture(uB, vUv).rgb;
            // DJ křivka: do poloviny crossfaderu zůstává druhý kanál naplno.
            float wa = clamp(2.0 * (1.0 - uMix), 0.0, 1.0);
            float wb = clamp(2.0 * uMix, 0.0, 1.0);
            vec2 p = (vUv - 0.5) * vec2(uAspect, 1.0);
            vec3 c;
            if (uMode == 0)      c = mix(a, b, uMix);
            else if (uMode == 1) c = min(a * wa + b * wb, vec3(1.0));
            else if (uMode == 2) c = 1.0 - (1.0 - a * wa) * (1.0 - b * wb);
            else if (uMode == 3) c = max(a * wa, b * wb);
            else if (uMode == 4) c = abs(a * wa - b * wb);
            else if (uMode == 5)
            {
                // Kruh: B se rozpíná ze středu (0 = střed, 1 = roh obrazu), měkký okraj.
                float d = length(p) / length(vec2(uAspect, 1.0) * 0.5);
                float radius = uMix * 1.3 - 0.15;
                c = mix(a, b, 1.0 - smoothstep(radius - 0.15, radius + 0.15, d));
            }
            else
            {
                // Výseče: 12 výsečí (pomalu se točí), B prorůstá každou od jejího středu.
                float sector = fract(atan(p.y, p.x) / 6.2831853 * 12.0 + uTime * 0.02);
                float d = abs(sector - 0.5) * 2.0;
                float edge = uMix * 1.2 - 0.1;
                c = mix(a, b, 1.0 - smoothstep(edge - 0.1, edge + 0.1, d));
            }

            fragColor = vec4(c, 1.0);
        }
        """;

    private readonly GL _gl;
    private readonly ShaderProgram _program;
    private readonly FullscreenPass _pass;
    private readonly int _mixLocation;
    private readonly int _modeLocation;

    public Compositor(GL gl)
    {
        _gl = gl;
        _program = new ShaderProgram(gl, FullscreenPass.VertexShader, FragmentShader);
        _pass = new FullscreenPass(gl);
        _mixLocation = _program.GetUniformLocation("uMix");
        _modeLocation = _program.GetUniformLocation("uMode");

        _program.Use();
        gl.Uniform1(_program.GetUniformLocation("uA"), 0);
        gl.Uniform1(_program.GetUniformLocation("uB"), 1);
    }

    /// <param name="mix">Poloha crossfaderu: 0 = jen A, 1 = jen B.</param>
    /// <param name="aspect">Poměr šířky a výšky cíle (pro tvarové režimy Kruh a Výseče).</param>
    /// <param name="time">Čas v sekundách (pomalé otáčení výsečí).</param>
    public void Draw(uint textureA, uint textureB, float mix, BlendMode mode, float aspect = 1f, float time = 0f)
    {
        _program.Use();
        _program.Set("uAspect", aspect);
        _program.Set("uTime", time);
        _gl.Disable(EnableCap.Blend);
        _program.Use();
        _gl.ActiveTexture(TextureUnit.Texture0);
        _gl.BindTexture(TextureTarget.Texture2D, textureA);
        _gl.ActiveTexture(TextureUnit.Texture1);
        _gl.BindTexture(TextureTarget.Texture2D, textureB);
        _gl.Uniform1(_mixLocation, Math.Clamp(mix, 0f, 1f));
        _gl.Uniform1(_modeLocation, (int)mode);
        _pass.Draw();
        _gl.ActiveTexture(TextureUnit.Texture0);
    }

    public void Dispose()
    {
        _pass.Dispose();
        _program.Dispose();
    }
}
