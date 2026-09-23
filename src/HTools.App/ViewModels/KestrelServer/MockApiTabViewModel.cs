using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HTools.App.Services;
using HTools.Core.Models;
using HTools.Server.Models;
using HTools.Server.Modules;

namespace HTools.App.ViewModels.KestrelServer;

public sealed partial class MockApiTabViewModel : ObservableObject, IAsyncDisposable
{
    private readonly MockApiServerModule _module = new();
    private readonly AppSettingsContext _settings;
    private bool _suppressPersist;

    public MockApiTabViewModel(AppSettingsContext settings)
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

    public async ValueTask DisposeAsync() => await _module.DisposeAsync();
}
