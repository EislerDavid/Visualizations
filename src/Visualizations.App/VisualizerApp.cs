using ImGuiNET;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;
using Visualizations.App.Effects;
using Visualizations.App.Mixing;
using Visualizations.App.Rendering;
using Visualizations.App.Ui;
using Visualizations.Core.Analysis;
using Visualizations.Core.Audio;

namespace Visualizations.App;

/// <summary>
/// Okno vizualizací: zachytávání zvuku, dva decky (A a B) s mixem, toolbar a klávesové zkratky.
/// Veřejné metody jsou akce, které volá toolbar i klávesnice.
/// </summary>
internal sealed class VisualizerApp : IDisposable
{
    public const float SensitivityStep = 3f;
    public const float SensitivityLimit = 24f;

    private const double ToolbarIdleSeconds = 3.0;

    private readonly IWindow _window;
    private readonly Random _random = new();

    private AudioCapture _capture;
    private AudioFrame _audio;
    private GL? _gl;
    private IInputContext? _input;
    private IMouse? _mouse;
    private ImGuiController? _imgui;
    private Compositor? _compositor;
    private Deck? _deckA;
    private Deck? _deckB;
    private Toolbar? _toolbar;

    private double _time;
    private double _lastActivity;
    private float _autoSwitchElapsed;
    private float _crossfadePhase;
    private bool _autoCrossfadeRunning;
    private bool _fullscreen;
    private Vector2D<int> _windowedPosition;
    private Vector2D<int> _windowedSize;

    /// <param name="processName">Aplikace, jejíž zvuk se zachytává (výchozí Spotify).</param>
    /// <param name="systemOutput">Začít zachytáváním celého výstupu místo aplikace.</param>
    public VisualizerApp(string processName, bool systemOutput)
    {
        ProcessName = processName;
        Settings = Settings.Load();
        _capture = StartCapture(systemOutput ? CaptureTarget.SystemOutput : CaptureTarget.ForProcess(processName));
        _audio = new AudioFrame(_capture.SampleRate);

        _window = Window.Create(WindowOptions.Default with
        {
            Size = new Vector2D<int>(1024, 576),
            Title = "Visualizations",
            API = new GraphicsAPI(ContextAPI.OpenGL, ContextProfile.Core, ContextFlags.ForwardCompatible, new APIVersion(3, 3)),
            VSync = true,
        });
        _window.Load += OnLoad;
        _window.FramebufferResize += OnFramebufferResize;
        _window.Render += OnRender;
        _window.Closing += OnClosing;
    }

    public Settings Settings { get; }

    /// <summary>Název aplikace, ze které se dá zachytávat (nabídka „Zdroj zvuku“).</summary>
    public string ProcessName { get; }

    /// <summary>Skutečně použitý zdroj (po případném přepnutí na celý výstup).</summary>
    public CaptureTarget Source { get; private set; } = CaptureTarget.SystemOutput;

    public string SourceDescription => _capture.Description;

    /// <summary>Upozornění ke zdroji (aplikace neběží, zachytávání selhalo); jinak <c>null</c>.</summary>
    public string? SourceWarning { get; private set; }

    public int AnimationIndex => _deckA?.Index ?? -1;

    public int MixAnimationIndex => _deckB?.Index ?? -1;

    public bool IsFullscreen => _fullscreen;

    public void Run() => _window.Run();

    public void SelectAnimation(int index)
    {
        if (_deckA is null || (uint)index >= (uint)VisualizationCatalog.All.Count)
        {
            return;
        }

        _deckA.TransitionSeconds = Settings.TransitionSeconds;
        _deckA.Select(index);
        Settings.Animation = VisualizationCatalog.All[index].Name;
        _autoSwitchElapsed = 0f;
        _window.Title = $"Visualizations – {Settings.Animation}";
    }

    public void NextAnimation() => SelectAnimation((AnimationIndex + 1) % VisualizationCatalog.All.Count);

    public void PreviousAnimation()
    {
        int count = VisualizationCatalog.All.Count;
        SelectAnimation((AnimationIndex - 1 + count) % count);
    }

    public void RandomAnimation()
    {
        int count = VisualizationCatalog.All.Count;
        int index = count < 2 ? 0 : (AnimationIndex + 1 + _random.Next(count - 1)) % count;
        SelectAnimation(index);
    }

    public void SelectMixAnimation(int index)
    {
        if (_deckB is null || (uint)index >= (uint)VisualizationCatalog.All.Count)
        {
            return;
        }

        _deckB.TransitionSeconds = Settings.TransitionSeconds;
        _deckB.Select(index);
        Settings.MixAnimation = VisualizationCatalog.All[index].Name;
    }

