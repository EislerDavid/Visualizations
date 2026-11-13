using Silk.NET.OpenGL;
using Visualizations.App.Effects;
using Visualizations.App.Rendering;
using Visualizations.Core.Analysis;

namespace Visualizations.App.Mixing;

/// <summary>
/// Jeden kanál mixu: běžící animace a plynulý přechod z předchozí. Každá animace kreslí do vlastní
/// textury; během přechodu běží obě a výsledek se prolíná.
/// </summary>
internal sealed class Deck : IDisposable
{
    private readonly GL _gl;
    private readonly Compositor _compositor;
    private readonly RenderTarget _output;
    private Slot? _current;
    private Slot? _outgoing;
    private float _progress = 1f;
    private int _width;
    private int _height;

    public Deck(GL gl, Compositor compositor, int width, int height)
    {
        _gl = gl;
        _compositor = compositor;
        _width = width;
        _height = height;
        _output = new RenderTarget(gl, width, height);
    }

    /// <summary>Index animace v <see cref="VisualizationCatalog"/>, nebo −1 když deck nic nehraje.</summary>
    public int Index { get; private set; } = -1;

    public VisualizationInfo? Current => _current?.Info;

    /// <summary>Délka prolínání při změně animace; 0 = okamžitý střih.</summary>
    public float TransitionSeconds { get; set; } = 1.5f;

    public void Select(int index)
    {
        if (index == Index || (uint)index >= (uint)VisualizationCatalog.All.Count)
        {
            return;
        }

        // Animace, které to umí (kaleidoskopy mezi sebou), se plynule promění bez druhého obrazu.
        var info = VisualizationCatalog.All[index];
        if (_current?.Visualization is IMorphable morphable && morphable.TryMorphTo(info, TransitionSeconds))
        {
            _current.Info = info;
            Index = index;
            return;
        }

        // Přerušený přechod: dosud odcházející animace zmizí, z aktuální se stane odcházející.
        _outgoing?.Dispose();
        _outgoing = null;
        if (TransitionSeconds > 0f && _current is not null)
        {
            _outgoing = _current;
            _progress = 0f;
        }
        else
        {
            _current?.Dispose();
            _progress = 1f;
        }

        _current = new Slot(_gl, info, _width, _height);
        Index = index;
    }

    /// <summary>Uvolní animace; deck pak nic nehraje.</summary>
    public void Clear()
    {
        _outgoing?.Dispose();
        _current?.Dispose();
        _outgoing = null;
        _current = null;
        Index = -1;
    }

    public void Resize(int width, int height)
    {
        _width = width;
        _height = height;
        _output.Resize(width, height);
        _current?.Resize(width, height);
        _outgoing?.Resize(width, height);
    }

    /// <summary>Vykreslí deck a vrátí texturu s výsledkem.</summary>
    public uint Render(AudioFrame audio, float deltaTime, double time)
    {
        if (_current is null)
        {
            throw new InvalidOperationException("Deck nemá vybranou animaci.");
        }

        _current.Render(_gl, audio, deltaTime, time);
        if (_outgoing is null)
        {
            return _current.Target.Texture;
        }

        _progress += TransitionSeconds > 0f ? deltaTime / TransitionSeconds : 1f;
        if (_progress >= 1f)
        {
            _outgoing.Dispose();
            _outgoing = null;
            return _current.Target.Texture;
        }

        _outgoing.Render(_gl, audio, deltaTime, time);
        _output.Bind();
        float eased = _progress * _progress * (3f - 2f * _progress);
        _compositor.Draw(_outgoing.Target.Texture, _current.Target.Texture, eased, BlendMode.Crossfade);
        return _output.Texture;
    }

    public void Dispose()
    {
        Clear();
        _output.Dispose();
    }

    private sealed class Slot : IDisposable
    {
        public Slot(GL gl, VisualizationInfo info, int width, int height)
        {
            Info = info;
            Visualization = info.Create();
            Visualization.Load(gl);
            Visualization.Resize(width, height);
            Target = new RenderTarget(gl, width, height);
        }

        public VisualizationInfo Info { get; set; }

        public IVisualization Visualization { get; }

        public RenderTarget Target { get; }

        public void Resize(int width, int height)
        {
            Target.Resize(width, height);
            Visualization.Resize(width, height);
        }

        public void Render(GL gl, AudioFrame audio, float deltaTime, double time)
        {
            Target.Bind();
            gl.Disable(EnableCap.Blend);
            gl.ClearColor(0f, 0f, 0f, 1f);
            gl.Clear(ClearBufferMask.ColorBufferBit);
            Visualization.Render(new FrameContext(audio, deltaTime, time, Target));
        }

        public void Dispose()
        {
            Visualization.Dispose();
            Target.Dispose();
        }
    }
}
