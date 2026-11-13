namespace Visualizations.App.Effects.Kaleidoscope;

/// <summary>Co kaleidoskop každý snímek přikreslí do zpětné vazby.</summary>
internal enum KaleidoscopeShape
{
    /// <summary>Průběh signálu jako čára přes střed.</summary>
    WaveLine,

    /// <summary>Průběh signálu stočený do kruhu.</summary>
    WaveRing,

    /// <summary>Růžice s <c>ShapeCount</c> lístky, lístky se natahují podle spektra.</summary>
    Rose,

    /// <summary>Hvězda s <c>ShapeCount</c> cípy; vnější cípy podle basů, vnitřní podle výšek.</summary>
    Star,

    /// <summary>Spirála s <c>ShapeCount</c> závity, vlní se podle průběhu signálu.</summary>
    Spiral,

    /// <summary><c>ShapeCount</c> paprsků ze středu, délka podle spektra.</summary>
    Spokes,

    /// <summary><c>ShapeCount</c> bodů na oběžných drahách, velikost podle spektra.</summary>
    Orbits,

    /// <summary>Lissajousova křivka s poměrem frekvencí <c>ShapeCount</c> : <c>ShapeCount − 1</c>.</summary>
    Lissajous,

    /// <summary>Při každém úderu vyšlehne <c>ShapeCount</c> paprsků v náhodném natočení.</summary>
    Burst,
}

/// <summary>
/// Parametry jednoho kaleidoskopu. Pohyb a barvy se při přechodu mezi presety plynule interpolují;
/// počet výsečí a tvar se prolnou (oba se krátce kreslí současně).
/// </summary>
/// <param name="Segments">Počet zrcadlených výsečí.</param>
/// <param name="FoldInWarp">Zrcadlit už ve zpětné vazbě (fraktálovější), jinak až při zobrazení.</param>
/// <param name="ShapeRadius">Velikost tvaru vůči výšce obrazu.</param>
/// <param name="ShapeAmplitude">Jak moc zvuk tvar deformuje.</param>
/// <param name="ZoomSpeed">Rozpínání obrazu od středu (záporné = smršťování), za sekundu.</param>
/// <param name="RotationSpeed">Rotace zpětné vazby, rad/s.</param>
/// <param name="Swirl">Rotace navíc u středu (víření), rad/s.</param>
/// <param name="ViewRotationSpeed">Otáčení celého kaleidoskopu, rad/s.</param>
/// <param name="DecayTime">Jak dlouho stopy doznívají, s.</param>
/// <param name="HueSpeed">Rychlost přebarvování stop, rad/s.</param>
/// <param name="Wobble">Vlnění obrazu (zlomek výšky).</param>
/// <param name="BeatPunch">Jak silně úder obraz „nafoukne“.</param>
/// <param name="BassSpin">O kolik basy zrychlí rotaci, rad/s.</param>
/// <param name="Intensity">Jas přikreslovaného tvaru.</param>
/// <param name="Saturation">Sytost barev (1 = beze změny).</param>
/// <param name="BaseHue">Výchozí odstín tvaru (0–1).</param>
internal sealed record KaleidoscopePreset(
    string Name,
    int Segments,
    bool FoldInWarp,
    KaleidoscopeShape Shape,
    int ShapeCount,
    float ShapeRadius,
    float ShapeAmplitude,
    float ZoomSpeed,
    float RotationSpeed,
    float Swirl,
    float ViewRotationSpeed,
    float DecayTime,
    float HueSpeed,
    float Wobble,
    float BeatPunch,
    float BassSpin,
    float Intensity,
    float Saturation,
    float BaseHue)
{
    /// <summary>Plynulé parametry pohybu a barev mezi <paramref name="a"/> a <paramref name="b"/>; ostatní zůstanou z <paramref name="a"/>.</summary>
    public static KaleidoscopePreset Lerp(KaleidoscopePreset a, KaleidoscopePreset b, float t) => a with
    {
        ZoomSpeed = Mix(a.ZoomSpeed, b.ZoomSpeed, t),
        RotationSpeed = Mix(a.RotationSpeed, b.RotationSpeed, t),
        Swirl = Mix(a.Swirl, b.Swirl, t),
        ViewRotationSpeed = Mix(a.ViewRotationSpeed, b.ViewRotationSpeed, t),
        DecayTime = Mix(a.DecayTime, b.DecayTime, t),
        HueSpeed = Mix(a.HueSpeed, b.HueSpeed, t),
        Wobble = Mix(a.Wobble, b.Wobble, t),
        BeatPunch = Mix(a.BeatPunch, b.BeatPunch, t),
        BassSpin = Mix(a.BassSpin, b.BassSpin, t),
        Saturation = Mix(a.Saturation, b.Saturation, t),
    };

    private static float Mix(float a, float b, float t) => a + (b - a) * t;
}
