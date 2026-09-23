using HTools.Core.Models;

namespace HTools.App.Services;

public interface IDialogService
{
    Task<string?> EditTextAsync(string title, string initialText);

    /// <summary>Shows the hotkey recorder. Returns null if cancelled, a <see cref="HotkeyDefinition"/>
    /// with <c>VirtualKey == 0</c> if the user chose "clear", or the captured combo otherwise.</summary>
    Task<HotkeyDefinition?> CaptureHotkeyAsync(string title);

    Task<bool> ShowImagePreviewAsync(string title, string imagePath, string closeLabel, string clearLabel);
}
