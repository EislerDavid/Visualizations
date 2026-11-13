namespace Visualizations.App.Effects;

/// <summary>
/// „Fire Storm“ z rodiny Bars and Waves: spektrum je zdroj žáru u spodního okraje, žár stoupá
/// vzhůru, víří, chladne a mapuje se na ohnivou paletu. Zpětná vazba nese „teplotu“ v kanálu R.
/// </summary>
internal sealed class FireStormVisualization : FeedbackVisualization
{
    protected override string WarpShader => """
        #version 330 core
        in vec2 vUv;
        out vec4 fragColor;
        uniform sampler2D uPrevious;
        uniform sampler2D uSpectrum;
        uniform vec2 uResolution;
        uniform float uTime;
        uniform float uDeltaTime;
        uniform float uBeat;

        float hash(vec2 p) { return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453); }

        float noise(vec2 p)
        {
            vec2 i = floor(p);
            vec2 f = fract(p);
            f = f * f * (3.0 - 2.0 * f);
            return mix(mix(hash(i), hash(i + vec2(1.0, 0.0)), f.x),
                       mix(hash(i + vec2(0.0, 1.0)), hash(i + vec2(1.0, 1.0)), f.x), f.y);
        }

        void main()
        {
            vec2 px = 1.0 / uResolution;

            // Žár stoupá a víří do stran.
            float rise = uDeltaTime * 0.45;
            float swirl = noise(vec2(vUv.x * 9.0, vUv.y * 5.0 - uTime * 1.7)) - 0.5;
            vec2 source = vUv - vec2(swirl * 0.012, rise);

            float heat = texture(uPrevious, source).r * 0.4
                       + texture(uPrevious, source + vec2(-2.0 * px.x, 0.0)).r * 0.15
                       + texture(uPrevious, source + vec2( 2.0 * px.x, 0.0)).r * 0.15
                       + texture(uPrevious, source + vec2(0.0, -2.0 * px.y)).r * 0.15
                       + texture(uPrevious, source + vec2(0.0,  2.0 * px.y)).r * 0.15;

            // Chladnutí se šumem, aby se plameny trhaly na jazyky.
            float cooling = uDeltaTime * (0.35 + 0.9 * noise(vUv * vec2(14.0, 7.0) + vec2(0.0, -uTime * 2.3)));
            heat = max(heat - cooling, 0.0);

            // Zdroj: spektrum jako žhavé sloupce u spodního okraje; úder přiloží pod kotel.
            float level = texture(uSpectrum, vec2(vUv.x, 0.5)).r;
            float top = level * 0.28;
            float fuel = (1.0 - smoothstep(top - 0.02, top + 0.005, vUv.y)) * level;
            heat = max(heat, fuel * (1.2 + 0.6 * uBeat));

            fragColor = vec4(heat, 0.0, 0.0, 1.0);
        }
        """;

    protected override string PresentShader => """
        #version 330 core
        in vec2 vUv;
        out vec4 fragColor;
        uniform sampler2D uFrame;
        void main()
        {
            float t = texture(uFrame, vUv).r;
            // černá → tmavě rudá → oranžová → žlutá → bílá
            vec3 color = vec3(smoothstep(0.02, 0.40, t), smoothstep(0.30, 0.80, t), smoothstep(0.70, 1.10, t));
            fragColor = vec4(color, 1.0);
        }
        """;
}
