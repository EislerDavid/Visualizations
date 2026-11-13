using System.Numerics;

namespace Visualizations.App.Effects.Ambient;

/// <summary>Presety rodiny Ambience. Preset uvádí jen parametry, které používá; nový preset = nový blok.</summary>
internal static class AmbientPresets
{
    public static IReadOnlyList<AmbientPreset> All { get; } =
    [
        // Kruhový průběh signálu ve víru, basy zrychlují rotaci, údery obraz nafouknou.
        new()
        {
            Name = "Swirl", Shape = AmbientShape.WaveRing, ShapeRadius = 0.16f, ShapeAmplitude = 0.35f, Intensity = 0.16f,
            ZoomSpeed = 0.18f, BassZoom = 0.5f, BeatPunch = 1.5f, RotationSpeed = 0.35f, BassSpin = 1.2f,
            Vortex = 1f, VortexWave = 0.5f, Wobble = 0.0025f, DecayTime = 0.55f, HueSpeed = 0.5f,
        },

        // Hladina: dvě vlnící se čáry, pomalé proudění a soustředné vlnky v modré paletě.
        new()
        {
            Name = "Water", Shape = AmbientShape.WaveLines, ShapeCount = 2, ShapeY = 0.5f, ShapeAmplitude = 0.25f,
            Intensity = 0.08f, ZoomSpeed = 0.04f, Ripple = 0.002f, Wobble = 0.0015f,
            Flow = 0.02f, FlowScale = 1.5f, FlowSpeed = 0.15f, Blur = 1f, DecayTime = 1.2f, HueSpeed = 0.1f,
            PaletteMix = 0.85f, Palette = AmbientPalette.Ocean, Vignette = 0.5f,
        },

        // Kouř stoupá od průběhu signálu u spodního okraje, víří a rozplývá se. Proudění musí být slabší
        // než stoupání a víry velké: kde by proudění táhlo tmavý vzduch shora proti kouři, rozhraní se
        // stlačí do ostré rovné hrany.
        new()
        {
            Name = "Smoke", Shape = AmbientShape.WaveLines, ShapeCount = 1, ShapeY = 0.15f, ShapeAmplitude = 0.18f,
            Intensity = 0.07f, Drift = new Vector2(0f, 0.12f), Flow = 0.02f, FlowScale = 1.5f, FlowSpeed = 0.15f,
            Blur = 2.5f, DecayTime = 1.6f, HueSpeed = 0.2f, PaletteMix = 0.6f, Palette = AmbientPalette.Smoke, Vignette = 0.6f,
        },

        // Mlhovina: barevné koule podle pásem spektra, pomalé proudění, dlouhé doznívání a hvězdy.
        new()
        {
            Name = "Nebula", Shape = AmbientShape.Orbs, ShapeCount = 6, ShapeRadius = 0.12f, ShapeAmplitude = 0.8f,
            Intensity = 0.06f, ZoomSpeed = 0.03f, RotationSpeed = 0.05f, Flow = 0.02f, FlowScale = 1.2f, FlowSpeed = 0.1f,
            Blur = 1.5f, DecayTime = 1.8f, HueSpeed = 0.3f, Stars = 0.3f, Vignette = 0.4f,
        },

        // Polární záře: tři průběhy nahoře se roztékají dolů v zeleno-fialové paletě.
        new()
        {
            Name = "Aurora", Shape = AmbientShape.WaveLines, ShapeCount = 3, ShapeY = 0.7f, ShapeAmplitude = 0.12f,
            Intensity = 0.07f, Drift = new Vector2(0.01f, -0.04f), Flow = 0.025f, FlowScale = 1.2f, FlowSpeed = 0.2f,
            Blur = 1.5f, DecayTime = 1f, HueSpeed = 0.15f, PaletteMix = 0.9f, Palette = AmbientPalette.Aurora, Vignette = 0.3f,
        },

        // Lávová lampa: bubliny stoupají a slévají se v ohnivé paletě.
        new()
        {
            Name = "Lava", Shape = AmbientShape.Bubbles, ShapeCount = 12, ShapeRadius = 0.05f, ShapeAmplitude = 0.6f,
            Intensity = 0.14f, Drift = new Vector2(0f, 0.03f), Flow = 0.025f, FlowScale = 1.2f, FlowSpeed = 0.15f,
            Blur = 2f, DecayTime = 1.4f, HueSpeed = 0f, PaletteMix = 1f, Palette = AmbientPalette.Lava, Vignette = 0.5f,
        },

        // Hyperprostor: hvězdy se rozmazávají do pruhů, údery přidají rychlost.
        new()
        {
            Name = "Warp", Shape = AmbientShape.WaveRing, ShapeRadius = 0.05f, ShapeAmplitude = 0.5f, Intensity = 0.04f,
            ZoomSpeed = 0.8f, BeatPunch = 2f, RotationSpeed = 0.1f, Vortex = 0.5f, DecayTime = 0.5f, HueSpeed = 1f, Stars = 3f,
        },

        // Inkoust ve vodě: na údery kápnou barvy, proudění je rozmývá.
        new()
        {
            Name = "Ink", Shape = AmbientShape.Drops, ShapeCount = 3, ShapeRadius = 0.022f, Intensity = 0.8f,
            Flow = 0.03f, FlowScale = 1.2f, FlowSpeed = 0.25f, Blur = 1.5f, DecayTime = 2f, HueSpeed = 0.2f, Vignette = 0.3f,
        },

        // Kruhy na vodě: každý úder rozvlní hladinu z jiného místa.
        new()
        {
            Name = "Ripples", Shape = AmbientShape.Ripples, ShapeCount = 6, ShapeRadius = 0.4f, ShapeAmplitude = 0.3f,
            Intensity = 0.25f, Flow = 0.01f, Blur = 0.5f, DecayTime = 0.8f, HueSpeed = 0.2f,
            PaletteMix = 0.7f, Palette = AmbientPalette.Ocean, Vignette = 0.5f,
        },
    ];
}
