namespace HTools.Core.Models;

public sealed class MouseEffectSettings
{
    public bool Enabled { get; set; }

    public int GhostCount { get; set; } = 20;

    public int EffectDurationMs { get; set; } = 900;

    /// <summary>Matches <c>HTools.Windows.Effects.EffectColor.Name</c> (a stable identifier, not
    /// display text — see that type for why).</summary>
    public string ColorName { get; set; } = "PlumPink";

    /// <summary>Matches <c>HTools.Windows.Effects.MouseGhostShape</c>'s name (kept as a string so
    /// Core doesn't need to depend on the Windows-interop project).</summary>
    public string Shape { get; set; } = "PlumBlossom";
}
