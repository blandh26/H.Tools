using Avalonia.Controls;
using HTools.App.Views;
using HTools.Core.Models;
using HTools.Core.Services;

namespace HTools.App.Services;

public sealed class DialogService : IDialogService
{
    private readonly ILocalizationService _loc;

    public DialogService(ILocalizationService loc)
    {
        _loc = loc;
    }

    public Window? Owner { get; set; }

    public async Task<string?> EditTextAsync(string title, string initialText)
    {
        if (Owner is null)
        {
            return null;
        }

        var window = new EditTextWindow
        {
            Title = title,
            ContentLabelText = _loc.Translate("Clipboard.Content"),
            SaveButtonText = _loc.Translate("Common.Save"),
            CancelButtonText = _loc.Translate("Common.Cancel"),
            InitialText = initialText,
        };

        return await window.ShowDialog<string?>(Owner);
    }

    public async Task<HotkeyDefinition?> CaptureHotkeyAsync(string title)
    {
        if (Owner is null)
        {
            return null;
        }

        var window = new HotkeyCaptureWindow { Title = title };
        window.SetTexts(
            _loc.Translate("Clipboard.CaptureInstruction"),
            _loc.Translate("Common.Clear"),
            _loc.Translate("Common.Cancel"));
        window.SetNoModifierError(_loc.Translate("Clipboard.NoModifierError"));
        window.SetUnsupportedKeyError(_loc.Translate("Clipboard.UnsupportedKeyError"));

        return await window.ShowDialog<HotkeyDefinition?>(Owner);
    }

    public async Task<bool> ShowImagePreviewAsync(string title, string imagePath, string closeLabel, string clearLabel)
    {
        if (Owner is null)
        {
            return false;
        }

        var window = new ImagePreviewWindow { Title = title };
        window.Load(imagePath, closeLabel, clearLabel);

        return await window.ShowDialog<bool>(Owner);
    }
}
