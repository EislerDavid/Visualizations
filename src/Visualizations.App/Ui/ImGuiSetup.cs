using System.Numerics;
using System.Runtime.InteropServices;
using ImGuiNET;
using Silk.NET.OpenGL.Extensions.ImGui;

namespace Visualizations.App.Ui;

/// <summary>Písmo, konfigurace a vzhled Dear ImGui.</summary>
internal static class ImGuiSetup
{
    public static readonly Vector4 Accent = new(0.20f, 0.47f, 0.90f, 1f);
    public static readonly Vector4 Warning = new(1.00f, 0.70f, 0.25f, 1f);
    public static readonly Vector4 Dim = new(0.62f, 0.65f, 0.72f, 1f);

    private const int FontSize = 17;

    // Výchozí písmo ImGui umí jen ASCII. Rozsahy: latinka vč. češtiny (U+0020–017F),
    // typografická interpunkce (– … „ “), šipky (← ↑ → ↓ ↔) a znaménko minus (−).
    // ImGui pole potřebuje po celou dobu běhu, proto se záměrně neuvolňuje.
    private static readonly nint GlyphRanges = AllocateGlyphRanges(
        [0x0020, 0x017F, 0x2010, 0x2027, 0x2190, 0x2199, 0x2212, 0x2212, 0]);

    /// <summary>Segoe UI s českými znaky; <c>null</c> (výchozí písmo), pokud ve Windows chybí.</summary>
    public static ImGuiFontConfig? FontConfig()
    {
        string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "segoeui.ttf");
        return File.Exists(path) ? new ImGuiFontConfig(path, FontSize, _ => GlyphRanges) : null;
    }

    /// <summary>Vypne imgui.ini – rozložení je pevné a stav se ukládá do vlastního nastavení.</summary>
    public static unsafe void ConfigureIO() => ImGui.GetIO().NativePtr->IniFilename = null;

    public static void ApplyStyle()
    {
        ImGui.StyleColorsDark();
        var style = ImGui.GetStyle();
        style.WindowPadding = new Vector2(10, 8);
        style.FramePadding = new Vector2(8, 5);
        style.ItemSpacing = new Vector2(6, 6);
        style.WindowRounding = 6f;
        style.PopupRounding = 6f;
        style.FrameRounding = 4f;
        style.GrabRounding = 4f;
        style.WindowBorderSize = 0f;
        style.PopupBorderSize = 0f;

        var colors = style.Colors;
        colors[(int)ImGuiCol.WindowBg] = new Vector4(0.06f, 0.07f, 0.09f, 1f);
        colors[(int)ImGuiCol.PopupBg] = new Vector4(0.08f, 0.09f, 0.12f, 0.97f);
        colors[(int)ImGuiCol.FrameBg] = new Vector4(0.15f, 0.17f, 0.21f, 1f);
        colors[(int)ImGuiCol.FrameBgHovered] = new Vector4(0.21f, 0.24f, 0.30f, 1f);
        colors[(int)ImGuiCol.FrameBgActive] = new Vector4(0.25f, 0.29f, 0.37f, 1f);
        colors[(int)ImGuiCol.Button] = new Vector4(0.17f, 0.19f, 0.24f, 1f);
        colors[(int)ImGuiCol.ButtonHovered] = new Vector4(0.26f, 0.36f, 0.56f, 1f);
        colors[(int)ImGuiCol.ButtonActive] = Accent;
        colors[(int)ImGuiCol.Header] = new Vector4(0.20f, 0.30f, 0.50f, 1f);
        colors[(int)ImGuiCol.HeaderHovered] = new Vector4(0.26f, 0.38f, 0.62f, 1f);
        colors[(int)ImGuiCol.HeaderActive] = Accent;
        colors[(int)ImGuiCol.CheckMark] = new Vector4(0.45f, 0.70f, 1f, 1f);
        colors[(int)ImGuiCol.SliderGrab] = new Vector4(0.40f, 0.62f, 1f, 1f);
        colors[(int)ImGuiCol.SliderGrabActive] = new Vector4(0.60f, 0.78f, 1f, 1f);
        colors[(int)ImGuiCol.Separator] = new Vector4(0.30f, 0.33f, 0.40f, 1f);
        colors[(int)ImGuiCol.TitleBg] = new Vector4(0.08f, 0.09f, 0.12f, 1f);
        colors[(int)ImGuiCol.TitleBgActive] = new Vector4(0.12f, 0.16f, 0.24f, 1f);
    }

    private static unsafe nint AllocateGlyphRanges(ReadOnlySpan<ushort> ranges)
    {
        nint memory = Marshal.AllocHGlobal(ranges.Length * sizeof(ushort));
        ranges.CopyTo(new Span<ushort>((void*)memory, ranges.Length));
        return memory;
    }
}
