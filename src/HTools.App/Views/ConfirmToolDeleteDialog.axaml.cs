using Avalonia.Controls;
using Avalonia.Interactivity;
using HTools.Core.Services;

namespace HTools.App.Views;

public partial class ConfirmToolDeleteDialog : Window
{
    /// <summary>Design-time only (the XAML previewer needs a parameterless constructor).</summary>
    public ConfirmToolDeleteDialog()
        : this(new LocalizationService(), string.Empty)
    {
    }

    public ConfirmToolDeleteDialog(ILocalizationService loc, string name)
    {
        InitializeComponent();
        Title = loc.Translate("CustomTool.DeleteTitle");
        PromptText.Text = string.Format(loc.Translate("CustomTool.DeletePrompt"), name);
        ConfirmButton.Content = loc.Translate("Common.Delete");
        CancelButton.Content = loc.Translate("Common.Cancel");
    }

    private void OnConfirmClick(object? sender, RoutedEventArgs e) => Close(true);

    private void OnCancelClick(object? sender, RoutedEventArgs e) => Close(false);
}
