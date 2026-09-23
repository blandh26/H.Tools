using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HTools.App.Services;
using HTools.Core.Models;
using HTools.Core.Services;
using HTools.Server.Models;
using HTools.Server.Modules;

namespace HTools.App.ViewModels.KestrelServer;

/// <summary>Standalone "Mock API server" tool — see <see cref="StaticFileTabViewModel"/> for why this
/// used to be a tab and now isn't.</summary>
public sealed partial class MockApiTabViewModel : LocalizedViewModelBase, IAsyncDisposable
{
    private readonly MockApiServerModule _module = new();
    private readonly AppSettingsContext _settings;
    private bool _suppressPersist;

    public MockApiTabViewModel(ILocalizationService loc, AppSettingsContext settings)
        : base(loc)
    {
        _settings = settings;
        _module.RequestReceived += (_, entry) => Dispatcher.UIThread.Post(() => RequestLog.Insert(0, entry));

        var saved = settings.Current.KestrelServer.MockApi;
        _suppressPersist = true;
        Port = saved.Port;
        foreach (var rule in saved.Rules)
        {
            Rules.Add(new MockRuleEditorItem
            {
                Method = rule.Method,
                Path = rule.Path,
                StatusCode = rule.StatusCode,
                ContentType = rule.ContentType,
                Body = rule.Body,
            });
        }

        if (Rules.Count == 0)
        {
            Rules.Add(new MockRuleEditorItem());
        }

        _suppressPersist = false;
        PushRules();
    }

    public ObservableCollection<MockRuleEditorItem> Rules { get; } = [];

    public ObservableCollection<RequestLogEntry> RequestLog { get; } = [];

    public string Title => Loc.Translate("Tool.MockApiServer.Name");

    public string PortLabel => Loc.Translate("MockServer.Port");

    public string StartLabel => Loc.Translate("MockServer.Start");

    public string StopLabel => Loc.Translate("MockServer.Stop");

    public string RulesLabel => Loc.Translate("KestrelServer.Rules");

    public string AddRuleLabel => Loc.Translate("KestrelServer.AddRule");

    public string RemoveRuleLabel => Loc.Translate("KestrelServer.RemoveRule");

    public string ApplyLabel => Loc.Translate("Common.Save");

    public string RequestLogLabel => Loc.Translate("MockServer.RequestLog");

    public string ClearLogLabel => Loc.Translate("MockServer.ClearLog");

    public string ServiceUrlLabel => Loc.Translate("KestrelServer.ServiceUrl");

    /// <summary>规则匹配说明（方法 + 路径精确匹配，未命中返回 404）。</summary>
    public string HintText => Loc.Translate("KestrelServer.MockApiHint");

    /// <summary>规则行里 Method 输入框的占位文字（"/path"、"Content-Type" 属于技术标识，保持原样不翻译）。</summary>
    public string MethodPlaceholder => Loc.Translate("KestrelServer.RuleMethod");

    public string BodyPlaceholder => Loc.Translate("MockServer.ResponseBody");

    public string AccessUrl => $"http://localhost:{Port}";

    [ObservableProperty]
    private int _port;

    [ObservableProperty]
    private bool _isRunning;

    [ObservableProperty]
    private string? _errorMessage;

    [RelayCommand]
    private void AddRule() => Rules.Add(new MockRuleEditorItem());

    [RelayCommand]
    private void RemoveRule(MockRuleEditorItem item)
    {
        Rules.Remove(item);
        PushRules();
        PersistRules();
    }

    [RelayCommand]
    private void ApplyRules()
    {
        PushRules();
        PersistRules();
    }

    [RelayCommand]
    private void ClearLog() => RequestLog.Clear();

    [RelayCommand]
    private async Task ToggleAsync()
    {
        if (IsRunning)
        {
            await _module.StopAsync();
            IsRunning = false;
            return;
        }

        PushRules();
        IsRunning = await _module.StartAsync(Port);
        ErrorMessage = IsRunning ? null : _module.LastError;
    }

    partial void OnPortChanged(int value)
    {
        if (_suppressPersist)
        {
            return;
        }

        _settings.Current.KestrelServer.MockApi.Port = value;
        _settings.Save();
        OnPropertyChanged(nameof(AccessUrl));
    }

    private void PushRules()
    {
        _module.SetRules(Rules.Select(r => new MockApiRule(r.Method, r.Path, r.StatusCode, r.ContentType, r.Body)));
    }

    private void PersistRules()
    {
        if (_suppressPersist)
        {
            return;
        }

        _settings.Current.KestrelServer.MockApi.Rules = Rules
            .Select(r => new MockApiRuleSettings
            {
                Method = r.Method,
                Path = r.Path,
                StatusCode = r.StatusCode,
                ContentType = r.ContentType,
                Body = r.Body,
            })
            .ToList();
        _settings.Save();
    }

    protected override void OnLanguageChanged()
    {
        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(PortLabel));
        OnPropertyChanged(nameof(StartLabel));
        OnPropertyChanged(nameof(StopLabel));
        OnPropertyChanged(nameof(RulesLabel));
        OnPropertyChanged(nameof(AddRuleLabel));
        OnPropertyChanged(nameof(RemoveRuleLabel));
        OnPropertyChanged(nameof(ApplyLabel));
        OnPropertyChanged(nameof(RequestLogLabel));
        OnPropertyChanged(nameof(ClearLogLabel));
        OnPropertyChanged(nameof(ServiceUrlLabel));
        OnPropertyChanged(nameof(HintText));
        OnPropertyChanged(nameof(MethodPlaceholder));
        OnPropertyChanged(nameof(BodyPlaceholder));
    }

    public async ValueTask DisposeAsync() => await _module.DisposeAsync();
}
