namespace HTools.Core.Models;

/// <summary>A user-customizable global hotkey combo. <c>VirtualKey</c> is a raw Win32 VK code (kept as
/// plain data here so Core doesn't need a Windows dependency); formatting/parsing key names is done
/// where the combo is captured/displayed.</summary>
public sealed class HotkeyDefinition
{
    public bool Ctrl { get; set; }

    public bool Alt { get; set; }

    public bool Shift { get; set; }

    public bool Win { get; set; }

    public int VirtualKey { get; set; }

    public bool HasModifier => Ctrl || Alt || Shift || Win;
}
