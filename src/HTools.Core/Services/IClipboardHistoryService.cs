using HTools.Core.Models;

namespace HTools.Core.Services;

public interface IClipboardHistoryService
{
    const int SlotCount = 10;

    IReadOnlyList<ClipboardSlot> Slots { get; }

    event EventHandler? SlotsChanged;

    ClipboardSlot GetSlot(int index);

    /// <summary>Directly sets one slot's text content (clearing any image), e.g. from manual edit or a
    /// hotkey-triggered capture. Returns the image path that was previously there, if any, so the caller
    /// can delete the now-orphaned file.</summary>
    string? SetSlotContent(int index, string content);

    /// <summary>Directly sets one slot's image (clearing any text), e.g. from a hotkey-triggered capture.
    /// Returns the image path that was previously there, if any (and different), so the caller can delete
    /// the now-orphaned file.</summary>
    string? SetSlotImage(int index, string imagePath);

    /// <summary>Places new text into the first empty slot, or overwrites the least-recently-updated slot.
    /// Returns the image path that was previously in the overwritten slot, if any, so the caller can delete
    /// the now-orphaned file.</summary>
    string? AddContent(string content);

    /// <summary>Places a new image into the first empty slot, or overwrites the least-recently-updated slot.
    /// Returns the image path that was previously in the overwritten slot, if any, so the caller can delete
    /// the now-orphaned file.</summary>
    string? AddImage(string imagePath);

    /// <summary>Replaces all slot state, e.g. when loading from persisted settings.</summary>
    void LoadFrom(IEnumerable<ClipboardSlot> slots);

    /// <summary>Updates one slot's hotkey bindings without touching its content.</summary>
    void SetSlotHotkeys(int index, HotkeyDefinition? pasteHotkey, HotkeyDefinition? copyHotkey);
}
