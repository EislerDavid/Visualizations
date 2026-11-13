namespace Visualizations.App.Effects;

/// <summary>
/// Plynulý přechod mezi dvěma presety jednoho enginu (<see cref="IMorphable"/>). Spojité parametry
/// interpoluje funkce <c>lerp</c>; nespojité (tvar, počet výsečí…) si engine prolne sám podle <see cref="Blend"/>.
/// </summary>
internal sealed class PresetMorph<T>(T initial, Func<T, T, float, T> lerp)
    where T : class
{
    private float _progress = 1f;
    private float _duration = 1f;

    /// <summary>Preset, ze kterého se přechází.</summary>
    public T From { get; private set; } = initial;

    /// <summary>Cílový preset (po dokončení přechodu aktuální).</summary>
    public T To { get; private set; } = initial;

    /// <summary>Interpolované spojité parametry pro tento snímek.</summary>
    public T Current { get; private set; } = initial;

    /// <summary>Váha cíle 0–1 (vyhlazená); 1 = přechod skončil.</summary>
    public float Blend { get; private set; } = 1f;

    public void Start(T target, float seconds)
    {
        // Přerušený přechod: pokračovat z aktuálního mezistavu, nespojité parametry od převažujícího presetu.
        From = _progress >= 1f ? To
            : Blend < 0.5f ? lerp(From, To, Blend)
            : lerp(To, From, 1f - Blend);
        To = target;
        _duration = seconds;
        _progress = seconds > 0f ? 0f : 1f;
        Update();
    }

    public void Advance(float deltaTime)
    {
        if (_progress < 1f)
        {
            _progress = MathF.Min(1f, _progress + deltaTime / _duration);
        }

        Update();
    }

    private void Update()
    {
        Blend = _progress * _progress * (3f - 2f * _progress);
        Current = _progress >= 1f ? To : lerp(From, To, Blend);
    }
}
