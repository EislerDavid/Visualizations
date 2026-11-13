namespace Visualizations.App.Effects;

/// <summary>Položka v nabídce animací.</summary>
/// <param name="Family">Rodina (ve WMP např. „Bars and Waves“, „Ambience“, „Battery“).</param>
/// <param name="Preset">Název konkrétní animace v rodině.</param>
internal sealed record VisualizationInfo(string Family, string Preset, Func<IVisualization> Create)
{
    public string Name => $"{Family}: {Preset}";
}

/// <summary>Seznam všech animací. Pořadí určuje klávesy 1–9 a přepínání šipkami.</summary>
internal static class VisualizationCatalog
{
    public static IReadOnlyList<VisualizationInfo> All { get; } =
    [
        new("Bars and Waves", "Bars", () => new BarsVisualization()),
        new("Bars and Waves", "Scope", () => new ScopeVisualization()),
    ];

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
}
