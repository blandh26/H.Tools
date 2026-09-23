using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using HTools.App.Services;
using HTools.Core.Models;
using HTools.Core.Services;
using HTools.Windows.Interop;
using HTools.Windows.Services;

namespace HTools.App.ViewModels;

public sealed partial class ClipboardViewModel : LocalizedViewModelBase
{
    private static readonly string ImagesFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HTools", "ClipboardImages");

    private readonly IClipboardHistoryService _history;
    private readonly GlobalHotkeyManager _hotkeys;
    private readonly AppSettingsContext _settings;
    private readonly IDialogService _dialogs;

    private readonly Dictionary<int, int> _pasteHotkeyIds = new();
    private readonly Dictionary<int, int> _copyHotkeyIds = new();

    // Keyed by slot index; value is the formatted label of the combo that failed to register
    // (which may be a custom binding, not necessarily the default Ctrl+{digit}).
    private readonly Dictionary<int, string> _failedPasteLabels = new();
    private readonly Dictionary<int, string> _failedCopyLabels = new();

    public ClipboardViewModel(
        ILocalizationService loc,
        IClipboardHistoryService history,
        GlobalHotkeyManager hotkeys,
        AppSettingsContext settings,
        IDialogService dialogs)
        : base(loc)
    {
        _history = history;
        _hotkeys = hotkeys;
        _settings = settings;
        _dialogs = dialogs;

        history.LoadFrom(settings.Current.ClipboardSlots);

        Slots = new ObservableCollection<ClipboardSlotViewModel>(
            history.Slots.Select(slot => new ClipboardSlotViewModel(slot, Loc)));

        _history.SlotsChanged += OnHistorySlotsChanged;
        _hotkeys.HotkeyTriggered += OnHotkeyTriggered;

        foreach (var slot in _history.Slots)
        {
            RegisterPasteHotkey(slot);
            if (slot.CopyHotkey is { VirtualKey: not 0 })
            {
                RegisterCopyHotkey(slot);
            }
        }
    }

    public ObservableCollection<ClipboardSlotViewModel> Slots { get; }

    public string Title => Loc.Translate("Clipboard.Title");

    public string HotkeyHint => Loc.Translate("Clipboard.HotkeyHint");

    public string NewLabel => Loc.Translate("Common.New");

    public string PasteHotkeyTooltip => Loc.Translate("Clipboard.PasteHotkeyTooltip");

    public string CopyHotkeyTooltip => Loc.Translate("Clipboard.CopyHotkeyTooltip");

    /// <summary>Non-empty when one or more hotkeys failed to register — almost always because another
    /// running app already owns that exact combo.</summary>
    public string HotkeyWarning
    {
        get
        {
            if (_failedPasteLabels.Count == 0 && _failedCopyLabels.Count == 0)
            {
                return string.Empty;
            }

            var labels = _failedPasteLabels.Values.Concat(_failedCopyLabels.Values);
            return Loc.Translate("Clipboard.HotkeyConflictPrefix") + string.Join(", ", labels);
        }
    }

    public bool HasHotkeyWarning => HotkeyWarning.Length > 0;

    [RelayCommand]
    private async Task EditSlotAsync(ClipboardSlotViewModel slot)
    {
        if (slot.IsImage)
        {
            var clear = await _dialogs.ShowImagePreviewAsync(
                slot.HotkeyLabel,
                slot.Slot.ImagePath!,
                Loc.Translate("Common.Cancel"),
                Loc.Translate("Common.Clear"));

            if (clear)
            {
                var oldPath = slot.Slot.ImagePath;
                _history.SetSlotContent(slot.Index, string.Empty);
                TryDeleteImage(oldPath);
            }

            return;
        }

        var title = $"{Loc.Translate("Clipboard.EditTitle")} · {slot.HotkeyLabel}";
        var result = await _dialogs.EditTextAsync(title, slot.Slot.Content);
        if (result is not null)
        {
            var evicted = _history.SetSlotContent(slot.Index, result);
            TryDeleteImage(evicted);
        }
    }

