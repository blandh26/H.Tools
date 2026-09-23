using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HTools.App.Services;
using HTools.Core.Services;
using HTools.Server.Modules;

namespace HTools.App.ViewModels.KestrelServer;

/// <summary>Standalone "static file server" tool — was previously one tab inside a combined
/// multi-function server page; each function now gets its own tool card per the user's request to
/// split every server function out independently.</summary>
public sealed partial class StaticFileTabViewModel : LocalizedViewModelBase, IAsyncDisposable
{
    private readonly StaticFileServerModule _module = new();
    private readonly AppSettingsContext _settings;
    private bool _suppressPersist;

    public StaticFileTabViewModel(ILocalizationService loc, AppSettingsContext settings)
        : base(loc)
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

    public string Title => Loc.Translate("Tool.StaticFileServer.Name");

    public string PortLabel => Loc.Translate("MockServer.Port");

    public string StartLabel => Loc.Translate("MockServer.Start");

    public string StopLabel => Loc.Translate("MockServer.Stop");

    public string RootFolderLabel => Loc.Translate("KestrelServer.RootFolder");

    /// <summary>"选择文件夹…"按钮文字，同时用作文件夹选择器的对话框标题。</summary>
    public string ChooseFolderLabel => Loc.Translate("KestrelServer.ChooseFolder");

    public string AccessUrl => $"http://localhost:{Port}/";

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

    partial void OnPortChanged(int value)
    {
        Persist();
        OnPropertyChanged(nameof(AccessUrl));
    }

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

    protected override void OnLanguageChanged()
    {
        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(PortLabel));
        OnPropertyChanged(nameof(StartLabel));
        OnPropertyChanged(nameof(StopLabel));
        OnPropertyChanged(nameof(RootFolderLabel));
        OnPropertyChanged(nameof(ChooseFolderLabel));
    }

    public async ValueTask DisposeAsync() => await _module.DisposeAsync();
}
