using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using HTools.Core.Services;
using HTools.Server.Http;

namespace HTools.App.ViewModels;

/// <summary>Standalone "Mock client" tool — manually build and send an HTTP request, inspect the
/// response. Split out from the combined MockServer tool (which used to have Server/Client tabs) per
/// the user's request to make every server-adjacent function its own independent tool.</summary>
public sealed partial class MockClientViewModel : LocalizedViewModelBase
{
    private readonly HttpRequestSender _sender = new();

    public ObservableCollection<MockRequestHistoryItem> History { get; } = [];

    public MockClientViewModel(ILocalizationService loc)
        : base(loc)
    {
    }

    public string[] HttpMethods { get; } = ["GET", "POST", "PUT", "PATCH", "DELETE"];

    public string Title => Loc.Translate("Tool.MockClient.Name");

    public string UrlLabel => Loc.Translate("MockServer.Url");

    public string HeadersLabel => Loc.Translate("MockServer.Headers");

    public string BodyLabel => Loc.Translate("MockServer.Body");

    public string SendLabel => Loc.Translate("MockServer.Send");

    public string ResponseLabel => Loc.Translate("MockServer.Response");

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

    [ObservableProperty]
    private MockRequestHistoryItem? _selectedHistory;

    partial void OnSelectedHistoryChanged(MockRequestHistoryItem? value)
    {
        if (value is null) return;
        ClientMethod = value.Method;
        ClientUrl = value.Url;
        ClientHeaders = value.Headers;
        ClientBody = value.Body;
        ClientResponseText = value.Response;
    }

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
            History.Insert(0, new MockRequestHistoryItem(ClientMethod, ClientUrl, ClientHeaders, ClientBody, ClientResponseText));
            while (History.Count > 100) History.RemoveAt(History.Count - 1);
        }
        finally
        {
            IsSending = false;
        }
    }

    [RelayCommand]
    private void ClearHistory()
    {
        History.Clear();
        SelectedHistory = null;
    }

    protected override void OnLanguageChanged()
    {
        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(UrlLabel));
        OnPropertyChanged(nameof(HeadersLabel));
        OnPropertyChanged(nameof(BodyLabel));
        OnPropertyChanged(nameof(SendLabel));
        OnPropertyChanged(nameof(ResponseLabel));
    }
}

public sealed record MockRequestHistoryItem(string Method, string Url, string Headers, string Body, string Response)
{
    public string Summary => $"{Method}  {Url}";
}
