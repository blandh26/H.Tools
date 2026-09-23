using Avalonia.Controls;
using HTools.App.ViewModels.KestrelServer;

namespace HTools.App.Views.KestrelServer;

public partial class UploadTabView : UserControl
{
    public UploadTabView()
    {
        InitializeComponent();
    }

    private async void OnChooseFolderClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var folders = await TopLevel.GetTopLevel(this)!.StorageProvider.OpenFolderPickerAsync(new Avalonia.Platform.Storage.FolderPickerOpenOptions { AllowMultiple = false });
        var path = folders.FirstOrDefault()?.Path.LocalPath;
        if (path is not null && DataContext is UploadTabViewModel vm) vm.UploadFolder = path;
    }
}
