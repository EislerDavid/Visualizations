using System.Numerics;
using ImGuiNET;
using Visualizations.App.Effects;
using Visualizations.App.Mixing;
using Visualizations.Core.Audio;

namespace Visualizations.App.Ui;

/// <summary>
/// Lišta dole v okně (výběr a přepínání animací, mix, citlivost, celá obrazovka) a k ní panel mixu,
/// nabídka „…“ a nápověda. Po chvíli bez pohybu myši se plynule schová.
/// </summary>
internal sealed class Toolbar(VisualizerApp app)
{
    private const ImGuiWindowFlags BarFlags = ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoMove
        | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoNav
        | ImGuiWindowFlags.NoBringToFrontOnFocus;

    private const ImGuiWindowFlags PanelFlags = ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoMove
        | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoNav
        | ImGuiWindowFlags.AlwaysAutoResize;

    private const float AnimationComboWidth = 250f;
    private const float MixerLabelWidth = 90f;
    private const float MixerControlWidth = 250f;

    private static readonly (string Keys, string Action)[] Shortcuts =
    [
        ("← / →", "předchozí / další animace"),
        ("1 – 9", "animace podle čísla v nabídce"),
        ("R", "náhodná animace (z výběru pro střídání)"),
        ("↑ / ↓", "citlivost ± 3 dB"),
        ("A", "automatické střídání animací"),
        ("M", "mix dvou animací"),
        ("F, F11, dvojklik", "celá obrazovka"),
        ("Esc", "opustit celou obrazovku"),
        ("T", "připnout lištu (neschovávat)"),
        ("F1", "tato nápověda"),
    ];

    private float _alpha = 1f;
    private bool _showHelp;

    public bool IsVisible => _alpha > 0.01f;

    public void ToggleHelp() => _showHelp = !_showHelp;

    /// <param name="wanted">Lišta má být vidět (pohyb myši, stisk klávesy, připnutí).</param>
    public void Draw(float deltaTime, bool wanted)
    {
        var io = ImGui.GetIO();
        bool inUse = io.WantCaptureMouse || ImGui.IsPopupOpen(string.Empty, ImGuiPopupFlags.AnyPopupId);
        float target = wanted || inUse ? 1f : 0f;
        _alpha = target > _alpha
            ? MathF.Min(target, _alpha + deltaTime * 6f)
            : MathF.Max(target, _alpha - deltaTime * 2f);

        if (IsVisible)
        {
            ImGui.PushStyleVar(ImGuiStyleVar.Alpha, _alpha);
            float barHeight = DrawBar();
            if (app.Settings.MixEnabled)
            {
                DrawMixer(barHeight);
            }

            ImGui.PopStyleVar();
        }

        if (_showHelp)
        {
            DrawHelp();
        }
    }

    private float DrawBar()
    {
        var io = ImGui.GetIO();
        var style = ImGui.GetStyle();
        var settings = app.Settings;
        float height = ImGui.GetFrameHeight() + style.WindowPadding.Y * 2;

        ImGui.SetNextWindowPos(new Vector2(0, io.DisplaySize.Y - height));
        ImGui.SetNextWindowSize(new Vector2(io.DisplaySize.X, height));
        ImGui.SetNextWindowBgAlpha(0.85f);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0f);
        ImGui.Begin("##toolbar", BarFlags);
        ImGui.PopStyleVar();

        // Výběr a rychlé přepínání animací.
        if (ImGui.ArrowButton("##previous", ImGuiDir.Left))
        {
            app.PreviousAnimation();
        }

        Tooltip("Předchozí animace (←)");
        ImGui.SameLine();
        AnimationCombo("##animation", app.AnimationIndex, app.SelectAnimation, app.RandomAnimation, AnimationComboWidth, numbered: true);
        Tooltip("Výběr animace (klávesy 1–9, R náhodně)");
        ImGui.SameLine();
        if (ImGui.ArrowButton("##next", ImGuiDir.Right))
        {
            app.NextAnimation();
        }

        Tooltip("Další animace (→)");

        Divider();
        ImGui.Checkbox("Střídat", ref settings.AutoSwitch);
        Tooltip($"Automaticky přepínat animace – {FamilyFilterLabel(settings.AutoSwitchFamily)} – každých {settings.AutoSwitchSeconds:0} s"
            + $"{(settings.AutoSwitchOnBeat ? " na úder" : string.Empty)} (A).\nVýběr, interval a pořadí se nastavují v nabídce „…“.");

