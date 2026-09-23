namespace HTools.Core.Models;

public sealed class KestrelServerSettings
{
    public StaticFileTabSettings StaticFile { get; set; } = new();

    public MockApiTabSettings MockApi { get; set; } = new();

    public WebhookTabSettings Webhook { get; set; } = new();

    public UploadTabSettings Upload { get; set; } = new();

    public ProxyTabSettings Proxy { get; set; } = new();

    public LatencyTabSettings Latency { get; set; } = new();
}

public sealed class StaticFileTabSettings
{
    public int Port { get; set; } = 5201;

    public string RootFolder { get; set; } = string.Empty;
}

public sealed class MockApiTabSettings
{
    public int Port { get; set; } = 5202;

    public List<MockApiRuleSettings> Rules { get; set; } = [];
}

public sealed class MockApiRuleSettings
{
    public string Method { get; set; } = "GET";

    public string Path { get; set; } = "/";

    public int StatusCode { get; set; } = 200;

    public string ContentType { get; set; } = "application/json";

    public string Body { get; set; } = "{}";
}

public sealed class WebhookTabSettings
{
    public int Port { get; set; } = 5203;
}

public sealed class UploadTabSettings
{
    public int Port { get; set; } = 5204;

    public string UploadFolder { get; set; } = string.Empty;
}

public sealed class ProxyTabSettings
{
    public int Port { get; set; } = 5205;

    public string TargetUrl { get; set; } = "http://localhost:3000";
}

public sealed class LatencyTabSettings
{
    public int Port { get; set; } = 5206;

    public int DelayMs { get; set; } = 1000;

    public string TargetUrl { get; set; } = string.Empty;
}
