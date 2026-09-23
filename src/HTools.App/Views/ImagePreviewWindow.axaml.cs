using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;

namespace HTools.App.Views;

public partial class ImagePreviewWindow : Window
{
    public ImagePreviewWindow()
    {
        InitializeComponent();
    }

    public void Load(string imagePath, string closeLabel, string clearLabel)
    {
        try
        {
            PreviewImage.Source = new Bitmap(imagePath);
        }
        catch (System.IO.IOException)
        {
            // Leave the preview blank if the file went missing.
        }

        CloseButton.Content = closeLabel;
        ClearButton.Content = clearLabel;
    }

    private void OnClearClick(object? sender, RoutedEventArgs e) => Close(true);

    private void OnCloseClick(object? sender, RoutedEventArgs e) => Close(false);
}
