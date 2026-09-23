using HTools.Core.Models;

namespace HTools.App.Services;

public static class HotkeyDefinitionFormatter
{
    public static string Format(HotkeyDefinition? hotkey)
    {
        if (hotkey is null || hotkey.VirtualKey == 0)
        {
            return string.Empty;
        }

        var parts = new List<string>();
        if (hotkey.Ctrl)
        {
            parts.Add("Ctrl");
        }

        if (hotkey.Alt)
        {
            parts.Add("Alt");
        }

        if (hotkey.Shift)
        {
            parts.Add("Shift");
        }

        if (hotkey.Win)
        {
            parts.Add("Win");
        }

        parts.Add(FormatKeyName(hotkey.VirtualKey));
        return string.Join("+", parts);
    }

    private static string FormatKeyName(int virtualKey) => virtualKey switch
    {
        >= 0x30 and <= 0x39 => ((char)virtualKey).ToString(),
        >= 0x41 and <= 0x5A => ((char)virtualKey).ToString(),
        >= 0x70 and <= 0x7B => $"F{virtualKey - 0x70 + 1}",
        0x20 => "Space",
        0x09 => "Tab",
        0x1B => "Esc",
        0x0D => "Enter",
        _ => $"Key{virtualKey}",
    };
}
