using Avalonia.Threading;
using HTools.App.Services;
using HTools.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace HTools.App.ViewModels;

public sealed partial class SystemMonitorViewModel : LocalizedViewModelBase, IDisposable
{
    private readonly SystemMonitorService _monitor = new();
    private CancellationTokenSource? _cancellation;
    private Task? _pollTask;
    private readonly Queue<double> _cpuHistory = new();
    private readonly Queue<double> _memoryHistory = new();
    private readonly Queue<double> _downloadHistory = new();
    private readonly Queue<double> _uploadHistory = new();
    private IReadOnlyList<HardwareSensor> _latestSensors = [];
    private IReadOnlyList<HardwareInfoRow> _hardwareRows = [];

    public SystemMonitorViewModel(ILocalizationService loc) : base(loc) { }

    public string Title => Loc.Translate("Tool.SystemMonitor.Name");

    public string CpuLabel => Loc.Translate("SystemMonitor.Cpu");
    public string MemoryLabel => Loc.Translate("SystemMonitor.Memory");
    public string NetworkLabel => Loc.Translate("SystemMonitor.Network");
    public string TemperatureLabel => Loc.Translate("SystemMonitor.Temperature");
    public string CpuTemperatureLabel => Loc.Translate("SystemMonitor.CpuTemperature");
    public string GpuTemperatureLabel => Loc.Translate("SystemMonitor.GpuTemperature");
    public string DownloadLabel => Loc.Translate("SystemMonitor.Download");
    public string UploadLabel => Loc.Translate("SystemMonitor.Upload");
    public string HardwareLabel => Loc.Translate("SystemMonitor.Hardware");
    public string HardwareCategoryLabel => Loc.Translate("SystemMonitor.HardwareCategory");
    public string HardwareDetailsLabel => Loc.Translate("SystemMonitor.HardwareDetails");

    public double MemoryPercent => MemoryTotalBytes == 0 ? 0 : MemoryUsedBytes * 100d / MemoryTotalBytes;

    public string CpuText => $"{CpuPercent:0}%";

    public string MemoryText => $"{FormatBytes(MemoryUsedBytes)} / {FormatBytes(MemoryTotalBytes)}";

    public string DownloadText => $"↓ {FormatRate(DownloadBytesPerSecond)}";

    public string UploadText => $"↑ {FormatRate(UploadBytesPerSecond)}";

    public string CpuTemperatureText => TemperatureText("CPU");

    public string GpuTemperatureText => TemperatureText("GPU", "Graphics");

    public string CpuTemperatureDisplay => $"{CpuTemperatureLabel}: {CpuTemperatureText}";

    public string GpuTemperatureDisplay => $"{GpuTemperatureLabel}: {GpuTemperatureText}";

    public string UnavailableText => Loc.Translate("SystemMonitor.Unavailable");

    public string UpdatedAtText => UpdatedAt is null ? Loc.Translate("SystemMonitor.Loading") : $"{Loc.Translate("SystemMonitor.UpdatedAt")} {UpdatedAt:HH:mm:ss}";

    [ObservableProperty] private double _cpuPercent;
    [ObservableProperty] private ulong _memoryTotalBytes;
    [ObservableProperty] private ulong _memoryUsedBytes;
    [ObservableProperty] private double _downloadBytesPerSecond;
    [ObservableProperty] private double _uploadBytesPerSecond;
    [ObservableProperty] private double[] _cpuHistoryValues = [];
    [ObservableProperty] private double[] _memoryHistoryValues = [];
    [ObservableProperty] private double[] _downloadHistoryValues = [];
    [ObservableProperty] private double[] _uploadHistoryValues = [];
    [ObservableProperty] private IReadOnlyList<HardwareInfoDisplayRow> _hardwareInventory = [];
    [ObservableProperty] private DateTime? _updatedAt;
    [ObservableProperty] private bool _isMonitoring;
    [ObservableProperty] private string? _errorMessage;

    partial void OnCpuPercentChanged(double value) => OnPropertyChanged(nameof(CpuText));
    partial void OnMemoryUsedBytesChanged(ulong value)
    {
        OnPropertyChanged(nameof(MemoryText));
        OnPropertyChanged(nameof(MemoryPercent));
    }
    partial void OnMemoryTotalBytesChanged(ulong value)
    {
        OnPropertyChanged(nameof(MemoryText));
        OnPropertyChanged(nameof(MemoryPercent));
    }
    partial void OnDownloadBytesPerSecondChanged(double value) => OnPropertyChanged(nameof(DownloadText));
    partial void OnUploadBytesPerSecondChanged(double value) => OnPropertyChanged(nameof(UploadText));
    partial void OnUpdatedAtChanged(DateTime? value) => OnPropertyChanged(nameof(UpdatedAtText));
    public void StartMonitoring()
    {
        if (_cancellation is not null) return;
        _cancellation = new CancellationTokenSource();
        IsMonitoring = true;
        _pollTask = PollAsync(_cancellation.Token);
    }

