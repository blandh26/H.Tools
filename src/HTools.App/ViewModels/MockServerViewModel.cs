using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HTools.App.Services;
using HTools.Core.Services;
using HTools.Server.Http;
using HTools.Server.Models;
using HTools.Server.Modules;

namespace HTools.App.ViewModels;

public sealed partial class MockServerViewModel : LocalizedViewModelBase, IAsyncDisposable
{
    private readonly SimpleEchoServerModule _server = new();
    private readonly HttpRequestSender _sender = new();
    private readonly AppSettingsContext _settings;
    private bool _suppressPersist;

    public MockServerViewModel(ILocalizationService loc, AppSettingsContext settings)
        : base(loc)
    {
        _settings = settings;
        _server.RequestReceived += (_, entry) => Dispatcher.UIThread.Post(() => RequestLog.Insert(0, entry));

        var saved = settings.Current.MockServer;
        _suppressPersist = true;
        Port = saved.Port;
        ResponseStatusCode = saved.ResponseStatusCode;
        ResponseBody = saved.ResponseBody;
        ClientUrl = $"http://localhost:{saved.Port}";
        _suppressPersist = false;
    }

    public ObservableCollection<RequestLogEntry> RequestLog { get; } = [];

    public string[] HttpMethods { get; } = ["GET", "POST", "PUT", "PATCH", "DELETE"];

    public string Title => Loc.Translate("MockServer.Title");

    public string ServerTabLabel => Loc.Translate("MockServer.ServerTab");

    public string ClientTabLabel => Loc.Translate("MockServer.ClientTab");

    public string PortLabel => Loc.Translate("MockServer.Port");

    public string StartLabel => Loc.Translate("MockServer.Start");

    public string StopLabel => Loc.Translate("MockServer.Stop");

    public string ResponseStatusLabel => Loc.Translate("MockServer.ResponseStatus");

    public string ResponseBodyLabel => Loc.Translate("MockServer.ResponseBody");

    public string RequestLogLabel => Loc.Translate("MockServer.RequestLog");

    public string ClearLogLabel => Loc.Translate("MockServer.ClearLog");

    public string MethodLabel => Loc.Translate("MockServer.Method");

    public string UrlLabel => Loc.Translate("MockServer.Url");

    public string HeadersLabel => Loc.Translate("MockServer.Headers");

    public string BodyLabel => Loc.Translate("MockServer.Body");

    public string SendLabel => Loc.Translate("MockServer.Send");

    public string ResponseLabel => Loc.Translate("MockServer.Response");

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
    private string _clientMethod = "GET";

    [ObservableProperty]
    private string _clientUrl = "http://localhost:5190";

    [ObservableProperty]
    private string _clientHeaders = string.Empty;

    [ObservableProperty]
    private string _clientBody = string.Empty;

    [ObservableProperty]
    private string _clientResponseText = string.Empty;

    [ObservableProperty]
    private bool _isSending;

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
            IsRunning = await _server.StartAsync(Port);
        }

        OnPropertyChanged(nameof(RunningStatus));
    }

    [RelayCommand]
    private void ClearLog() => RequestLog.Clear();

    partial void OnPortChanged(int value) => Persist(s => s.Port = value);

    partial void OnResponseStatusCodeChanged(int value) => Persist(s => s.ResponseStatusCode = value);

    partial void OnResponseBodyChanged(string value) => Persist(s => s.ResponseBody = value);

    private void Persist(Action<Core.Models.MockServerSettings> apply)
    {
        if (_suppressPersist)
        {
            return;
        }

        apply(_settings.Current.MockServer);
        _settings.Save();
    }

    public async ValueTask DisposeAsync() => await _server.DisposeAsync();

    [RelayCommand]
    private async Task SendAsync()
    {
        IsSending = true;
        try
        {
            var headers = ClientHeaders
                .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(line =>
                {
                    var index = line.IndexOf(':');
                    return index < 0 ? (line, string.Empty) : (line[..index].Trim(), line[(index + 1)..].Trim());
                })
                .ToList();

            var result = await _sender.SendAsync(ClientMethod, ClientUrl, headers, ClientBody);
            ClientResponseText = result.Success
                ? $"{result.StatusCode} ({result.DurationMs} ms)\n\n{result.Headers}\n\n{result.Body}"
                : string.Format(Loc.Translate("MockServer.RequestFailed"), result.Error);
        }
        finally
        {
            IsSending = false;
        }
    }

    protected override void OnLanguageChanged()
    {
        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(ServerTabLabel));
        OnPropertyChanged(nameof(ClientTabLabel));
        OnPropertyChanged(nameof(PortLabel));
        OnPropertyChanged(nameof(StartLabel));
        OnPropertyChanged(nameof(StopLabel));
        OnPropertyChanged(nameof(ResponseStatusLabel));
        OnPropertyChanged(nameof(ResponseBodyLabel));
        OnPropertyChanged(nameof(RequestLogLabel));
        OnPropertyChanged(nameof(ClearLogLabel));
        OnPropertyChanged(nameof(MethodLabel));
        OnPropertyChanged(nameof(UrlLabel));
        OnPropertyChanged(nameof(HeadersLabel));
        OnPropertyChanged(nameof(BodyLabel));
        OnPropertyChanged(nameof(SendLabel));
        OnPropertyChanged(nameof(ResponseLabel));
        OnPropertyChanged(nameof(RunningStatus));
    }
}
