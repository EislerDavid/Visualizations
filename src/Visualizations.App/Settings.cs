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
    public string Animation = "Bars and Waves: Bars";
    public float Sensitivity;

    public bool AutoSwitch;
    public float AutoSwitchSeconds = 20f;
    public bool AutoSwitchRandom;
    public float TransitionSeconds = 1.5f;

    public bool MixEnabled;
    public string MixAnimation = "Bars and Waves: Scope";
    public float Crossfader = 0.5f;
    public BlendMode MixMode = BlendMode.Screen;
    public bool AutoCrossfade;
    public float AutoCrossfadeSeconds = 12f;

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
