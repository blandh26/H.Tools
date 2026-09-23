using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HTools.App.Services;
using HTools.Core.Services;
using HTools.Server.Models;
using HTools.Server.Modules;

namespace HTools.App.ViewModels.KestrelServer;

/// <summary>Standalone "latency simulator" tool — see <see cref="StaticFileTabViewModel"/> for why this
/// used to be a tab and now isn't.</summary>
public sealed partial class LatencyTabViewModel : LocalizedViewModelBase, IAsyncDisposable
{
    private readonly LatencySimulatorModule _module = new();
    private readonly AppSettingsContext _settings;
    private bool _suppressPersist;

    public LatencyTabViewModel(ILocalizationService loc, AppSettingsContext settings)
        : base(loc)
    {
        _settings = settings;
        _module.RequestReceived += (_, entry) => Dispatcher.UIThread.Post(() => RequestLog.Insert(0, entry));

        var saved = settings.Current.KestrelServer.Latency;
        _suppressPersist = true;
        Port = saved.Port;
        DelayMs = saved.DelayMs;
        TargetUrl = saved.TargetUrl;
        _suppressPersist = false;
    }

    public ObservableCollection<RequestLogEntry> RequestLog { get; } = [];

    public string Title => Loc.Translate("Tool.LatencyServer.Name");

    public string PortLabel => Loc.Translate("MockServer.Port");

    public string StartLabel => Loc.Translate("MockServer.Start");

    public string StopLabel => Loc.Translate("MockServer.Stop");

    public string DelayMsLabel => Loc.Translate("KestrelServer.DelayMs");

    public string TargetUrlLabel => Loc.Translate("KestrelServer.TargetUrl");

    [ObservableProperty]
    private int _port;

    [ObservableProperty]
    private int _delayMs;

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

        _module.DelayMs = DelayMs;
        _module.TargetUrl = string.IsNullOrWhiteSpace(TargetUrl) ? null : TargetUrl;
        IsRunning = await _module.StartAsync(Port);
        ErrorMessage = IsRunning ? null : _module.LastError;
    }

    partial void OnPortChanged(int value) => Persist();

    partial void OnDelayMsChanged(int value) => Persist();

    partial void OnTargetUrlChanged(string value) => Persist();

    private void Persist()
    {
        if (_suppressPersist)
        {
            return;
        }

        var s = _settings.Current.KestrelServer.Latency;
        s.Port = Port;
        s.DelayMs = DelayMs;
        s.TargetUrl = TargetUrl;
        _settings.Save();
    }

    protected override void OnLanguageChanged()
    {
        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(PortLabel));
        OnPropertyChanged(nameof(StartLabel));
        OnPropertyChanged(nameof(StopLabel));
        OnPropertyChanged(nameof(DelayMsLabel));
        OnPropertyChanged(nameof(TargetUrlLabel));
    }

    public async ValueTask DisposeAsync() => await _module.DisposeAsync();
}
