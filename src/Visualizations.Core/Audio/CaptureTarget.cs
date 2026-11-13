namespace Visualizations.Core.Audio;

/// <summary>Co se má zachytávat: zvuk jedné aplikace, nebo celý výstup výchozího zařízení.</summary>
/// <param name="ProcessName">Název procesu bez přípony (např. „Spotify“); <c>null</c> = celý výstup.</param>
public sealed record CaptureTarget(string? ProcessName)
{
    public static CaptureTarget SystemOutput { get; } = new((string?)null);

    public static CaptureTarget ForProcess(string processName) => new(processName);

    public override string ToString() => ProcessName ?? "celý výstup";
}