    public void StopMonitoring()
    {
        if (_cancellation is null) return;
        _cancellation.Cancel();
        _cancellation.Dispose();
        _cancellation = null;
        IsMonitoring = false;
    }

    private async Task PollAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Run(_monitor.Open, cancellationToken);
            var inventory = await Task.Run(_monitor.ReadHardwareInventory, cancellationToken);
            Dispatcher.UIThread.Post(() =>
            {
                _hardwareRows = inventory;
                RefreshHardwareInventory();
            });
            while (!cancellationToken.IsCancellationRequested)
            {
                var snapshot = await Task.Run(_monitor.ReadSnapshot, cancellationToken);
                Dispatcher.UIThread.Post(() => ApplySnapshot(snapshot));
                await Task.Delay(1000, cancellationToken);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            Dispatcher.UIThread.Post(() => ErrorMessage = ex.Message);
        }
    }

    private void ApplySnapshot(LiveSystemSnapshot snapshot)
    {
        CpuPercent = snapshot.CpuPercent;
        MemoryTotalBytes = snapshot.MemoryTotalBytes;
        MemoryUsedBytes = snapshot.MemoryUsedBytes;
        DownloadBytesPerSecond = snapshot.DownloadBytesPerSecond;
        UploadBytesPerSecond = snapshot.UploadBytesPerSecond;
        _latestSensors = snapshot.Sensors;
        UpdatedAt = DateTime.Now;
        ErrorMessage = null;
        Add(_cpuHistory, CpuPercent);
        Add(_memoryHistory, MemoryPercent);
        Add(_downloadHistory, DownloadBytesPerSecond / 1024d);
        Add(_uploadHistory, UploadBytesPerSecond / 1024d);
        CpuHistoryValues = _cpuHistory.ToArray();
        MemoryHistoryValues = _memoryHistory.ToArray();
        DownloadHistoryValues = _downloadHistory.ToArray();
        UploadHistoryValues = _uploadHistory.ToArray();
        OnPropertyChanged(nameof(CpuTemperatureText));
        OnPropertyChanged(nameof(GpuTemperatureText));
        OnPropertyChanged(nameof(CpuTemperatureDisplay));
        OnPropertyChanged(nameof(GpuTemperatureDisplay));
    }

    private static void Add(Queue<double> queue, double value)
    {
        queue.Enqueue(value);
        while (queue.Count > 60) queue.Dequeue();
    }

    private void RefreshHardwareInventory() => HardwareInventory = _hardwareRows.Select(row =>
        new HardwareInfoDisplayRow(Loc.Translate($"SystemMonitor.Category.{row.Category}"), row.Details)).ToArray();

    private string TemperatureText(params string[] hardwareHints)
    {
        var matches = _latestSensors.Where(s => s.SensorType == "Temperature" && hardwareHints.Any(h =>
                s.HardwareType.Contains(h, StringComparison.OrdinalIgnoreCase)
                || s.Hardware.Contains(h, StringComparison.OrdinalIgnoreCase)))
            .Select(s => double.TryParse(s.Value.Replace("°C", "").Trim(), out var value) ? value : (double?)null)
            .Where(x => x.HasValue).Select(x => x!.Value).ToArray();
        return matches.Length == 0 ? UnavailableText : $"{matches.Max():0} °C";
    }

    private static string FormatBytes(ulong value) => value == 0 ? "--" : $"{value / 1024d / 1024 / 1024:0.0} GB";

    private static string FormatRate(double value) => value < 1024 ? $"{value:0} B/s" : value < 1024 * 1024 ? $"{value / 1024:0.0} KB/s" : $"{value / 1024 / 1024:0.00} MB/s";

    protected override void OnLanguageChanged()
    {
        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(CpuLabel));
        OnPropertyChanged(nameof(MemoryLabel));
        OnPropertyChanged(nameof(NetworkLabel));
        OnPropertyChanged(nameof(TemperatureLabel));
        OnPropertyChanged(nameof(CpuTemperatureLabel));
        OnPropertyChanged(nameof(GpuTemperatureLabel));
        OnPropertyChanged(nameof(DownloadLabel));
        OnPropertyChanged(nameof(UploadLabel));
        OnPropertyChanged(nameof(HardwareLabel));
        OnPropertyChanged(nameof(HardwareCategoryLabel));
        OnPropertyChanged(nameof(HardwareDetailsLabel));
        RefreshHardwareInventory();
        OnPropertyChanged(nameof(UnavailableText));
        OnPropertyChanged(nameof(UpdatedAtText));
    }

    public void Dispose()
    {
        StopMonitoring();
        try { _pollTask?.Wait(1500); } catch { }
        _monitor.Dispose();
    }
}

public sealed record HardwareInfoDisplayRow(string Category, string Details);
