namespace Visualizations.App.Mixing;

/// <summary>
/// Jak se při mixu skládají dvě animace. Pořadí odpovídá <c>uMode</c> v kompozitoru.
/// Ve všech režimech platí: crossfader 0 = jen A, 1 = jen B.
/// </summary>
internal enum BlendMode
{
    Crossfade,
    Add,
    Screen,
    Lighten,
    Difference,
    Radial,
    Wedges,
}

internal static class BlendModeExtensions
{
    public static string Label(this BlendMode mode) => mode switch
    {
        BlendMode.Crossfade => "Prolínání",
        BlendMode.Add => "Sčítání",
        BlendMode.Screen => "Závoj",
        BlendMode.Lighten => "Zesvětlení",
        BlendMode.Difference => "Rozdíl",
        BlendMode.Radial => "Kruh",
        BlendMode.Wedges => "Výseče",
        _ => mode.ToString(),
    };

    public static string Description(this BlendMode mode) => mode switch
    {
        BlendMode.Crossfade => "Klasické prolnutí – crossfader plynule přechází z A do B.",
        BlendMode.Add => "Světlo se sčítá; uprostřed crossfaderu jsou obě animace naplno.",
        BlendMode.Screen => "Jako dva projektory na jedno plátno – jemnější než sčítání.",
        BlendMode.Lighten => "V každém bodě vyhraje jasnější z obou animací.",
        BlendMode.Difference => "Rozdíl barev – psychedelické negativní prolnutí.",
        BlendMode.Radial => "B se rozpíná ze středu jako kruh – drží středovou symetrii kaleidoskopů.",
        BlendMode.Wedges => "B prorůstá dvanácti výsečemi od jejich středu – kaleidoskop v kaleidoskopu.",
        _ => string.Empty,
    };
}
