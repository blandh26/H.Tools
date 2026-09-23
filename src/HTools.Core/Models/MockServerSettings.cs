namespace HTools.Core.Models;

public sealed class MockServerSettings
{
    public int Port { get; set; } = 5190;

    public int ResponseStatusCode { get; set; } = 200;

    public string ResponseBody { get; set; } = "OK";

    public string ResponseContentType { get; set; } = "text/plain; charset=utf-8";
}
