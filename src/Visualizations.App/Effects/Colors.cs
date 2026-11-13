using System.Numerics;

namespace Visualizations.App.Effects;

internal static class Colors
{
    /// <summary>HSV → RGB; odstín 0–1 dokola (0 červená, 1/3 zelená, 2/3 modrá).</summary>
    public static Vector3 Hsv(float hue, float saturation, float value)
    {
        float h = (hue - MathF.Floor(hue)) * 6f;
        float c = value * saturation;
        float x = c * (1f - MathF.Abs(h % 2f - 1f));
        var rgb = (int)h switch
        {
            0 => new Vector3(c, x, 0),
            1 => new Vector3(x, c, 0),
            2 => new Vector3(0, c, x),
            3 => new Vector3(0, x, c),
            4 => new Vector3(x, 0, c),
            _ => new Vector3(c, 0, x),
        };
        return rgb + new Vector3(value - c);
    }
}
