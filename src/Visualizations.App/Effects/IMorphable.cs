namespace Visualizations.App.Effects;

/// <summary>
/// Vizualizace, která umí plynule přejít na jinou animaci změnou vlastních parametrů
/// (např. kaleidoskop na jiný kaleidoskop) místo prolínání dvou samostatných obrazů.
/// </summary>
internal interface IMorphable
{
    /// <returns><c>false</c>, pokud na cílovou animaci přejít neumí – pak se použije obyčejné prolnutí.</returns>
    bool TryMorphTo(VisualizationInfo target, float seconds);
}
