using System.Diagnostics;
using System.Management;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using LibreHardwareMonitor.Hardware;

namespace HTools.App.Services;

public sealed class SystemMonitorService : IDisposable
{
    private readonly object _sensorGate = new();
    private readonly Computer _computer = new()
    {
        IsCpuEnabled = true,
        IsGpuEnabled = true,
        IsMemoryEnabled = true,
        IsMotherboardEnabled = true,
        IsControllerEnabled = true,
        IsNetworkEnabled = true,
        IsStorageEnabled = true,
        IsPowerMonitorEnabled = true,
    };

    private ulong? _lastIdle;
    private ulong? _lastKernel;
    private ulong? _lastUser;
    private long? _lastReceived;
    private long? _lastSent;
    private long _lastNetworkTicks;
    private bool _opened;

    public void Open()
    {
        lock (_sensorGate)
        {
            try
            {
                if (_opened) return;
                _computer.Open();
                _opened = true;
            }
            catch
            {
                // CPU/memory/network monitoring remains available if sensor drivers are unavailable.
            }
        }
    }

    public LiveSystemSnapshot ReadSnapshot()
    {
        var cpu = ReadCpuUsage();
        var memory = ReadMemory();
        var (download, upload) = ReadNetworkRates();
        var sensors = ReadSensors();
        return new LiveSystemSnapshot(cpu, memory.TotalBytes, memory.UsedBytes, download, upload, sensors);
    }

    /// <summary>
    /// 读取硬件清单。这里只负责"取数"，不拼最终展示文字：每一行返回一个格式模板 + 原始参数，
    /// 由 <see cref="HTools.App.ViewModels.SystemMonitorViewModel"/> 在 UI 线程按当前语言格式化。
    /// 这样 "核 / 线程"、"序列号"、"驱动"、"可用 / 共" 这类词可以随语言切换即时刷新，
    /// 而取不到的值（参数为 null）会统一显示为本地化的"不可用"。
    /// </summary>
    public IReadOnlyList<HardwareInfoRow> ReadHardwareInventory()
    {
        var items = new List<HardwareInfoRow>
        {
            HardwareInfoRow.Plain("System", Environment.MachineName, Environment.OSVersion.VersionString, RuntimeInformation.OSArchitecture.ToString()),
        };

        // 含有自然语言词汇的行 → 使用语言文件中的 SystemMonitor.Detail.* 模板
        AddWmi(items, "Processor", "SELECT Name, NumberOfCores, NumberOfLogicalProcessors, MaxClockSpeed FROM Win32_Processor",
            o => HardwareInfoRow.Localized("Processor", "SystemMonitor.Detail.Processor",
                Str(o, "Name"), Str(o, "NumberOfCores"), Str(o, "NumberOfLogicalProcessors"), Str(o, "MaxClockSpeed")));
        AddWmi(items, "Motherboard", "SELECT Manufacturer, Product, SerialNumber FROM Win32_BaseBoard",
            o => HardwareInfoRow.Localized("Motherboard", "SystemMonitor.Detail.Motherboard",
                Str(o, "Manufacturer"), Str(o, "Product"), Str(o, "SerialNumber")));
        AddWmi(items, "Graphics", "SELECT Name, AdapterRAM, DriverVersion FROM Win32_VideoController",
            o => HardwareInfoRow.Localized("Graphics", "SystemMonitor.Detail.Graphics",
                Str(o, "Name"), FormatBytes(ToUInt64(o["AdapterRAM"])), Str(o, "DriverVersion")));

        // 其余行只是"值 · 值 · 值"（单位 MHz / GB / Mbps 与语言无关），直接用 · 连接
        AddWmi(items, "Bios", "SELECT Manufacturer, SMBIOSBIOSVersion, ReleaseDate FROM Win32_BIOS",
            o => HardwareInfoRow.Plain("Bios", JoinSpace(Str(o, "Manufacturer"), Str(o, "SMBIOSBIOSVersion")), FormatWmiDate(o["ReleaseDate"])));
        AddWmi(items, "Memory", "SELECT Manufacturer, Capacity, Speed, ConfiguredClockSpeed FROM Win32_PhysicalMemory",
            o => HardwareInfoRow.Plain("Memory", Str(o, "Manufacturer"), FormatBytes(ToUInt64(o["Capacity"])), WithUnit(Str(o, "Speed"), "MHz")));
        AddWmi(items, "Storage", "SELECT Model, MediaType, Size, InterfaceType FROM Win32_DiskDrive",
            o => HardwareInfoRow.Plain("Storage", Str(o, "Model"), FormatBytes(ToUInt64(o["Size"])), Str(o, "InterfaceType")));
        AddWmi(items, "Audio", "SELECT Name, Manufacturer, Status FROM Win32_SoundDevice",
            o => HardwareInfoRow.Plain("Audio", Str(o, "Name"), Str(o, "Manufacturer"), Str(o, "Status")));
        AddWmi(items, "Display", "SELECT Name, MonitorManufacturer, ScreenWidth, ScreenHeight, Status FROM Win32_DesktopMonitor",
            o => HardwareInfoRow.Plain("Display", Str(o, "Name"), Str(o, "MonitorManufacturer"),
                Str(o, "ScreenWidth") is { } w && Str(o, "ScreenHeight") is { } h ? $"{w} × {h}" : null, Str(o, "Status")));
        AddWmi(items, "Battery", "SELECT Name, Manufacturer, Status FROM Win32_Battery",
            o => HardwareInfoRow.Plain("Battery", Str(o, "Name"), Str(o, "Manufacturer"), Str(o, "Status")));
        AddWmi(items, "Network", "SELECT NetConnectionID, Name, MACAddress, Speed FROM Win32_NetworkAdapter WHERE PhysicalAdapter = TRUE",
            o => HardwareInfoRow.Plain("Network", Str(o, "NetConnectionID"), Str(o, "Name"), Str(o, "MACAddress"), FormatBits(ToUInt64(o["Speed"]))));

        try
        {
            var drives = DriveInfo.GetDrives().Where(d => d.IsReady).Select(d =>
                HardwareInfoRow.Localized("Volumes", "SystemMonitor.Detail.Volume",
                    d.Name, d.DriveFormat, FormatBytes((ulong)d.AvailableFreeSpace), FormatBytes((ulong)d.TotalSize)));
            items.AddRange(drives);
        }
        catch { }

        return items;
    }

