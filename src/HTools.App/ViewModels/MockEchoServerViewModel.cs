using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HTools.App.Services;
using HTools.Core.Services;
using HTools.Server.Models;
using HTools.Server.Modules;

namespace HTools.App.ViewModels;

/// <summary>Standalone "Mock server" tool — starts a server that returns one fixed configured response
/// to every request and logs what it received. Split out from the combined MockServer tool per the
/// user's request to make every server-adjacent function its own independent tool.</summary>
public sealed partial class MockEchoServerViewModel : LocalizedViewModelBase, IAsyncDisposable
{
    private readonly SimpleEchoServerModule _server = new();
    private readonly AppSettingsContext _settings;
    private bool _suppressPersist;

    public MockEchoServerViewModel(ILocalizationService loc, AppSettingsContext settings)
        : base(loc)
    {
        _settings = settings;
        _server.RequestReceived += (_, entry) => Dispatcher.UIThread.Post(() => RequestLog.Insert(0, entry));

        var saved = settings.Current.MockServer;
        _suppressPersist = true;
        Port = saved.Port;
        ResponseStatusCode = saved.ResponseStatusCode;
        ResponseBody = saved.ResponseBody;
        ResponseContentType = saved.ResponseContentType;
        _suppressPersist = false;
    }

    public ObservableCollection<RequestLogEntry> RequestLog { get; } = [];

    public string Title => Loc.Translate("Tool.MockEchoServer.Name");

    public string PortLabel => Loc.Translate("MockServer.Port");

    public string StartLabel => Loc.Translate("MockServer.Start");

    public string StopLabel => Loc.Translate("MockServer.Stop");

    public string ResponseStatusLabel => Loc.Translate("MockServer.ResponseStatus");

    public string ResponseBodyLabel => Loc.Translate("MockServer.ResponseBody");

    public string ResponseContentTypeLabel => "响应类型";

    public string AccessUrl => $"http://localhost:{Port}/";

    public string RequestLogLabel => Loc.Translate("MockServer.RequestLog");

    public string ClearLogLabel => Loc.Translate("MockServer.ClearLog");

    public string RunningStatus => IsRunning ? Loc.Translate("KestrelServer.Running") : Loc.Translate("KestrelServer.Stopped");

    [ObservableProperty]
    private int _port = 5190;

    [ObservableProperty]
    private bool _isRunning;

    [ObservableProperty]
    private int _responseStatusCode = 200;

    [ObservableProperty]
    private string _responseBody = "OK";

    [ObservableProperty]
    private string _responseContentType = "text/plain; charset=utf-8";

    [RelayCommand]
    private async Task ToggleServerAsync()
    {
        if (IsRunning)
        {
            await _server.StopAsync();
            IsRunning = false;
        }
        else
        {
            _server.ResponseStatusCode = ResponseStatusCode;
            _server.ResponseBody = ResponseBody;
            _server.ResponseContentType = ResponseContentType;
            IsRunning = await _server.StartAsync(Port);
        }

        OnPropertyChanged(nameof(RunningStatus));
    }

    [RelayCommand]
    private void ClearLog() => RequestLog.Clear();

    partial void OnPortChanged(int value)
    {
        Persist(s => s.Port = value);
        OnPropertyChanged(nameof(AccessUrl));
    }

    partial void OnResponseStatusCodeChanged(int value) => Persist(s => s.ResponseStatusCode = value);

    partial void OnResponseBodyChanged(string value) => Persist(s => s.ResponseBody = value);

    partial void OnResponseContentTypeChanged(string value) => Persist(s => s.ResponseContentType = value);

    private void Persist(Action<Core.Models.MockServerSettings> apply)
    {
        if (_suppressPersist)
        {
            return;
        }

        apply(_settings.Current.MockServer);
        _settings.Save();
    }

    protected override void OnLanguageChanged()
    {
        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(PortLabel));
        OnPropertyChanged(nameof(StartLabel));
        OnPropertyChanged(nameof(StopLabel));
        OnPropertyChanged(nameof(ResponseStatusLabel));
        OnPropertyChanged(nameof(ResponseBodyLabel));
        OnPropertyChanged(nameof(RequestLogLabel));
        OnPropertyChanged(nameof(ClearLogLabel));
        OnPropertyChanged(nameof(RunningStatus));
    }

    public async ValueTask DisposeAsync() => await _server.DisposeAsync();
}
