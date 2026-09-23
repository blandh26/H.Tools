namespace HTools.Core.Models;

/// <summary>One of the 10 clipboard slots. Defaults to Ctrl+1..Ctrl+9, Ctrl+0 for paste, but both the
/// paste and the capture-to-slot hotkeys are user-customizable.</summary>
public sealed class ClipboardSlot
{
    public int Index { get; set; }

    public string Content { get; set; } = string.Empty;

    /// <summary>Path to a saved PNG when this slot holds an image instead of text. Mutually exclusive
    /// with <see cref="Content"/> — setting one clears the other.</summary>
    public string? ImagePath { get; set; }

    public DateTime UpdatedAt { get; set; }

    /// <summary>Null = use the default Ctrl+{digit} for this slot's index.</summary>
    public HotkeyDefinition? PasteHotkey { get; set; }

    /// <summary>Null = no capture hotkey configured for this slot (opt-in, unlike paste).</summary>
    public HotkeyDefinition? CopyHotkey { get; set; }

    /// <summary>The digit shown to the user for this slot: 1-9 then 0 for index 9.</summary>
    public int DisplayDigit => (Index + 1) % 10;

    public bool IsImage => !string.IsNullOrEmpty(ImagePath);

    public bool IsEmpty => string.IsNullOrEmpty(Content) && string.IsNullOrEmpty(ImagePath);
}
