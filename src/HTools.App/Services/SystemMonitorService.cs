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

    public IReadOnlyList<HardwareInfoRow> ReadHardwareInventory()
    {
        var items = new List<HardwareInfoRow>
        {
            new("System", $"{Environment.MachineName} · {Environment.OSVersion.VersionString} · {RuntimeInformation.OSArchitecture}"),
        };

        AddWmi(items, "Processor", "SELECT Name, NumberOfCores, NumberOfLogicalProcessors, MaxClockSpeed FROM Win32_Processor",
            o => $"{o["Name"]} · {o["NumberOfCores"]} cores / {o["NumberOfLogicalProcessors"]} threads · {o["MaxClockSpeed"]} MHz");
        AddWmi(items, "Motherboard", "SELECT Manufacturer, Product, SerialNumber FROM Win32_BaseBoard",
            o => $"{o["Manufacturer"]} {o["Product"]} · S/N {o["SerialNumber"]}");
        AddWmi(items, "Bios", "SELECT Manufacturer, SMBIOSBIOSVersion, ReleaseDate FROM Win32_BIOS",
            o => $"{o["Manufacturer"]} {o["SMBIOSBIOSVersion"]} · {o["ReleaseDate"]}");
        AddWmi(items, "Graphics", "SELECT Name, AdapterRAM, DriverVersion FROM Win32_VideoController",
            o => $"{o["Name"]} · {FormatBytes(ToUInt64(o["AdapterRAM"]))} · Driver {o["DriverVersion"]}");
        AddWmi(items, "Memory", "SELECT Manufacturer, Capacity, Speed, ConfiguredClockSpeed FROM Win32_PhysicalMemory",
            o => $"{o["Manufacturer"]} · {FormatBytes(ToUInt64(o["Capacity"]))} · {o["Speed"]} MHz");
        AddWmi(items, "Storage", "SELECT Model, MediaType, Size, InterfaceType FROM Win32_DiskDrive",
            o => $"{o["Model"]} · {FormatBytes(ToUInt64(o["Size"]))} · {o["InterfaceType"]}");
        AddWmi(items, "Audio", "SELECT Name, Manufacturer, Status FROM Win32_SoundDevice",
            o => $"{o["Name"]} · {o["Manufacturer"]} · {o["Status"]}");
        AddWmi(items, "Display", "SELECT Name, MonitorManufacturer, ScreenWidth, ScreenHeight, Status FROM Win32_DesktopMonitor",
            o => $"{o["Name"]} · {o["MonitorManufacturer"]} · {o["ScreenWidth"]} × {o["ScreenHeight"]} · {o["Status"]}");
        AddWmi(items, "Battery", "SELECT Name, Manufacturer, Status FROM Win32_Battery",
            o => $"{o["Name"]} · {o["Manufacturer"]} · {o["Status"]}");
        AddWmi(items, "Network", "SELECT NetConnectionID, Name, MACAddress, Speed FROM Win32_NetworkAdapter WHERE PhysicalAdapter = TRUE",
            o => $"{o["NetConnectionID"]} · {o["Name"]} · {o["MACAddress"]} · {FormatBits(ToUInt64(o["Speed"]))}");

        try
        {
            var drives = DriveInfo.GetDrives().Where(d => d.IsReady).Select(d =>
                new HardwareInfoRow("Volumes", $"{d.Name} · {d.DriveFormat} · {FormatBytes((ulong)d.AvailableFreeSpace)} free / {FormatBytes((ulong)d.TotalSize)}"));
            items.AddRange(drives);
        }
        catch { }

        return items;
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

    private static void AddWmi(List<HardwareInfoRow> output, string category, string query, Func<ManagementBaseObject, string> format)
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(query);
            using var results = searcher.Get();
            foreach (ManagementBaseObject item in results)
            {
                using (item) output.Add(new HardwareInfoRow(category, format(item)));
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

    private static string FormatBytes(ulong bytes) => bytes == 0 ? "n/a" : $"{bytes / 1024d / 1024 / 1024:0.##} GB";

    private static string FormatBits(ulong bitsPerSecond) => bitsPerSecond == 0 ? "speed n/a" : $"{bitsPerSecond / 1_000_000d:0.#} Mbps";

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

public sealed record HardwareInfoRow(string Category, string Details);
