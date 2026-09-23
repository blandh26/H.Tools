namespace HTools.Core.Models;

public sealed class AppSettings
{
    public string Language { get; set; } = "zh-CN";

    public bool StartWithWindows { get; set; }

    public bool IsDarkTheme { get; set; } = true;

    public int MinToolCardWidth { get; set; } = 220;

    public List<CustomToolItem> CustomTools { get; set; } = [];

    public List<string> ToolboxOrder { get; set; } = [];

    public List<string> PinnedToolIds { get; set; } = [];

    public List<ClipboardSlot> ClipboardSlots { get; set; } = [];

    public MouseEffectSettings MouseEffect { get; set; } = new();

    public MockServerSettings MockServer { get; set; } = new();

    public KestrelServerSettings KestrelServer { get; set; } = new();

    public ScreenshotSettings Screenshot { get; set; } = new();
}
