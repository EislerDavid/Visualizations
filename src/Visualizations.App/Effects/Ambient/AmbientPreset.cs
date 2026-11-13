using System.Numerics;

namespace Visualizations.App.Effects.Ambient;

/// <summary>Co ambientní animace přikresluje do zpětné vazby.</summary>
internal enum AmbientShape
{
    /// <summary>Průběh signálu stočený do kruhu uprostřed.</summary>
    WaveRing,

    /// <summary><c>ShapeCount</c> vodorovných průběhů signálu ve výšce <c>ShapeY</c>.</summary>
    WaveLines,

    /// <summary>Kruhy na vodě: na úder vznikne v náhodném místě rozpínající se kruh (nejvýš <c>ShapeCount</c>).</summary>
    Ripples,

    /// <summary>Bubliny stoupající zespodu; víc s basy a údery (nejvýš <c>ShapeCount</c>).</summary>
    Bubbles,

    /// <summary>Kapky barvy na náhodných místech (na úder <c>ShapeCount</c>), proudění je rozmývá jako inkoust.</summary>
    Drops,

    /// <summary><c>ShapeCount</c> zářících koulí na pomalých drahách, velikost podle pásem spektra.</summary>
    Orbs,
}

/// <summary>Barevná paleta: jas obrazu 0 → 1 se mapuje přes čtyři barvy.</summary>
internal readonly record struct AmbientPalette(Vector3 C0, Vector3 C1, Vector3 C2, Vector3 C3)
{
    public static AmbientPalette Ocean { get; } = new(
        new(0.00f, 0.02f, 0.06f), new(0.00f, 0.22f, 0.50f), new(0.15f, 0.70f, 0.85f), new(0.85f, 1.00f, 1.00f));

    public static AmbientPalette Smoke { get; } = new(
        new(0.00f, 0.00f, 0.00f), new(0.12f, 0.12f, 0.18f), new(0.50f, 0.46f, 0.62f), new(0.95f, 0.93f, 1.00f));

    public static AmbientPalette Aurora { get; } = new(
        new(0.00f, 0.02f, 0.04f), new(0.00f, 0.35f, 0.25f), new(0.20f, 0.95f, 0.55f), new(0.85f, 0.45f, 0.95f));

    public static AmbientPalette Lava { get; } = new(
        new(0.04f, 0.00f, 0.00f), new(0.55f, 0.05f, 0.00f), new(1.00f, 0.45f, 0.00f), new(1.00f, 0.95f, 0.55f));

    public static AmbientPalette Lerp(AmbientPalette a, AmbientPalette b, float t) => new(
        Vector3.Lerp(a.C0, b.C0, t), Vector3.Lerp(a.C1, b.C1, t), Vector3.Lerp(a.C2, b.C2, t), Vector3.Lerp(a.C3, b.C3, t));
}

/// <summary>
/// Parametry ambientní animace. Výchozí hodnoty = nic se neděje, preset uvádí jen to, co používá.
/// Pohyb a barvy se při přechodu mezi presety plynule interpolují; tvar se prolne.
/// </summary>
internal sealed record AmbientPreset
{
    public required string Name { get; init; }

    // --- Tvar (kreslí se zvlášť pro každý preset, při přechodu s vahou) ---
    public AmbientShape Shape { get; init; } = AmbientShape.WaveRing;

    public int ShapeCount { get; init; } = 1;

    /// <summary>Velikost tvaru vůči výšce obrazu.</summary>
    public float ShapeRadius { get; init; } = 0.16f;

    /// <summary>Jak moc zvuk tvar deformuje.</summary>
    public float ShapeAmplitude { get; init; } = 0.35f;

    /// <summary>Svislá poloha tvaru (0 = dole, 1 = nahoře) – pro <see cref="AmbientShape.WaveLines"/>.</summary>
    public float ShapeY { get; init; } = 0.5f;

    /// <summary>Jas přikreslovaného tvaru.</summary>
    public float Intensity { get; init; } = 0.16f;

    public float BaseHue { get; init; }

    // --- Pohyb zpětné vazby (za sekundu) ---

    /// <summary>Rozpínání od středu (záporné = smršťování).</summary>
    public float ZoomSpeed { get; init; }

    /// <summary>Rozpínání navíc podle basů.</summary>
    public float BassZoom { get; init; }

    /// <summary>Jak silně úder obraz „nafoukne“.</summary>
    public float BeatPunch { get; init; }

