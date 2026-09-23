using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HTools.App.Services;
using HTools.Core.Services;
using HTools.Server.Models;
using HTools.Server.Modules;

namespace HTools.App.ViewModels.KestrelServer;

/// <summary>Standalone "Webhook receiver" tool — see <see cref="StaticFileTabViewModel"/> for why this
/// used to be a tab and now isn't.</summary>
public sealed partial class WebhookTabViewModel : LocalizedViewModelBase, IAsyncDisposable
{
    private readonly WebhookReceiverModule _module = new();
    private readonly AppSettingsContext _settings;
    private bool _suppressPersist;

    public WebhookTabViewModel(ILocalizationService loc, AppSettingsContext settings)
        : base(loc)
    {
        _settings = settings;
        _module.RequestReceived += (_, entry) => Dispatcher.UIThread.Post(() => RequestLog.Insert(0, entry));

        _suppressPersist = true;
        Port = settings.Current.KestrelServer.Webhook.Port;
        _suppressPersist = false;
    }

    public ObservableCollection<RequestLogEntry> RequestLog { get; } = [];

    public string Title => Loc.Translate("Tool.WebhookServer.Name");

    public string PortLabel => Loc.Translate("MockServer.Port");

    public string StartLabel => Loc.Translate("MockServer.Start");

    public string StopLabel => Loc.Translate("MockServer.Stop");

    public string RequestLogLabel => Loc.Translate("MockServer.RequestLog");

    public string ClearLogLabel => Loc.Translate("MockServer.ClearLog");

    /// <summary>"接收地址"——第三方服务把 Webhook 推送到这里。</summary>
    public string ReceiveUrlLabel => Loc.Translate("KestrelServer.ReceiveUrl");

    public string HintText => Loc.Translate("KestrelServer.WebhookHint");

    public string AccessUrl => $"http://localhost:{Port}/";

    [ObservableProperty]
    private int _port;

    [ObservableProperty]
    private bool _isRunning;

    [ObservableProperty]
    private string? _errorMessage;

    [RelayCommand]
    private void ClearLog() => RequestLog.Clear();

    partial void OnPortChanged(int value)
    {
        if (_suppressPersist)
        {
            return;
        }

        _settings.Current.KestrelServer.Webhook.Port = value;
        _settings.Save();
        OnPropertyChanged(nameof(AccessUrl));
    }

    [RelayCommand]
    private async Task ToggleAsync()
    {
        if (IsRunning)
        {
            await _module.StopAsync();
            IsRunning = false;
            return;
        }

        IsRunning = await _module.StartAsync(Port);
        ErrorMessage = IsRunning ? null : _module.LastError;
    }

    protected override void OnLanguageChanged()
    {
        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(PortLabel));
        OnPropertyChanged(nameof(StartLabel));
        OnPropertyChanged(nameof(StopLabel));
        OnPropertyChanged(nameof(RequestLogLabel));
        OnPropertyChanged(nameof(ClearLogLabel));
        OnPropertyChanged(nameof(ReceiveUrlLabel));
        OnPropertyChanged(nameof(HintText));
    }

    public async ValueTask DisposeAsync() => await _module.DisposeAsync();
}
