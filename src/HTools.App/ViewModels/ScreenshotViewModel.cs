using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HTools.App.Services;
using HTools.Core.Models;
using HTools.Core.Services;
using HTools.Windows.Screenshot;
using HTools.Windows.Services;

namespace HTools.App.ViewModels;

public sealed partial class ScreenshotViewModel : LocalizedViewModelBase
{
    private const string HotkeyTag = "screenshot-capture";

    private readonly ScreenshotService _service;
    private readonly GlobalHotkeyManager _hotkeys;
    private readonly AppSettingsContext _settings;
    private readonly IDialogService _dialogs;

    private int? _registeredHotkeyId;
    private bool _hotkeyFailed;

    public ScreenshotViewModel(
        ILocalizationService loc,
        ScreenshotService service,
        GlobalHotkeyManager hotkeys,
        AppSettingsContext settings,
        IDialogService dialogs)
        : base(loc)
    {
        _service = service;
        _hotkeys = hotkeys;
        _settings = settings;
        _dialogs = dialogs;

        _hotkeys.HotkeyTriggered += OnHotkeyTriggered;

        Hotkey = settings.Current.Screenshot.Hotkey ?? DefaultHotkey();
        RegisterHotkey();
    }

    public string Title => Loc.Translate("Screenshot.Title");

    public string CaptureNowLabel => Loc.Translate("Screenshot.CaptureNow");

    public string HotkeyLabel => Loc.Translate("Screenshot.Hotkey");

    public string Hint => Loc.Translate("Screenshot.Hint");

    public string HotkeyValueLabel => HotkeyDefinitionFormatter.Format(Hotkey);

    public string HotkeyWarning => _hotkeyFailed
        ? Loc.Translate("Screenshot.HotkeyConflictPrefix") + HotkeyValueLabel
        : string.Empty;

    public bool HasHotkeyWarning => _hotkeyFailed;

    [ObservableProperty]
    private HotkeyDefinition? _hotkey;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [RelayCommand]
    private void CaptureNow() => _service.Capture(OnResult, BuildOverlayTexts());

    private ScreenshotOverlayTexts BuildOverlayTexts() => new(
        Loc.Translate("Screenshot.OverlayHint"),
        Loc.Translate("Screenshot.SaveDialogFilter"),
        Loc.Translate("Screenshot.Tool.Rectangle"),
        Loc.Translate("Screenshot.Tool.RectangleFilled"),
        Loc.Translate("Screenshot.Tool.Ellipse"),
        Loc.Translate("Screenshot.Tool.EllipseFilled"),
        Loc.Translate("Screenshot.Tool.Line"),
        Loc.Translate("Screenshot.Tool.Arrow"),
        Loc.Translate("Screenshot.Tool.Freehand"),
        Loc.Translate("Screenshot.Tool.Text"),
        Loc.Translate("Screenshot.Tool.Mosaic"),
        Loc.Translate("Screenshot.Action.Undo"),
        Loc.Translate("Screenshot.Action.Pin"),
        Loc.Translate("Screenshot.Action.Copy"),
        Loc.Translate("Screenshot.Action.Save"),
        Loc.Translate("Screenshot.Action.Cancel"),
        Loc.Translate("Screenshot.CustomColor"));

    [RelayCommand]
    private async Task RecordHotkeyAsync()
    {
        var result = await _dialogs.CaptureHotkeyAsync(Title);
        if (result is null)
        {
            return;
        }

        UnregisterHotkey();
        Hotkey = result.VirtualKey == 0 ? DefaultHotkey() : result;
        RegisterHotkey();

        _settings.Current.Screenshot.Hotkey = result.VirtualKey == 0 ? null : result;
        _settings.Save();

        OnPropertyChanged(nameof(HotkeyValueLabel));
        OnPropertyChanged(nameof(HotkeyWarning));
        OnPropertyChanged(nameof(HasHotkeyWarning));
    }

    private static HotkeyDefinition DefaultHotkey() => new() { Ctrl = true, Alt = true, VirtualKey = 0x41 }; // Ctrl+Alt+A

    private void RegisterHotkey()
    {
        if (Hotkey is not { VirtualKey: not 0 } hotkey)
        {
            return;
        }

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

        var id = _hotkeys.Register(modifiers, (uint)hotkey.VirtualKey, HotkeyTag);
        _hotkeyFailed = id is null;
        _registeredHotkeyId = id;
    }

    private void UnregisterHotkey()
    {
        if (_registeredHotkeyId is int id)
        {
            _hotkeys.Unregister(id);
            _registeredHotkeyId = null;
        }
    }

    private void OnHotkeyTriggered(object? sender, HotkeyTriggeredEventArgs e)
    {
        if (e.Tag is string tag && tag == HotkeyTag)
        {
            _service.Capture(OnResult, BuildOverlayTexts());
        }
    }

    private void OnResult(ScreenshotResult result)
    {
        Dispatcher.UIThread.Post(() =>
        {
            StatusMessage = result.Outcome switch
            {
                ScreenshotOutcome.CopiedToClipboard => Loc.Translate("Screenshot.StatusCopied"),
                ScreenshotOutcome.SavedToFile => string.Format(Loc.Translate("Screenshot.StatusSaved"), result.SavedFilePath),
                ScreenshotOutcome.PinnedToScreen => Loc.Translate("Screenshot.StatusPinned"),
                _ => string.Empty,
            };
        });
    }

    protected override void OnLanguageChanged()
    {
        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(CaptureNowLabel));
        OnPropertyChanged(nameof(HotkeyLabel));
        OnPropertyChanged(nameof(Hint));
        OnPropertyChanged(nameof(HotkeyWarning));
    }
}
