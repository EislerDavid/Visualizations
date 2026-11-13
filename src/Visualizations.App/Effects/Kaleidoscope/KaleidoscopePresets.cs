namespace Visualizations.App.Effects.Kaleidoscope;

/// <summary>Presety rodiny Battery (kaleidoskopy). Nový preset = nový řádek.</summary>
internal static class KaleidoscopePresets
{
    public static IReadOnlyList<KaleidoscopePreset> All { get; } =
    [
        new("Kaleidoscope", Segments: 6, FoldInWarp: false, KaleidoscopeShape.WaveLine, ShapeCount: 1,
            ShapeRadius: 0.45f, ShapeAmplitude: 0.25f, ZoomSpeed: 0.25f, RotationSpeed: 0.15f, Swirl: 0f,
            ViewRotationSpeed: 0.12f, DecayTime: 0.9f, HueSpeed: 1.2f, Wobble: 0.002f, BeatPunch: 1.2f,
            BassSpin: 0.6f, Intensity: 0.18f, Saturation: 1.0f, BaseHue: 0.00f),

        new("Mandala", Segments: 8, FoldInWarp: true, KaleidoscopeShape.Rose, ShapeCount: 4,
            ShapeRadius: 0.3f, ShapeAmplitude: 0.35f, ZoomSpeed: 0.3f, RotationSpeed: 0.05f, Swirl: 0.2f,
            ViewRotationSpeed: 0.05f, DecayTime: 1.2f, HueSpeed: 0.9f, Wobble: 0.0015f, BeatPunch: 1.0f,
            BassSpin: 0.3f, Intensity: 0.12f, Saturation: 1.0f, BaseHue: 0.75f),

        new("Stardust", Segments: 12, FoldInWarp: false, KaleidoscopeShape.Orbits, ShapeCount: 5,
            ShapeRadius: 0.25f, ShapeAmplitude: 0.5f, ZoomSpeed: 0.6f, RotationSpeed: 0.3f, Swirl: 0.1f,
            ViewRotationSpeed: -0.08f, DecayTime: 0.6f, HueSpeed: 1.5f, Wobble: 0.001f, BeatPunch: 2.5f,
            BassSpin: 0.8f, Intensity: 0.35f, Saturation: 0.9f, BaseHue: 0.55f),

        new("Spiral", Segments: 5, FoldInWarp: false, KaleidoscopeShape.Spiral, ShapeCount: 3,
            ShapeRadius: 0.35f, ShapeAmplitude: 0.2f, ZoomSpeed: 0.2f, RotationSpeed: 0.6f, Swirl: 0.8f,
            ViewRotationSpeed: 0.1f, DecayTime: 0.8f, HueSpeed: 1.2f, Wobble: 0.002f, BeatPunch: 1.0f,
            BassSpin: 1.0f, Intensity: 0.14f, Saturation: 1.0f, BaseHue: 0.10f),

        new("Crystal", Segments: 3, FoldInWarp: true, KaleidoscopeShape.Star, ShapeCount: 6,
            ShapeRadius: 0.2f, ShapeAmplitude: 0.4f, ZoomSpeed: 0.35f, RotationSpeed: 0f, Swirl: 0f,
            ViewRotationSpeed: 0.06f, DecayTime: 0.5f, HueSpeed: 0.6f, Wobble: 0f, BeatPunch: 1.5f,
            BassSpin: 0.2f, Intensity: 0.22f, Saturation: 0.8f, BaseHue: 0.50f),

        new("Web", Segments: 10, FoldInWarp: false, KaleidoscopeShape.Spokes, ShapeCount: 5,
            ShapeRadius: 0.45f, ShapeAmplitude: 0.3f, ZoomSpeed: -0.15f, RotationSpeed: 0.1f, Swirl: 0f,
            ViewRotationSpeed: 0.04f, DecayTime: 1.0f, HueSpeed: 0.8f, Wobble: 0.001f, BeatPunch: 0.8f,
            BassSpin: 0.4f, Intensity: 0.12f, Saturation: 0.9f, BaseHue: 0.30f),

        new("Lotus", Segments: 7, FoldInWarp: true, KaleidoscopeShape.Rose, ShapeCount: 7,
            ShapeRadius: 0.3f, ShapeAmplitude: 0.25f, ZoomSpeed: 0.1f, RotationSpeed: -0.1f, Swirl: 0.3f,
            ViewRotationSpeed: 0.03f, DecayTime: 1.4f, HueSpeed: 0.5f, Wobble: 0.003f, BeatPunch: 0.6f,
            BassSpin: 0.3f, Intensity: 0.08f, Saturation: 1.05f, BaseHue: 0.90f),

        new("Tunnel", Segments: 6, FoldInWarp: false, KaleidoscopeShape.WaveRing, ShapeCount: 1,
            ShapeRadius: 0.08f, ShapeAmplitude: 0.6f, ZoomSpeed: 0.9f, RotationSpeed: 0.2f, Swirl: 0.4f,
            ViewRotationSpeed: 0.15f, DecayTime: 0.9f, HueSpeed: 1.5f, Wobble: 0f, BeatPunch: 3.0f,
            BassSpin: 0.5f, Intensity: 0.16f, Saturation: 1.0f, BaseHue: 0.65f),

        new("Harmony", Segments: 4, FoldInWarp: true, KaleidoscopeShape.Lissajous, ShapeCount: 3,
            ShapeRadius: 0.3f, ShapeAmplitude: 0.3f, ZoomSpeed: 0.12f, RotationSpeed: 0.25f, Swirl: -0.3f,
            ViewRotationSpeed: -0.06f, DecayTime: 0.9f, HueSpeed: 1.0f, Wobble: 0.002f, BeatPunch: 1.0f,
            BassSpin: 0.5f, Intensity: 0.1f, Saturation: 1.0f, BaseHue: 0.20f),

        new("Supernova", Segments: 8, FoldInWarp: false, KaleidoscopeShape.Burst, ShapeCount: 9,
            ShapeRadius: 0.4f, ShapeAmplitude: 0.5f, ZoomSpeed: 0.7f, RotationSpeed: 0.1f, Swirl: 0f,
            ViewRotationSpeed: 0.05f, DecayTime: 0.7f, HueSpeed: 2.0f, Wobble: 0f, BeatPunch: 2.0f,
            BassSpin: 0.4f, Intensity: 0.3f, Saturation: 0.9f, BaseHue: 0.05f),
    ];
}
