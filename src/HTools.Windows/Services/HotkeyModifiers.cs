namespace HTools.Windows.Services;

/// <summary>Matches the Win32 MOD_* RegisterHotKey flags exactly, so it can be cast straight to the
/// native call without translation.</summary>
[Flags]
public enum HotkeyModifiers : uint
{
    None = 0,
    Alt = 0x0001,
    Control = 0x0002,
    Shift = 0x0004,
    Win = 0x0008,
}
