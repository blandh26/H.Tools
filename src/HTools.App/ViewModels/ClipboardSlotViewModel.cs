using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using HTools.App.Services;
using HTools.Core.Models;
using HTools.Core.Services;

namespace HTools.App.ViewModels;

public sealed partial class ClipboardSlotViewModel : LocalizedViewModelBase
{
    public ClipboardSlotViewModel(ClipboardSlot slot, ILocalizationService loc)
        : base(loc)
    {
        Slot = slot;
        RefreshImage();
    }

    public ClipboardSlot Slot { get; }

    public int Index => Slot.Index;

    public int DisplayDigit => Slot.DisplayDigit;

    /// <summary>The effective paste hotkey label: the slot's custom binding, or the Ctrl+{digit} default.</summary>
    public string HotkeyLabel => Slot.PasteHotkey is { VirtualKey: not 0 }
        ? HotkeyDefinitionFormatter.Format(Slot.PasteHotkey)
        : $"Ctrl+{DisplayDigit}";

    public string CopyHotkeyLabel => Slot.CopyHotkey is { VirtualKey: not 0 }
        ? HotkeyDefinitionFormatter.Format(Slot.CopyHotkey)
        : Loc.Translate("Clipboard.CopyHotkeyEmpty");

    public bool IsImage => Slot.IsImage;

    public Bitmap? ImageSource { get; private set; }

    public string Content => Slot.IsImage
        ? Loc.Translate("Clipboard.ImagePlaceholder")
        : (string.IsNullOrEmpty(Slot.Content) ? Loc.Translate("Clipboard.Empty") : Slot.Content);

    public bool HasContent => !Slot.IsEmpty;

    public string UpdatedAtText => Slot.UpdatedAt == default ? string.Empty : Slot.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss");

    public void Refresh()
    {
        RefreshImage();
        OnPropertyChanged(nameof(Content));
        OnPropertyChanged(nameof(HasContent));
        OnPropertyChanged(nameof(UpdatedAtText));
        OnPropertyChanged(nameof(IsImage));
        OnPropertyChanged(nameof(ImageSource));
        OnPropertyChanged(nameof(HotkeyLabel));
        OnPropertyChanged(nameof(CopyHotkeyLabel));
    }

    private void RefreshImage()
    {
        ImageSource?.Dispose();
        ImageSource = null;

        if (!Slot.IsImage)
        {
            return;
        }

        try
        {
            ImageSource = new Bitmap(Slot.ImagePath!);
        }
        catch (System.IO.IOException)
        {
            ImageSource = null;
        }
    }

    protected override void OnLanguageChanged()
    {
        OnPropertyChanged(nameof(Content));
        OnPropertyChanged(nameof(CopyHotkeyLabel));
    }
}
