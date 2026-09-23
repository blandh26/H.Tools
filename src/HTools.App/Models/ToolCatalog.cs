namespace HTools.App.Models;

public static class ToolCatalog
{
    public static readonly IReadOnlyList<ToolDescriptor> All =
    [
        new ToolDescriptor("clipboard", NavGroupKeys.Utilities, "📋", "Tool.Clipboard.Name", "Tool.Clipboard.Description", IsAvailable: true),
        new ToolDescriptor("mouse-effect", NavGroupKeys.Utilities, "🖱", "Tool.MouseEffect.Name", "Tool.MouseEffect.Description", IsAvailable: true),
        new ToolDescriptor("screenshot", NavGroupKeys.Utilities, "✂", "Tool.Screenshot.Name", "Tool.Screenshot.Description", IsAvailable: true),
        new ToolDescriptor("system-monitor", NavGroupKeys.Utilities, "📊", "Tool.SystemMonitor.Name", "Tool.SystemMonitor.Description", IsAvailable: true),

        // Each of these used to be one tab inside two combined "Mock Server" / "multi-function server"
        // tools; split into independent tools per the user's explicit request (see git history/commit
        // message for the reasoning) so every server function gets its own card instead of being buried
        // behind a TabControl.
        new ToolDescriptor("mock-client", NavGroupKeys.DevTools, "📮", "Tool.MockClient.Name", "Tool.MockClient.Description", IsAvailable: true),
        new ToolDescriptor("mock-echo-server", NavGroupKeys.DevTools, "🔁", "Tool.MockEchoServer.Name", "Tool.MockEchoServer.Description", IsAvailable: true),
        new ToolDescriptor("static-file-server", NavGroupKeys.DevTools, "📁", "Tool.StaticFileServer.Name", "Tool.StaticFileServer.Description", IsAvailable: true),
        new ToolDescriptor("mock-api-server", NavGroupKeys.DevTools, "🧩", "Tool.MockApiServer.Name", "Tool.MockApiServer.Description", IsAvailable: true),
        new ToolDescriptor("webhook-server", NavGroupKeys.DevTools, "🪝", "Tool.WebhookServer.Name", "Tool.WebhookServer.Description", IsAvailable: true),
        new ToolDescriptor("upload-server", NavGroupKeys.DevTools, "📤", "Tool.UploadServer.Name", "Tool.UploadServer.Description", IsAvailable: true),
        new ToolDescriptor("reverse-proxy-server", NavGroupKeys.DevTools, "🔀", "Tool.ReverseProxyServer.Name", "Tool.ReverseProxyServer.Description", IsAvailable: true),
        new ToolDescriptor("latency-server", NavGroupKeys.DevTools, "⏱", "Tool.LatencyServer.Name", "Tool.LatencyServer.Description", IsAvailable: true),
    ];

    public static IEnumerable<ToolDescriptor> ForGroup(string groupKey) => All.Where(t => t.GroupKey == groupKey);
}
