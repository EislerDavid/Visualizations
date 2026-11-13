using System.Text.Json;
using System.Text.Json.Serialization;
using Visualizations.App.Mixing;

namespace Visualizations.App;

/// <summary>
/// Uživatelské nastavení – zároveň živý stav, se kterým pracuje toolbar (proto veřejná pole,
/// která jde předat do ImGui přes <c>ref</c>). Ukládá se při zavření do %APPDATA%\Visualizations.
/// </summary>
internal sealed class Settings
{
    public string Animation = "Battery: Kaleidoscope";
    public float Sensitivity;

    public bool AutoSwitch;
    public float AutoSwitchSeconds = 20f;
    public bool AutoSwitchRandom;

    /// <summary>Rodina, ze které vybírá střídání, náhodný výběr a obměna v mixu; prázdné = všechny animace.</summary>
    public string AutoSwitchFamily = "Battery";

    /// <summary>Po uplynutí intervalu přepnout až na nejbližší úder.</summary>
    public bool AutoSwitchOnBeat = true;

    public float TransitionSeconds = 2.5f;

    public bool MixEnabled;
    public string MixAnimation = "Battery: Mandala";
    public float Crossfader = 0.5f;
    public BlendMode MixMode = BlendMode.Screen;
    public bool AutoCrossfade;
    public float AutoCrossfadeSeconds = 12f;

    /// <summary>Při automatickém crossfaderu vyměnit úplně skrytou animaci za jinou.</summary>
    public bool AutoMixRefresh = true;

    public bool ToolbarPinned;

    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Visualizations", "settings.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        IncludeFields = true,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public static Settings Load()
    {
        try
        {
            return File.Exists(FilePath)
                ? JsonSerializer.Deserialize<Settings>(File.ReadAllText(FilePath), JsonOptions) ?? new Settings()
                : new Settings();
        }
        catch (Exception e) when (e is IOException or JsonException or UnauthorizedAccessException)
        {
            Console.WriteLine($"Nastavení nejde načíst ({e.Message}), použiju výchozí.");
            return new Settings();
        }
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this, JsonOptions));
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            Console.WriteLine($"Nastavení nejde uložit: {e.Message}");
        }
    }
}
