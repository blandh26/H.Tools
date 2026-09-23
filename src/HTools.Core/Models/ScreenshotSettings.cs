namespace HTools.Core.Models;

public sealed class ScreenshotSettings
{
    /// <summary>Null = use the built-in default (Ctrl+Alt+A).</summary>
    public HotkeyDefinition? Hotkey { get; set; }
}
