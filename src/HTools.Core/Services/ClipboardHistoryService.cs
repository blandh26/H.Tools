using HTools.Core.Models;

namespace HTools.Core.Services;

public sealed class ClipboardHistoryService : IClipboardHistoryService
{
    private readonly ClipboardSlot[] _slots;

    public ClipboardHistoryService()
    {
        _slots = new ClipboardSlot[IClipboardHistoryService.SlotCount];
        for (var i = 0; i < _slots.Length; i++)
        {
            _slots[i] = new ClipboardSlot { Index = i };
        }
    }

    public IReadOnlyList<ClipboardSlot> Slots => _slots;

    public event EventHandler? SlotsChanged;

    public ClipboardSlot GetSlot(int index)
    {
        if (index < 0 || index >= _slots.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        return _slots[index];
    }

    public string? SetSlotContent(int index, string content)
    {
        var slot = GetSlot(index);
        var evictedImagePath = slot.ImagePath;
        slot.Content = content;
        slot.ImagePath = null;
        slot.UpdatedAt = DateTime.Now;
        SlotsChanged?.Invoke(this, EventArgs.Empty);
        return evictedImagePath;
    }

    public string? SetSlotImage(int index, string imagePath)
    {
        var slot = GetSlot(index);
        var evictedImagePath = slot.ImagePath == imagePath ? null : slot.ImagePath;
        slot.ImagePath = imagePath;
        slot.Content = string.Empty;
        slot.UpdatedAt = DateTime.Now;
        SlotsChanged?.Invoke(this, EventArgs.Empty);
        return evictedImagePath;
    }

    public string? AddContent(string content)
    {
        var target = _slots.FirstOrDefault(s => s.IsEmpty)
            ?? _slots.OrderBy(s => s.UpdatedAt).First();

        var evictedImagePath = target.ImagePath;
        target.Content = content;
        target.ImagePath = null;
        target.UpdatedAt = DateTime.Now;
        SlotsChanged?.Invoke(this, EventArgs.Empty);
        return evictedImagePath;
    }

    public string? AddImage(string imagePath)
    {
        var target = _slots.FirstOrDefault(s => s.IsEmpty)
            ?? _slots.OrderBy(s => s.UpdatedAt).First();

        var evictedImagePath = target.ImagePath;
        target.ImagePath = imagePath;
        target.Content = string.Empty;
        target.UpdatedAt = DateTime.Now;
        SlotsChanged?.Invoke(this, EventArgs.Empty);
        return evictedImagePath;
    }

    public void LoadFrom(IEnumerable<ClipboardSlot> slots)
    {
        foreach (var slot in slots)
        {
            if (slot.Index >= 0 && slot.Index < _slots.Length)
            {
                _slots[slot.Index].Content = slot.Content;
                _slots[slot.Index].ImagePath = slot.ImagePath;
                _slots[slot.Index].UpdatedAt = slot.UpdatedAt;
                _slots[slot.Index].PasteHotkey = slot.PasteHotkey;
                _slots[slot.Index].CopyHotkey = slot.CopyHotkey;
            }
        }

        SlotsChanged?.Invoke(this, EventArgs.Empty);
    }

    public void SetSlotHotkeys(int index, HotkeyDefinition? pasteHotkey, HotkeyDefinition? copyHotkey)
    {
        var slot = GetSlot(index);
        slot.PasteHotkey = pasteHotkey;
        slot.CopyHotkey = copyHotkey;
        SlotsChanged?.Invoke(this, EventArgs.Empty);
    }
}
