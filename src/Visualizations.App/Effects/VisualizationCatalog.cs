using Visualizations.App.Effects.Kaleidoscope;

namespace Visualizations.App.Effects;

/// <summary>Položka v nabídce animací.</summary>
/// <param name="Family">Rodina (ve WMP např. „Battery“, „Bars and Waves“, „Ambience“).</param>
/// <param name="Preset">Název konkrétní animace v rodině.</param>
/// <param name="MorphData">Data pro plynulý přechod (<see cref="IMorphable"/>), např. preset kaleidoskopu.</param>
internal sealed record VisualizationInfo(string Family, string Preset, Func<IVisualization> Create, object? MorphData = null)
{
    public string Name => $"{Family}: {Preset}";
}

/// <summary>Seznam všech animací. Pořadí určuje klávesy 1–9 a přepínání šipkami.</summary>
internal static class VisualizationCatalog
{
    /// <summary>Rodina kaleidoskopů – na ni je ve výchozím stavu laděné střídání a mix.</summary>
    public const string KaleidoscopeFamily = "Battery";

    public static IReadOnlyList<VisualizationInfo> All { get; } =
    [
        .. KaleidoscopePresets.All.Select(preset =>
            new VisualizationInfo(KaleidoscopeFamily, preset.Name, () => new KaleidoscopeVisualization(preset), preset)),
        new("Bars and Waves", "Bars", () => new BarsVisualization()),
        new("Bars and Waves", "Scope", () => new ScopeVisualization()),
        new("Bars and Waves", "Fire Storm", () => new FireStormVisualization()),
        new("Ambience", "Swirl", () => new AmbienceSwirlVisualization()),
        new("Spikes", "Starburst", () => new SpikesStarburstVisualization()),
    ];

    /// <summary>Rodiny v pořadí, v jakém jsou v seznamu.</summary>
    public static IReadOnlyList<string> Families { get; } = All.Select(info => info.Family).Distinct().ToList();

    public static int IndexOf(string? name)
    {
        for (int i = 0; i < All.Count; i++)
        {
            if (All[i].Name == name)
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>Popisek rodiny pro uživatele.</summary>
    public static string FamilyLabel(string family) =>
        family == KaleidoscopeFamily ? "Kaleidoskopy (Battery)" : family;
}
