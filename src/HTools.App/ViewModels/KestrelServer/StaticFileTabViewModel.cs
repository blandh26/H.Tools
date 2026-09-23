using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HTools.App.Services;
using HTools.Server.Modules;

namespace HTools.App.ViewModels.KestrelServer;

public sealed partial class StaticFileTabViewModel : ObservableObject, IAsyncDisposable
{
    private readonly StaticFileServerModule _module = new();
    private readonly AppSettingsContext _settings;
    private bool _suppressPersist;

    public StaticFileTabViewModel(AppSettingsContext settings)
    {
        _settings = settings;
        var saved = settings.Current.KestrelServer.StaticFile;

        _suppressPersist = true;
        Port = saved.Port;
        RootFolder = string.IsNullOrEmpty(saved.RootFolder)
            ? Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            : saved.RootFolder;
        _suppressPersist = false;
    }

    [ObservableProperty]
    private int _port;

    [ObservableProperty]
    private string _rootFolder = string.Empty;

    [ObservableProperty]
    private bool _isRunning;

    [ObservableProperty]
    private string? _errorMessage;

    [RelayCommand]
    private async Task ToggleAsync()
    {
        if (IsRunning)
        {
            await _module.StopAsync();
            IsRunning = false;
            return;
        }

        _module.RootFolder = RootFolder;
        IsRunning = await _module.StartAsync(Port);
        ErrorMessage = IsRunning ? null : _module.LastError;
    }

    partial void OnPortChanged(int value) => Persist();

    partial void OnRootFolderChanged(string value) => Persist();

    private void Persist()
    {
        if (_suppressPersist)
        {
            return;
        }

        var s = _settings.Current.KestrelServer.StaticFile;
        s.Port = Port;
        s.RootFolder = RootFolder;
        _settings.Save();
    }

    public async ValueTask DisposeAsync() => await _module.DisposeAsync();
}
