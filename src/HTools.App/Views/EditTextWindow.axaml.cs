using Avalonia.Controls;
using Avalonia.Interactivity;

namespace HTools.App.Views;

public partial class EditTextWindow : Window
{
    public EditTextWindow()
    {
        InitializeComponent();
    }

    public string InitialText
    {
        get => ContentTextBox.Text ?? string.Empty;
        set => ContentTextBox.Text = value;
    }

    public string ContentLabelText
    {
        set => ContentLabel.Text = value;
    }

    public string SaveButtonText
    {
        set => SaveButton.Content = value;
    }

    public string CancelButtonText
    {
        set => CancelButton.Content = value;
    }

    private void OnSaveClick(object? sender, RoutedEventArgs e) => Close(ContentTextBox.Text ?? string.Empty);

    private void OnCancelClick(object? sender, RoutedEventArgs e) => Close(null);
}