        Divider();
        if (ToggleButton("Mix", settings.MixEnabled))
        {
            app.ToggleMix();
        }

        Tooltip("Míchání dvou animací (M)");

        Divider();
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted("Citlivost");
        ImGui.SameLine();
        if (ImGui.Button("−"))
        {
            app.AdjustSensitivity(-VisualizerApp.SensitivityStep);
        }

        Tooltip("Snížit citlivost (↓)");
        ImGui.SameLine();
        FixedWidthText(FormatDecibels(settings.Sensitivity), 58f);
        if (ImGui.Button("+"))
        {
            app.AdjustSensitivity(VisualizerApp.SensitivityStep);
        }

        Tooltip("Zvýšit citlivost (↑)");

        // Pravá část: stav zdroje, celá obrazovka, další nastavení.
        string status = $"{app.Source.ProcessName ?? "Celý výstup"} · {io.Framerate:0} FPS";
        string fullscreenLabel = app.IsFullscreen ? "Okno" : "Celá obrazovka";
        float rightWidth = ImGui.CalcTextSize(status).X + ButtonWidth(fullscreenLabel) + ButtonWidth("…")
            + style.ItemSpacing.X * 2;
        float lastItemEnd = ImGui.GetItemRectMax().X - ImGui.GetWindowPos().X;
        ImGui.SameLine(MathF.Max(lastItemEnd + 24f, ImGui.GetWindowContentRegionMax().X - rightWidth));

        ImGui.AlignTextToFramePadding();
        ImGui.PushStyleColor(ImGuiCol.Text, app.SourceWarning is null ? ImGuiSetup.Dim : ImGuiSetup.Warning);
        ImGui.TextUnformatted(status);
        ImGui.PopStyleColor();
        Tooltip(app.SourceWarning ?? $"Zdroj zvuku: {app.SourceDescription}");

        ImGui.SameLine();
        if (ImGui.Button(fullscreenLabel))
        {
            app.ToggleFullscreen();
        }

        Tooltip("Celá obrazovka (F, F11 nebo dvojklik; Esc zpět)");
        ImGui.SameLine();
        if (ImGui.Button("…"))
        {
            ImGui.OpenPopup("more");
        }

        Tooltip("Další nastavení");
        DrawMorePopup();

