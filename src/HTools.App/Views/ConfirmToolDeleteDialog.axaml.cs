using Avalonia.Controls;
using Avalonia.Interactivity;

namespace HTools.App.Views;

public partial class ConfirmToolDeleteDialog : Window
{
    public ConfirmToolDeleteDialog()
        : this(string.Empty)
    {
    }

    public ConfirmToolDeleteDialog(string name)
    {
        InitializeComponent();
        PromptText.Text = $"确定删除“{name}”吗？";
    }

    private void OnConfirmClick(object? sender, RoutedEventArgs e) => Close(true);

    private void OnCancelClick(object? sender, RoutedEventArgs e) => Close(false);
}
