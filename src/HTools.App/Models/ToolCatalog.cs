namespace HTools.App.Models;

public static class ToolCatalog
{
    public static readonly IReadOnlyList<ToolDescriptor> All =
    [
        new ToolDescriptor("clipboard", NavGroupKeys.Utilities, "📋", "Tool.Clipboard.Name", "Tool.Clipboard.Description", IsAvailable: true),
        new ToolDescriptor("mouse-effect", NavGroupKeys.Utilities, "🖱", "Tool.MouseEffect.Name", "Tool.MouseEffect.Description", IsAvailable: true),
        new ToolDescriptor("screenshot", NavGroupKeys.Utilities, "✂", "Tool.Screenshot.Name", "Tool.Screenshot.Description", IsAvailable: true),
        new ToolDescriptor("mock-server", NavGroupKeys.NetworkTools, "🌐", "Tool.MockServer.Name", "Tool.MockServer.Description", IsAvailable: true),
        new ToolDescriptor("kestrel-server", NavGroupKeys.NetworkTools, "📡", "Tool.KestrelServer.Name", "Tool.KestrelServer.Description", IsAvailable: true),
    ];

    public static ToolDescriptor? FindById(string id) => All.FirstOrDefault(t => t.Id == id);

    public static IEnumerable<ToolDescriptor> ForGroup(string groupKey) => All.Where(t => t.GroupKey == groupKey);
}