        ImGui.End();
        return height;
    }

    private void DrawMorePopup()
    {
        if (!ImGui.BeginPopup("more"))
        {
            return;
        }

        var settings = app.Settings;

        ImGui.SeparatorText("Zdroj zvuku");
        bool fromApp = app.Source.ProcessName is not null;
        if (ImGui.RadioButton(app.ProcessName, fromApp) && !fromApp)
        {
            app.SwitchSource(CaptureTarget.ForProcess(app.ProcessName));
        }

        ImGui.SameLine();
        if (ImGui.RadioButton("Celý výstup PC", !fromApp) && fromApp)
        {
            app.SwitchSource(CaptureTarget.SystemOutput);
        }

        if (ImGui.Button("Znovu připojit"))
        {
            app.SwitchSource(app.Source.ProcessName is null ? CaptureTarget.SystemOutput : CaptureTarget.ForProcess(app.ProcessName));
        }

        Tooltip($"Např. když se {app.ProcessName} mezitím restartovalo");
        ColoredText(app.SourceDescription, ImGuiSetup.Dim);
        if (app.SourceWarning is { } warning)
        {
            ColoredText(warning, ImGuiSetup.Warning);
        }

        ImGui.SeparatorText("Střídání animací");
        ImGui.Checkbox("Automaticky střídat (A)", ref settings.AutoSwitch);
        ImGui.SetNextItemWidth(200f);
        if (ImGui.BeginCombo("Vybírat z", FamilyFilterLabel(settings.AutoSwitchFamily)))
        {
            if (ImGui.Selectable(FamilyFilterLabel(string.Empty), settings.AutoSwitchFamily.Length == 0))
            {
                settings.AutoSwitchFamily = string.Empty;
            }

            foreach (string family in VisualizationCatalog.Families)
            {
                if (ImGui.Selectable(VisualizationCatalog.FamilyLabel(family), settings.AutoSwitchFamily == family))
                {
                    settings.AutoSwitchFamily = family;
                }
            }

            ImGui.EndCombo();
        }

        Tooltip("Ze kterých animací vybírá střídání, náhodný výběr (R) i obměna v mixu");
        ImGui.SetNextItemWidth(200f);
        ImGui.SliderFloat("Interval", ref settings.AutoSwitchSeconds, 5f, 120f, "%.0f s");
        ImGui.Checkbox("Náhodné pořadí", ref settings.AutoSwitchRandom);
        ImGui.Checkbox("Přepínat na úder", ref settings.AutoSwitchOnBeat);
        Tooltip("Po uplynutí intervalu počká na nejbližší úder (nejdéle 2 s)");
        ImGui.SetNextItemWidth(200f);
        ImGui.SliderFloat("Délka přechodu", ref settings.TransitionSeconds, 0f, 5f, "%.1f s");
        Tooltip("Kaleidoskopy se mezi sebou plynule promění, ostatní animace se prolnou (0 = střih)");

        ImGui.SeparatorText("Lišta");
        ImGui.Checkbox("Připnout – neschovávat (T)", ref settings.ToolbarPinned);
        if (ImGui.Button("Klávesové zkratky (F1)"))
        {
            _showHelp = true;
            ImGui.CloseCurrentPopup();
        }

        ImGui.EndPopup();
    }

    private void DrawMixer(float barHeight)
    {
        var io = ImGui.GetIO();
        var settings = app.Settings;

        ImGui.SetNextWindowPos(new Vector2(io.DisplaySize.X - 10f, io.DisplaySize.Y - barHeight - 10f), ImGuiCond.Always, new Vector2(1f, 1f));
        ImGui.SetNextWindowBgAlpha(0.85f);
        ImGui.Begin("##mixer", PanelFlags);

        ImGui.SeparatorText("Mix");

        MixerLabel("A");
        ImGui.TextUnformatted(app.AnimationIndex >= 0 ? VisualizationCatalog.All[app.AnimationIndex].Name : "—");

        MixerLabel("B");
        AnimationCombo("##mixAnimation", app.MixAnimationIndex, app.SelectMixAnimation, app.RandomMixAnimation, MixerControlWidth, numbered: false);

        MixerLabel("Crossfader");
        ImGui.SetNextItemWidth(MixerControlWidth);
        int percentB = (int)MathF.Round(settings.Crossfader * 100f);
        ImGui.SliderFloat("##crossfader", ref settings.Crossfader, 0f, 1f, $"A {100 - percentB} : {percentB} B");
        if (ImGui.IsItemActive())
        {
            settings.AutoCrossfade = false;
        }

        Tooltip("Poměr mezi animacemi A a B");

        MixerLabel("Režim");
        ImGui.SetNextItemWidth(MixerControlWidth);
        if (ImGui.BeginCombo("##mixMode", settings.MixMode.Label()))
        {
            foreach (var mode in Enum.GetValues<BlendMode>())
            {
                if (ImGui.Selectable(mode.Label(), mode == settings.MixMode))
                {
                    settings.MixMode = mode;
                }

                Tooltip(mode.Description());
            }

            ImGui.EndCombo();
        }

        MixerLabel("Automaticky");
        ImGui.Checkbox("##autoCrossfade", ref settings.AutoCrossfade);
        Tooltip("Crossfader se sám pomalu přelévá mezi A a B");
        if (settings.AutoCrossfade)
        {
            ImGui.SameLine();
            ImGui.SetNextItemWidth(MixerControlWidth - ImGui.GetFrameHeight() - ImGui.GetStyle().ItemSpacing.X);
            ImGui.SliderFloat("##autoCrossfadeSeconds", ref settings.AutoCrossfadeSeconds, 2f, 60f, "perioda %.0f s");
        }

        MixerLabel("Obměňovat");
        ImGui.BeginDisabled(!settings.AutoCrossfade);
        ImGui.Checkbox("##autoMixRefresh", ref settings.AutoMixRefresh);
        ImGui.EndDisabled();
        Tooltip($"Při automatickém crossfaderu se úplně skrytá animace vymění za jinou ({FamilyFilterLabel(settings.AutoSwitchFamily)}),\ntakže každý návrat crossfaderu přinese novou kombinaci.");

        ImGui.End();
    }

    private void DrawHelp()
    {
        var io = ImGui.GetIO();
        ImGui.SetNextWindowPos(io.DisplaySize * 0.5f, ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));
        ImGui.SetNextWindowBgAlpha(0.95f);
        if (ImGui.Begin("Klávesové zkratky", ref _showHelp,
                ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoSavedSettings)
            && ImGui.BeginTable("##shortcuts", 2, ImGuiTableFlags.PadOuterX))
        {
            foreach (var (keys, action) in Shortcuts)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(keys);
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(action);
            }

            ImGui.EndTable();
        }

        ImGui.End();
    }

    /// <summary>Seznam animací seskupený podle rodin, nahoře volba „Náhodně“.</summary>
    private static void AnimationCombo(string id, int selected, Action<int> select, Action random, float width, bool numbered)
    {
        var all = VisualizationCatalog.All;
        ImGui.SetNextItemWidth(width);
        if (!ImGui.BeginCombo(id, selected >= 0 ? all[selected].Name : "—", ImGuiComboFlags.HeightLargest))
        {
            return;
        }

        if (ImGui.Selectable(numbered ? "Náhodně    (R)" : "Náhodně"))
        {
            random();
        }

        Tooltip("Náhodná animace z výběru pro střídání (nabídka „…“)");

        string? family = null;
        for (int i = 0; i < all.Count; i++)
        {
            if (all[i].Family != family)
            {
                family = all[i].Family;
                ImGui.SeparatorText(VisualizationCatalog.FamilyLabel(family));
            }

            string label = numbered && i < 9 ? $"{i + 1}    {all[i].Preset}" : all[i].Preset;
            if (ImGui.Selectable($"{label}##{i}", i == selected))
            {
                select(i);
            }

            if (i == selected)
            {
                ImGui.SetItemDefaultFocus();
            }
        }

        ImGui.EndCombo();
    }

    private static string FamilyFilterLabel(string family) =>
        string.IsNullOrEmpty(family) ? "Všechny animace" : VisualizationCatalog.FamilyLabel(family);

    private static bool ToggleButton(string label, bool active)
    {
        if (active)
        {
            ImGui.PushStyleColor(ImGuiCol.Button, ImGuiSetup.Accent);
        }

        bool clicked = ImGui.Button(label);
        if (active)
        {
            ImGui.PopStyleColor();
        }

        return clicked;
    }

    private static void Divider()
    {
        ImGui.SameLine(0f, 14f);
        ImGui.AlignTextToFramePadding();
        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.35f, 0.38f, 0.45f, 1f));
        ImGui.TextUnformatted("|");
        ImGui.PopStyleColor();
        ImGui.SameLine(0f, 14f);
    }

    /// <summary>Text vycentrovaný v pevné šířce, aby se okolní prvky při změně hodnoty neposouvaly.</summary>
    private static void FixedWidthText(string text, float width)
    {
        float start = ImGui.GetCursorPosX();
        ImGui.SetCursorPosX(start + (width - ImGui.CalcTextSize(text).X) / 2f);
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted(text);
        ImGui.SameLine(start + width);
    }

    private static void MixerLabel(string text)
    {
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted(text);
        ImGui.SameLine(MixerLabelWidth);
    }

    private static void ColoredText(string text, Vector4 color)
    {
        ImGui.PushStyleColor(ImGuiCol.Text, color);
        ImGui.PushTextWrapPos(ImGui.GetCursorPosX() + 360f);
        ImGui.TextUnformatted(text);
        ImGui.PopTextWrapPos();
        ImGui.PopStyleColor();
    }

    private static void Tooltip(string text)
    {
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.DelayShort))
        {
            ImGui.BeginTooltip();
            ImGui.TextUnformatted(text);
            ImGui.EndTooltip();
        }
    }

    private static float ButtonWidth(string label) => ImGui.CalcTextSize(label).X + ImGui.GetStyle().FramePadding.X * 2;

    private static string FormatDecibels(float value) => value switch
    {
        > 0f => $"+{value:0} dB",
        < 0f => $"−{-value:0} dB",
        _ => "0 dB",
    };
}
