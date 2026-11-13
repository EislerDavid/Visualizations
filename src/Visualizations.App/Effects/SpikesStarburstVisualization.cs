namespace Visualizations.App.Effects;

/// <summary>
/// „Spikes“: hvězdice paprsků ze středu, délka paprsku = úroveň pásma spektra (basy nahoře a dole,
/// výšky po stranách, zrcadleně). Pomalu rotuje, barvy plynou dokola, jádro pulzuje s údery.
/// </summary>
internal sealed class SpikesStarburstVisualization : ShaderVisualization
{
    protected override string FragmentShader => """
        #version 330 core
        in vec2 vUv;
        out vec4 fragColor;
        uniform sampler2D uSpectrum;
        uniform vec2 uResolution;
        uniform float uTime;
        uniform float uBass;
        uniform float uBeat;

        const float PI = 3.14159265;
        const float SPIKES = 48.0;  // paprsků na polovinu kruhu (zrcadlením 96)

        vec3 hsv(float h, float s, float v)
        {
            vec3 k = clamp(abs(mod(h * 6.0 + vec3(0.0, 4.0, 2.0), 6.0) - 3.0) - 1.0, 0.0, 1.0);
            return v * mix(vec3(1.0), k, s);
        }

        void main()
        {
            vec2 p = (vUv - 0.5) * vec2(uResolution.x / uResolution.y, 1.0);
            float r = length(p);

            // Úhel 0–1 dokola se zrcadlením → symetrická hvězda; pomalá rotace.
            float a = atan(p.y, p.x) / (2.0 * PI) + 0.25 + uTime * 0.02;
            float m = abs(fract(a) * 2.0 - 1.0);
            float cell = m * SPIKES;
            float index = floor(cell);
            float level = texture(uSpectrum, vec2((index + 0.5) / SPIKES, 0.5)).r;

            float inner = 0.10 + 0.03 * uBeat;
            float len = 0.03 + level * 0.40;
            float k = (r - inner) / len;                          // 0 u kořene, 1 na špičce
            float halfWidth = 0.45 * (1.0 - clamp(k, 0.0, 1.0));  // paprsek se ke špičce zužuje
            float d = abs(fract(cell) - 0.5) - halfWidth;          // < 0 uvnitř paprsku
            float aa = fwidth(cell) * 1.5;
            float spike = (1.0 - smoothstep(-aa, aa, d)) * step(0.0, k) * step(k, 1.0);

            vec3 color = hsv(fract(index / SPIKES * 0.5 + uTime * 0.04), 0.75, 1.0);
            vec3 c = color * spike * (0.55 + 0.45 * (1.0 - k));

            // Záře za špičkami.
            float beyond = max(r - inner - len, 0.0);
            float glow = exp(-beyond * 18.0) * (1.0 - smoothstep(0.0, 0.5, abs(fract(cell) - 0.5))) * level * step(inner, r);
            c += color * glow * 0.35;

            // Jádro: zářící prstenec u kořenů paprsků a měkké světlo uvnitř, které bliká s údery.
            vec3 coreColor = mix(hsv(fract(uTime * 0.04), 0.6, 1.0), vec3(1.0), 0.4 * uBeat);
            float ring = exp(-pow((r - inner) / 0.006, 2.0)) * (0.5 + 0.5 * uBass);
            float light = exp(-r / (inner * 0.45)) * (0.12 + 0.6 * uBeat + 0.3 * uBass);
            c += coreColor * (ring + light);

            fragColor = vec4(c, 1.0);
        }
        """;
}