    public void ToggleMix()
    {
        Settings.MixEnabled = !Settings.MixEnabled;
        ApplyMixState();
    }

    public void AdjustSensitivity(float deltaDecibels) =>
        Settings.Sensitivity = Math.Clamp(Settings.Sensitivity + deltaDecibels, -SensitivityLimit, SensitivityLimit);

    /// <summary>Přepíná mezi oknem a celou obrazovkou (okno bez rámečku přes celý monitor).</summary>
    public void ToggleFullscreen()
    {
        if (_fullscreen)
        {
            _window.WindowBorder = WindowBorder.Resizable;
            _window.Size = _windowedSize;
            _window.Position = _windowedPosition;
            _fullscreen = false;
            return;
        }

        if (_window.Monitor is not { } monitor)
        {
            return;
        }

        _windowedPosition = _window.Position;
        _windowedSize = _window.Size;
        _window.WindowBorder = WindowBorder.Hidden;
        _window.Position = monitor.Bounds.Origin;
        _window.Size = monitor.Bounds.Size;
        _fullscreen = true;
    }

    public void SwitchSource(CaptureTarget target)
    {
        _capture.Dispose();
        _capture = StartCapture(target);
        if (_capture.SampleRate != _audio.SampleRate)
        {
            _audio = new AudioFrame(_capture.SampleRate);
        }
    }

    public void Dispose()
    {
        _capture.Dispose();
        _window.Dispose();
    }

    private AudioCapture StartCapture(CaptureTarget target)
    {
        AudioCapture capture;
        try
        {
            capture = AudioCapture.StartAsync(target).GetAwaiter().GetResult();
            Source = target;
            SourceWarning = null;
        }
        catch (Exception e) when (target.ProcessName is not null)
        {
            // Aktivace process loopbacku je asynchronní (dokončí se na vlákně COM), proto lze bezpečně čekat.
            capture = AudioCapture.StartAsync(CaptureTarget.SystemOutput).GetAwaiter().GetResult();
            Source = CaptureTarget.SystemOutput;
            SourceWarning = $"{e.Message} Zachytávám celý výstup.";
        }

        capture.Stopped += error =>
        {
            if (error is not null)
            {
                SourceWarning = $"Zachytávání selhalo: {error.Message}";
            }
        };

        Console.WriteLine($"Zachytávám: {capture.Description}, {capture.SampleRate} Hz");
        if (SourceWarning is not null)
        {
            Console.WriteLine(SourceWarning);
        }

        return capture;
    }

    private void OnLoad()
    {
        _gl = GL.GetApi(_window);
        _input = _window.CreateInput();
        _imgui = new ImGuiController(_gl, _window, _input, ImGuiSetup.FontConfig(), ImGuiSetup.ConfigureIO);
        ImGuiSetup.ApplyStyle();

        var size = _window.FramebufferSize;
        _compositor = new Compositor(_gl);
        _deckA = new Deck(_gl, _compositor, size.X, size.Y);
        _deckB = new Deck(_gl, _compositor, size.X, size.Y);
        _toolbar = new Toolbar(this);

        SelectAnimation(Math.Max(VisualizationCatalog.IndexOf(Settings.Animation), 0));
        ApplyMixState();

        foreach (var keyboard in _input.Keyboards)
        {
            keyboard.KeyDown += OnKeyDown;
        }

        _mouse = _input.Mice.FirstOrDefault();
        if (_mouse is not null)
        {
            _mouse.MouseMove += (_, _) => _lastActivity = _time;
            _mouse.DoubleClick += OnDoubleClick;
        }
    }

    private void OnFramebufferResize(Vector2D<int> size)
    {
        _deckA?.Resize(size.X, size.Y);
        _deckB?.Resize(size.X, size.Y);
    }