    /// <summary>取 WMI 属性的字符串值；null / 空白统一视为"取不到"(返回 null)，交给 UI 显示为"不可用"。</summary>
    private static string? Str(ManagementBaseObject o, string property)
    {
        try
        {
            var text = o[property]?.ToString()?.Trim();
            return string.IsNullOrEmpty(text) ? null : text;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>"厂商 版本" 这种空格连接的组合；两边都没有时返回 null。</summary>
    private static string? JoinSpace(params string?[] parts)
    {
        var text = string.Join(' ', parts.Where(p => p is not null));
        return text.Length == 0 ? null : text;
    }

    private static string? WithUnit(string? value, string unit) => value is null ? null : $"{value} {unit}";

    /// <summary>WMI 的日期是 "20230101000000.000000+000" 这种 CIM 格式，转成 yyyy-MM-dd 便于阅读。</summary>
    private static string? FormatWmiDate(object? value)
    {
        if (value?.ToString() is not { Length: > 0 } text) return null;
        try { return ManagementDateTimeConverter.ToDateTime(text).ToString("yyyy-MM-dd"); }
        catch { return text; }
    }

    private double ReadCpuUsage()
    {
        if (!GetSystemTimes(out var idleTime, out var kernelTime, out var userTime)) return 0;
        var idle = ToUInt64(idleTime);
        var kernel = ToUInt64(kernelTime);
        var user = ToUInt64(userTime);
        var usage = 0d;
        if (_lastIdle is { } lastIdle && _lastKernel is { } lastKernel && _lastUser is { } lastUser)
        {
            var total = (kernel - lastKernel) + (user - lastUser);
            if (total > 0) usage = Math.Clamp(100d * (1d - (double)(idle - lastIdle) / total), 0, 100);
        }

        _lastIdle = idle;
        _lastKernel = kernel;
        _lastUser = user;
        return usage;
    }

    private static (ulong TotalBytes, ulong UsedBytes) ReadMemory()
    {
        var status = new MemoryStatus { Length = (uint)Marshal.SizeOf<MemoryStatus>() };
        if (!GlobalMemoryStatusEx(ref status)) return (0, 0);
        return (status.TotalPhysical, status.TotalPhysical - status.AvailablePhysical);
    }

    private (double DownloadBytesPerSecond, double UploadBytesPerSecond) ReadNetworkRates()
    {
        long received = 0, sent = 0;
        try
        {
            foreach (var adapter in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (adapter.OperationalStatus != OperationalStatus.Up || adapter.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;
                var stats = adapter.GetIPStatistics();
                received += stats.BytesReceived;
                sent += stats.BytesSent;
            }
        }
        catch { }

        var now = Stopwatch.GetTimestamp();
        var elapsed = _lastNetworkTicks == 0 ? 0 : (double)(now - _lastNetworkTicks) / Stopwatch.Frequency;
        var downRate = elapsed > 0 && _lastReceived is { } previousDown ? Math.Max(0, received - previousDown) / elapsed : 0;
        var upRate = elapsed > 0 && _lastSent is { } previousUp ? Math.Max(0, sent - previousUp) / elapsed : 0;
        _lastReceived = received;
        _lastSent = sent;
        _lastNetworkTicks = now;
        return (downRate, upRate);
    }

    private IReadOnlyList<HardwareSensor> ReadSensors()
    {
        lock (_sensorGate)
        {
            if (!_opened) return [];
            try
            {
                _computer.Accept(new HardwareUpdateVisitor());
                var output = new List<HardwareSensor>();
                foreach (var hardware in Flatten(_computer.Hardware))
                {
                    foreach (var sensor in hardware.Sensors)
                    {
                        if (sensor.Value is not { } value) continue;
                        if (sensor.SensorType == SensorType.Temperature
                            && (value <= 1 || sensor.Name.Contains("warning", StringComparison.OrdinalIgnoreCase)
                                || sensor.Name.Contains("critical", StringComparison.OrdinalIgnoreCase)
                                || sensor.Name.Contains("limit", StringComparison.OrdinalIgnoreCase))) continue;
                        var unit = sensor.SensorType switch
                        {
                            SensorType.Temperature => "°C",
                            SensorType.Fan => "RPM",
                            SensorType.Voltage => "V",
                            SensorType.Clock => "MHz",
                            SensorType.Load => "%",
                            SensorType.Power => "W",
                            SensorType.Data => "GB",
                            SensorType.SmallData => "MB",
                            _ => "",
                        };
                        output.Add(new HardwareSensor(hardware.Name, hardware.HardwareType.ToString(), sensor.Name, sensor.SensorType.ToString(), $"{value:0.0} {unit}".Trim()));
                    }
                }
                return output;
            }
            catch
            {
                return [];
            }
        }
    }

    private static IEnumerable<IHardware> Flatten(IEnumerable<IHardware> hardware)
    {
        foreach (var item in hardware)
        {
            yield return item;
            foreach (var sub in Flatten(item.SubHardware)) yield return sub;
        }
    }

    private static void AddWmi(List<HardwareInfoRow> output, string category, string query, Func<ManagementBaseObject, HardwareInfoRow> create)
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(query);
            using var results = searcher.Get();
            foreach (ManagementBaseObject item in results)
            {
                using (item) output.Add(create(item));
            }
        }
        catch { }
    }

    private static ulong ToUInt64(object? value)
    {
        try { return Convert.ToUInt64(value); } catch { return 0; }
    }

    private static ulong ToUInt64(System.Runtime.InteropServices.ComTypes.FILETIME value) =>
        ((ulong)(uint)value.dwHighDateTime << 32) | (uint)value.dwLowDateTime;

    // 0 表示 WMI 没给出数值 → 返回 null，UI 端显示本地化的"不可用"（以前是写死的英文 "n/a"）
    private static string? FormatBytes(ulong bytes) => bytes == 0 ? null : $"{bytes / 1024d / 1024 / 1024:0.##} GB";

    private static string? FormatBits(ulong bitsPerSecond) => bitsPerSecond == 0 ? null : $"{bitsPerSecond / 1_000_000d:0.#} Mbps";

    public void Dispose()
    {
        lock (_sensorGate)
        {
            if (_opened) _computer.Close();
            _opened = false;
        }
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetSystemTimes(out System.Runtime.InteropServices.ComTypes.FILETIME idleTime,
        out System.Runtime.InteropServices.ComTypes.FILETIME kernelTime,
        out System.Runtime.InteropServices.ComTypes.FILETIME userTime);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx(ref MemoryStatus buffer);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MemoryStatus
    {
        public uint Length;
        public uint MemoryLoad;
        public ulong TotalPhysical;
        public ulong AvailablePhysical;
        public ulong TotalPageFile;
        public ulong AvailablePageFile;
        public ulong TotalVirtual;
        public ulong AvailableVirtual;
        public ulong AvailableExtendedVirtual;
    }

    private sealed class HardwareUpdateVisitor : IVisitor
    {
        public void VisitComputer(IComputer computer) => computer.Traverse(this);
        public void VisitHardware(IHardware hardware)
        {
            hardware.Update();
            foreach (var subHardware in hardware.SubHardware) subHardware.Accept(this);
        }
        public void VisitSensor(ISensor sensor) { }
        public void VisitParameter(IParameter parameter) { }
    }
}

public sealed record LiveSystemSnapshot(double CpuPercent, ulong MemoryTotalBytes, ulong MemoryUsedBytes,
    double DownloadBytesPerSecond, double UploadBytesPerSecond, IReadOnlyList<HardwareSensor> Sensors);

public sealed record HardwareSensor(string Hardware, string HardwareType, string Name, string SensorType, string Value);

/// <summary>
/// 一行硬件信息的"原始数据"。<paramref name="Format"/> 为 <c>string.Format</c> 模板：
/// 当 <paramref name="FormatIsLocalizationKey"/> 为 true 时它是语言文件的键（如 SystemMonitor.Detail.Processor），
/// 否则就是与语言无关的字面模板（"{0} · {1} · …"）。<paramref name="Args"/> 中的 null 表示该值取不到。
/// </summary>
public sealed record HardwareInfoRow(string Category, string Format, IReadOnlyList<string?> Args, bool FormatIsLocalizationKey)
{
    /// <summary>使用语言文件模板的行。</summary>
    public static HardwareInfoRow Localized(string category, string formatKey, params string?[] args) =>
        new(category, formatKey, args, true);

    /// <summary>纯值行：各参数以 " · " 连接。</summary>
    public static HardwareInfoRow Plain(string category, params string?[] args) =>
        new(category, string.Join(" · ", args.Select((_, i) => $"{{{i}}}")), args, false);
}
