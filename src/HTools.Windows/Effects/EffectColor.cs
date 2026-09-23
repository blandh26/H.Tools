using System.Drawing;

namespace HTools.Windows.Effects;

/// <summary><paramref name="Name"/> is a stable identifier (used for persistence and as a loc key
/// suffix), not display text — the UI translates it via <c>MouseEffect.Color.{Name}</c>.</summary>
public readonly record struct EffectColor(string Name, Color Color)
{
    public static readonly EffectColor PlumPink = new("PlumPink", Color.FromArgb(219, 39, 90));
    public static readonly EffectColor Blue = new("Blue", Color.FromArgb(125, 211, 252));
    public static readonly EffectColor Green = new("Green", Color.FromArgb(34, 197, 94));
    public static readonly EffectColor Amber = new("Amber", Color.FromArgb(245, 158, 11));

    public static readonly EffectColor[] All =
    [
        PlumPink,
        Blue,
        Green,
        Amber,
    ];
}
