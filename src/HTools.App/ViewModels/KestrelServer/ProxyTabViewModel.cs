using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HTools.App.Services;
using HTools.Core.Services;
using HTools.Server.Models;
using HTools.Server.Modules;

namespace HTools.App.ViewModels.KestrelServer;

/// <summary>Standalone "reverse proxy" tool — see <see cref="StaticFileTabViewModel"/> for why this
/// used to be a tab and now isn't.</summary>
public sealed partial class ProxyTabViewModel : LocalizedViewModelBase, IAsyncDisposable
{
    private readonly ReverseProxyModule _module = new();
    private readonly AppSettingsContext _settings;
    private bool _suppressPersist;

    public ProxyTabViewModel(ILocalizationService loc, AppSettingsContext settings)
        : base(loc)
    {
        _settings = settings;
        _module.RequestReceived += (_, entry) => Dispatcher.UIThread.Post(() => RequestLog.Insert(0, entry));

        var saved = settings.Current.KestrelServer.Proxy;
        _suppressPersist = true;
        Port = saved.Port;
        TargetUrl = saved.TargetUrl;
        _suppressPersist = false;
    }

    public ObservableCollection<RequestLogEntry> RequestLog { get; } = [];

    public string Title => Loc.Translate("Tool.ReverseProxyServer.Name");

    public string PortLabel => Loc.Translate("MockServer.Port");

    public string StartLabel => Loc.Translate("MockServer.Start");

    public string StopLabel => Loc.Translate("MockServer.Stop");

    public string TargetUrlLabel => Loc.Translate("KestrelServer.TargetUrl");

    public string AccessUrl => $"http://localhost:{Port}/";

    [ObservableProperty]
    private int _port;

    [ObservableProperty]
    private string _targetUrl = string.Empty;

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

        _module.TargetBaseUrl = TargetUrl;
        IsRunning = await _module.StartAsync(Port);
        ErrorMessage = IsRunning ? null : _module.LastError;
    }

    partial void OnPortChanged(int value)
    {
        Persist();
        OnPropertyChanged(nameof(AccessUrl));
    }

    partial void OnTargetUrlChanged(string value) => Persist();

    private void Persist()
    {
        if (_suppressPersist)
        {
            return;
        }

        var s = _settings.Current.KestrelServer.Proxy;
        s.Port = Port;
        s.TargetUrl = TargetUrl;
        _settings.Save();
    }

    protected override void OnLanguageChanged()
    {
        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(PortLabel));
        OnPropertyChanged(nameof(StartLabel));
        OnPropertyChanged(nameof(StopLabel));
        OnPropertyChanged(nameof(TargetUrlLabel));
    }

    public async ValueTask DisposeAsync() => await _module.DisposeAsync();
}
