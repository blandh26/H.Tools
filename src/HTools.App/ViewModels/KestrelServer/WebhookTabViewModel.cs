using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HTools.App.Services;
using HTools.Server.Models;
using HTools.Server.Modules;

namespace HTools.App.ViewModels.KestrelServer;

public sealed partial class WebhookTabViewModel : ObservableObject, IAsyncDisposable
{
    private readonly WebhookReceiverModule _module = new();
    private readonly AppSettingsContext _settings;
    private bool _suppressPersist;

    public WebhookTabViewModel(AppSettingsContext settings)
    {
        _settings = settings;
        _module.RequestReceived += (_, entry) => Dispatcher.UIThread.Post(() => RequestLog.Insert(0, entry));

        _suppressPersist = true;
        Port = settings.Current.KestrelServer.Webhook.Port;
        _suppressPersist = false;
    }

    public ObservableCollection<RequestLogEntry> RequestLog { get; } = [];

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

    public async ValueTask DisposeAsync() => await _module.DisposeAsync();
}
