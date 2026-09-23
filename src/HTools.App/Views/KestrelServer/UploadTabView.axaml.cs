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
        if (DataContext is not UploadTabViewModel vm) return;

        // 系统文件夹选择器的标题也跟随当前界面语言
        var folders = await TopLevel.GetTopLevel(this)!.StorageProvider.OpenFolderPickerAsync(
            new Avalonia.Platform.Storage.FolderPickerOpenOptions { Title = vm.ChooseFolderLabel, AllowMultiple = false });
        var path = folders.FirstOrDefault()?.Path.LocalPath;
        if (path is not null) vm.UploadFolder = path;
    }
}
