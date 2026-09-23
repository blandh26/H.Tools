using Avalonia.Input;

namespace HTools.App.Services;

/// <summary>Maps Avalonia's Key/KeyModifiers to the raw Win32 VK codes and modifier flags the hotkey
/// recorder needs. Covers digits, letters, F1-F12, and a few common extras — plenty for a hotkey combo.</summary>
public static class HotkeyCaptureHelper
{
    public static bool IsModifierKey(Key key) => key is Key.LeftCtrl or Key.RightCtrl
        or Key.LeftAlt or Key.RightAlt
        or Key.LeftShift or Key.RightShift
        or Key.LWin or Key.RWin;

    public static bool TryGetVirtualKey(Key key, out int virtualKey)
    {
        virtualKey = key switch
        {
            Key.D0 => 0x30,
            Key.D1 => 0x31,
            Key.D2 => 0x32,
            Key.D3 => 0x33,
            Key.D4 => 0x34,
            Key.D5 => 0x35,
            Key.D6 => 0x36,
            Key.D7 => 0x37,
            Key.D8 => 0x38,
            Key.D9 => 0x39,
            Key.A => 0x41,
            Key.B => 0x42,
            Key.C => 0x43,
            Key.D => 0x44,
            Key.E => 0x45,
            Key.F => 0x46,
            Key.G => 0x47,
            Key.H => 0x48,
            Key.I => 0x49,
            Key.J => 0x4A,
            Key.K => 0x4B,
            Key.L => 0x4C,
            Key.M => 0x4D,
            Key.N => 0x4E,
            Key.O => 0x4F,
            Key.P => 0x50,
            Key.Q => 0x51,
            Key.R => 0x52,
            Key.S => 0x53,
            Key.T => 0x54,
            Key.U => 0x55,
            Key.V => 0x56,
            Key.W => 0x57,
            Key.X => 0x58,
            Key.Y => 0x59,
            Key.Z => 0x5A,
            Key.F1 => 0x70,
            Key.F2 => 0x71,
            Key.F3 => 0x72,
            Key.F4 => 0x73,
            Key.F5 => 0x74,
            Key.F6 => 0x75,
            Key.F7 => 0x76,
            Key.F8 => 0x77,
            Key.F9 => 0x78,
            Key.F10 => 0x79,
            Key.F11 => 0x7A,
            Key.F12 => 0x7B,
            Key.Space => 0x20,
            Key.Tab => 0x09,
            Key.Escape => 0x1B,
            Key.Enter => 0x0D,
            _ => 0,
        };

        return virtualKey != 0;
    }

    public static (bool Ctrl, bool Alt, bool Shift, bool Win) SplitModifiers(KeyModifiers modifiers) => (
        modifiers.HasFlag(KeyModifiers.Control),
        modifiers.HasFlag(KeyModifiers.Alt),
        modifiers.HasFlag(KeyModifiers.Shift),
        modifiers.HasFlag(KeyModifiers.Meta));
}
