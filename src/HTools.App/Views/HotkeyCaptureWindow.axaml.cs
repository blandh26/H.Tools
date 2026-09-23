using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using HTools.App.Services;
using HTools.Core.Models;

namespace HTools.App.Views;

public partial class HotkeyCaptureWindow : Window
{
    public HotkeyCaptureWindow()
    {
        InitializeComponent();
        Focusable = true;
        AttachedToVisualTree += (_, _) => Focus();
    }

    public void SetTexts(string instruction, string clearLabel, string cancelLabel)
    {
        InstructionText.Text = instruction;
        ClearButton.Content = clearLabel;
        CancelButton.Content = cancelLabel;
    }

    public void SetNoModifierError(string text) => _noModifierError = text;

    public void SetUnsupportedKeyError(string text) => _unsupportedKeyError = text;

    private string _noModifierError = "Hold at least one modifier key (Ctrl/Alt/Shift/Win).";
    private string _unsupportedKeyError = "That key isn't supported, try another.";

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.Key == Key.Escape)
        {
            Close(null);
            return;
        }

        if (HotkeyCaptureHelper.IsModifierKey(e.Key))
        {
            return;
        }

        var (ctrl, alt, shift, win) = HotkeyCaptureHelper.SplitModifiers(e.KeyModifiers);

        if (!(ctrl || alt || shift || win))
        {
            ShowError(_noModifierError);
            return;
        }

        if (!HotkeyCaptureHelper.TryGetVirtualKey(e.Key, out var virtualKey))
        {
            ShowError(_unsupportedKeyError);
            return;
        }

        var hotkey = new HotkeyDefinition { Ctrl = ctrl, Alt = alt, Shift = shift, Win = win, VirtualKey = virtualKey };
        CapturedText.Text = HotkeyDefinitionFormatter.Format(hotkey);
        Close(hotkey);
    }

    private void ShowError(string text)
    {
        ErrorText.Text = text;
        ErrorText.IsVisible = true;
    }

    private void OnClearClick(object? sender, RoutedEventArgs e) => Close(new HotkeyDefinition { VirtualKey = 0 });

    private void OnCancelClick(object? sender, RoutedEventArgs e) => Close(null);
}