    /// <summary>Rotace, rad/s.</summary>
    public float RotationSpeed { get; init; }

    /// <summary>Rotace navíc podle basů, rad/s.</summary>
    public float BassSpin { get; init; }

    /// <summary>0 = obraz se točí celý, 1 = víc u středu (vír).</summary>
    public float Vortex { get; init; }

    /// <summary>Vlnění síly víru po poloměru.</summary>
    public float VortexWave { get; init; }

    /// <summary>Vlnění obrazu (zlomek výšky za snímek).</summary>
    public float Wobble { get; init; }

    /// <summary>Soustředné vlnky na hladině (za snímek).</summary>
    public float Ripple { get; init; }

    /// <summary>Posun obrazu (zlomek výšky za sekundu), např. stoupající kouř.</summary>
    public Vector2 Drift { get; init; }

    /// <summary>
    /// Síla proudění (vírové pole ze šumu), ~zlomek výšky za sekundu. Zásada: <c>Flow × FlowScale ≤ ~0,03</c>.
    /// Silnější nebo drobnější víry obsah roztahují a stlačují rychleji, než ho rozostření stihne vyhladit,
    /// a v obraze vznikají ostré praskliny a hrany.
    /// </summary>
    public float Flow { get; init; }

    /// <summary>Velikost vírů proudění (větší = menší víry).</summary>
    public float FlowScale { get; init; } = 2f;

    /// <summary>Jak rychle se proudění mění.</summary>
    public float FlowSpeed { get; init; } = 0.2f;

    /// <summary>Rozostření za snímek v pixelech (měkkost, kouř).</summary>
    public float Blur { get; init; }

    /// <summary>Jak dlouho stopy doznívají, s.</summary>
    public float DecayTime { get; init; } = 1f;

    /// <summary>Rychlost přebarvování stop, rad/s.</summary>
    public float HueSpeed { get; init; } = 0.5f;

    /// <summary>Množství blikajících hvězd.</summary>
    public float Stars { get; init; }

    // --- Barvy ---
    public float Exposure { get; init; } = 1.6f;

    /// <summary>0 = vlastní barvy tvarů, 1 = barvy podle palety.</summary>
    public float PaletteMix { get; init; }

    public AmbientPalette Palette { get; init; } = AmbientPalette.Ocean;

    /// <summary>Ztmavení okrajů obrazu.</summary>
    public float Vignette { get; init; }

    /// <summary>Plynulé parametry pohybu a barev mezi <paramref name="a"/> a <paramref name="b"/>; tvar zůstane z <paramref name="a"/>.</summary>
    public static AmbientPreset Lerp(AmbientPreset a, AmbientPreset b, float t) => a with
    {
        ZoomSpeed = Mix(a.ZoomSpeed, b.ZoomSpeed, t),
        BassZoom = Mix(a.BassZoom, b.BassZoom, t),
        BeatPunch = Mix(a.BeatPunch, b.BeatPunch, t),
        RotationSpeed = Mix(a.RotationSpeed, b.RotationSpeed, t),
        BassSpin = Mix(a.BassSpin, b.BassSpin, t),
        Vortex = Mix(a.Vortex, b.Vortex, t),
        VortexWave = Mix(a.VortexWave, b.VortexWave, t),
        Wobble = Mix(a.Wobble, b.Wobble, t),
        Ripple = Mix(a.Ripple, b.Ripple, t),
        Drift = Vector2.Lerp(a.Drift, b.Drift, t),
        Flow = Mix(a.Flow, b.Flow, t),
        FlowScale = Mix(a.FlowScale, b.FlowScale, t),
        FlowSpeed = Mix(a.FlowSpeed, b.FlowSpeed, t),
        Blur = Mix(a.Blur, b.Blur, t),
        DecayTime = Mix(a.DecayTime, b.DecayTime, t),
        HueSpeed = Mix(a.HueSpeed, b.HueSpeed, t),
        Stars = Mix(a.Stars, b.Stars, t),
        Exposure = Mix(a.Exposure, b.Exposure, t),
        PaletteMix = Mix(a.PaletteMix, b.PaletteMix, t),
        Palette = AmbientPalette.Lerp(a.Palette, b.Palette, t),
        Vignette = Mix(a.Vignette, b.Vignette, t),
    };

    private static float Mix(float a, float b, float t) => a + (b - a) * t;
}