    [RelayCommand]
    private async Task NewEntryAsync()
    {
        if (ClipboardInterop.ContainsImage())
        {
            var imagePath = ClipboardInterop.CaptureImageToFile(ImagesFolder);
            if (imagePath is not null)
            {
                var evicted = _history.AddImage(imagePath);
                TryDeleteImage(evicted);
                return;
            }
        }

        var prefill = ClipboardInterop.GetText() ?? string.Empty;
        var result = await _dialogs.EditTextAsync(Loc.Translate("Clipboard.EditTitle"), prefill);
        if (result is not null)
        {
            var evicted = _history.AddContent(result);
            TryDeleteImage(evicted);
        }
    }

    [RelayCommand]
    private async Task RecordPasteHotkeyAsync(ClipboardSlotViewModel slotVm)
    {
        var result = await _dialogs.CaptureHotkeyAsync($"{Loc.Translate("Clipboard.Title")} · {slotVm.HotkeyLabel}");
        if (result is null)
        {
            return;
        }

        var slot = slotVm.Slot;
        UnregisterPasteHotkey(slot.Index);
        _failedPasteLabels.Remove(slot.Index);

        slot.PasteHotkey = result.VirtualKey == 0 ? null : result;
        RegisterPasteHotkey(slot);
        RefreshSlotAndWarning(slotVm);
    }

    [RelayCommand]
    private async Task RecordCopyHotkeyAsync(ClipboardSlotViewModel slotVm)
    {
        var result = await _dialogs.CaptureHotkeyAsync($"{Loc.Translate("Clipboard.Title")} · {slotVm.HotkeyLabel}");
        if (result is null)
        {
            return;
        }

        var slot = slotVm.Slot;
        UnregisterCopyHotkey(slot.Index);
        _failedCopyLabels.Remove(slot.Index);

        slot.CopyHotkey = result.VirtualKey == 0 ? null : result;
        if (slot.CopyHotkey is not null)
        {
            RegisterCopyHotkey(slot);
        }

        RefreshSlotAndWarning(slotVm);
    }

    private void RegisterPasteHotkey(ClipboardSlot slot)
    {
        var (modifiers, virtualKey) = ResolvePasteCombo(slot);
        var id = _hotkeys.Register(modifiers, virtualKey, (slot.Index, IsCopy: false));
        if (id is int registeredId)
        {
            _pasteHotkeyIds[slot.Index] = registeredId;
        }
        else
        {
            _failedPasteLabels[slot.Index] = HotkeyDefinitionFormatter.Format(
                slot.PasteHotkey ?? new HotkeyDefinition { Ctrl = true, VirtualKey = (int)virtualKey });
        }
    }

    private void RegisterCopyHotkey(ClipboardSlot slot)
    {
        if (slot.CopyHotkey is not { VirtualKey: not 0 } hotkey)
        {
            return;
        }

        var modifiers = ToModifiers(hotkey);
        var id = _hotkeys.Register(modifiers, (uint)hotkey.VirtualKey, (slot.Index, IsCopy: true));
        if (id is int registeredId)
        {
            _copyHotkeyIds[slot.Index] = registeredId;
        }
        else
        {
            _failedCopyLabels[slot.Index] = HotkeyDefinitionFormatter.Format(hotkey);
        }
    }

    private void UnregisterPasteHotkey(int index)
    {
        if (_pasteHotkeyIds.Remove(index, out var id))
        {
            _hotkeys.Unregister(id);
        }
    }

    private void UnregisterCopyHotkey(int index)
    {
        if (_copyHotkeyIds.Remove(index, out var id))
        {
            _hotkeys.Unregister(id);
        }
    }