    private void OnRender(double delta)
    {
        if (_gl is null || _imgui is null || _compositor is null || _deckA is null || _deckB is null || _toolbar is null)
        {
            return;
        }

        float deltaTime = (float)delta;
        _time += delta;
        UpdateAutomation(deltaTime);

        _audio.Spectrum.GainDecibels = Settings.Sensitivity;
        _audio.Update(_capture, deltaTime);

        // ImGui po sobě může nechat zapnutý scissor test – ten by ořezal i mazání plátna.
        _gl.Disable(EnableCap.ScissorTest);
        _gl.Disable(EnableCap.DepthTest);
        _gl.Disable(EnableCap.CullFace);

        _deckA.TransitionSeconds = _deckB.TransitionSeconds = Settings.TransitionSeconds;
        uint textureA = _deckA.Render(_audio, deltaTime, _time);
        bool mixing = Settings.MixEnabled && _deckB.Index >= 0;
        uint textureB = mixing ? _deckB.Render(_audio, deltaTime, _time) : textureA;

        var size = _window.FramebufferSize;
        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        _gl.Viewport(0, 0, (uint)size.X, (uint)size.Y);
        _compositor.Draw(textureA, textureB, mixing ? Settings.Crossfader : 0f, mixing ? Settings.MixMode : BlendMode.Crossfade);

        _imgui.Update(deltaTime);
        _toolbar.Draw(deltaTime, wanted: Settings.ToolbarPinned || _time - _lastActivity < ToolbarIdleSeconds);
        _imgui.Render();

        if (_mouse is not null)
        {
            var mode = _fullscreen && !_toolbar.IsVisible ? CursorMode.Hidden : CursorMode.Normal;
            if (_mouse.Cursor.CursorMode != mode)
            {
                _mouse.Cursor.CursorMode = mode;
            }
        }
    }

    private void UpdateAutomation(float deltaTime)
    {
        if (Settings.AutoSwitch)
        {
            _autoSwitchElapsed += deltaTime;
            if (_autoSwitchElapsed >= Settings.AutoSwitchSeconds)
            {
                if (Settings.AutoSwitchRandom)
                {
                    RandomAnimation();
                }
                else
                {
                    NextAnimation();
                }

                _autoSwitchElapsed = 0f;
            }
        }
        else
        {
            _autoSwitchElapsed = 0f;
        }

        if (Settings.MixEnabled && Settings.AutoCrossfade)
        {
            if (!_autoCrossfadeRunning)
            {
                // Navázat na aktuální polohu crossfaderu, ať neposkočí.
                _crossfadePhase = MathF.Acos(Math.Clamp(1f - 2f * Settings.Crossfader, -1f, 1f)) / (2f * MathF.PI);
                _autoCrossfadeRunning = true;
            }

            _crossfadePhase += deltaTime / MathF.Max(Settings.AutoCrossfadeSeconds, 0.1f);
            Settings.Crossfader = 0.5f - 0.5f * MathF.Cos(2f * MathF.PI * _crossfadePhase);
        }
        else
        {
            _autoCrossfadeRunning = false;
        }
    }

    private void ApplyMixState()
    {
        if (_deckB is null)
        {
            return;
        }

        if (!Settings.MixEnabled)
        {
            _deckB.Clear();
            return;
        }

        // B má smysl jen jako jiná animace než A; když by se shodovaly, vezme se následující.
        int index = VisualizationCatalog.IndexOf(Settings.MixAnimation);
        if (index < 0 || index == AnimationIndex)
        {
            index = (AnimationIndex + 1) % VisualizationCatalog.All.Count;
        }

        SelectMixAnimation(index);
    }

    private void OnKeyDown(IKeyboard keyboard, Key key, int scancode)
    {
        if (ImGui.GetIO().WantCaptureKeyboard)
        {
            return;
        }

        _lastActivity = _time;
        switch (key)
        {
            case Key.Left:
                PreviousAnimation();
                break;
            case Key.Right:
                NextAnimation();
                break;
            case Key.Up:
                AdjustSensitivity(SensitivityStep);
                break;
            case Key.Down:
                AdjustSensitivity(-SensitivityStep);
                break;
            case >= Key.Number1 and <= Key.Number9:
                SelectAnimation(key - Key.Number1);
                break;
            case Key.A:
                Settings.AutoSwitch = !Settings.AutoSwitch;
                break;
            case Key.M:
                ToggleMix();
                break;
            case Key.F or Key.F11:
                ToggleFullscreen();
                break;
            case Key.Escape when _fullscreen:
                ToggleFullscreen();
                break;
            case Key.T:
                Settings.ToolbarPinned = !Settings.ToolbarPinned;
                break;
            case Key.F1:
                _toolbar?.ToggleHelp();
                break;
        }
    }

    private void OnDoubleClick(IMouse mouse, MouseButton button, System.Numerics.Vector2 position)
    {
        if (button == MouseButton.Left && !ImGui.GetIO().WantCaptureMouse)
        {
            ToggleFullscreen();
        }
    }

    private void OnClosing()
    {
        Settings.Save();
        _deckA?.Dispose();
        _deckB?.Dispose();
        _compositor?.Dispose();
        _imgui?.Dispose();
        _input?.Dispose();
    }
}