    private static (HotkeyModifiers Modifiers, uint VirtualKey) ResolvePasteCombo(ClipboardSlot slot)
    {
        if (slot.PasteHotkey is { VirtualKey: not 0 } custom)
        {
            return (ToModifiers(custom), (uint)custom.VirtualKey);
        }

        var virtualKey = (uint)(slot.Index == 9 ? '0' : '1' + slot.Index);
        return (HotkeyModifiers.Control, virtualKey);
    }

    private static HotkeyModifiers ToModifiers(HotkeyDefinition hotkey)
    {
        var modifiers = HotkeyModifiers.None;
        if (hotkey.Ctrl)
        {
            modifiers |= HotkeyModifiers.Control;
        }

        if (hotkey.Alt)
        {
            modifiers |= HotkeyModifiers.Alt;
        }

        if (hotkey.Shift)
        {
            modifiers |= HotkeyModifiers.Shift;
        }

        if (hotkey.Win)
        {
            modifiers |= HotkeyModifiers.Win;
        }

        return modifiers;
    }

    private void OnHotkeyTriggered(object? sender, HotkeyTriggeredEventArgs e)
    {
        if (e.Tag is not ValueTuple<int, bool> tag)
        {
            return;
        }

        var (index, isCopy) = tag;
        if (isCopy)
        {
            OnCopyHotkey(index);
        }
        else
        {
            OnPasteHotkey(index);
        }
    }

    private void OnPasteHotkey(int index)
    {
        var slot = _history.GetSlot(index);
        if (slot.IsEmpty)
        {
            return;
        }

        if (slot.IsImage)
        {
            if (ClipboardInterop.SetImageFromFile(slot.ImagePath!))
            {
                PasteSimulator.SendCtrlV();
            }
        }
        else if (ClipboardInterop.SetText(slot.Content))
        {
            PasteSimulator.SendCtrlV();
        }
    }

    private void OnCopyHotkey(int index)
    {
        var oldImagePath = _history.GetSlot(index).ImagePath;

        if (ClipboardInterop.ContainsImage())
        {
            var imagePath = ClipboardInterop.CaptureImageToFile(ImagesFolder);
            if (imagePath is not null)
            {
                _history.SetSlotImage(index, imagePath);
                TryDeleteImage(oldImagePath == imagePath ? null : oldImagePath);
                return;
            }
        }

        var text = ClipboardInterop.GetText();
        if (text is not null)
        {
            _history.SetSlotContent(index, text);
            TryDeleteImage(oldImagePath);
        }
    }

    private void OnHistorySlotsChanged(object? sender, EventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            foreach (var slotViewModel in Slots)
            {
                slotViewModel.Refresh();
            }

            PersistSlots();
        });
    }

    private void RefreshSlotAndWarning(ClipboardSlotViewModel slotVm)
    {
        slotVm.Refresh();
        OnPropertyChanged(nameof(HotkeyWarning));
        OnPropertyChanged(nameof(HasHotkeyWarning));
        PersistSlots();
    }

    private void PersistSlots()
    {
        _settings.Current.ClipboardSlots = _history.Slots
            .Select(s => new ClipboardSlot
            {
                Index = s.Index,
                Content = s.Content,
                ImagePath = s.ImagePath,
                UpdatedAt = s.UpdatedAt,
                PasteHotkey = s.PasteHotkey,
                CopyHotkey = s.CopyHotkey,
            })
            .ToList();
        _settings.Save();
    }

    private static void TryDeleteImage(string? path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException)
        {
            // Best-effort cleanup; a locked/missing file just leaves a harmless orphan.
        }
    }

    protected override void OnLanguageChanged()
    {
        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(HotkeyHint));
        OnPropertyChanged(nameof(NewLabel));
        OnPropertyChanged(nameof(HotkeyWarning));
        OnPropertyChanged(nameof(PasteHotkeyTooltip));
        OnPropertyChanged(nameof(CopyHotkeyTooltip));
    }
}
