using HTools.Server.Hosting;
using HTools.Server.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace HTools.Server.Modules;

/// <summary>Delays every request by a configurable amount before responding — either forwarding to a
/// target URL (with the same delay applied) or echoing basic request info back as JSON.</summary>
public sealed class LatencySimulatorModule : ServerModuleBase
{
    private static readonly HttpClient Client = new();

    public int DelayMs { get; set; } = 1000;

    public string? TargetUrl { get; set; }

    public event EventHandler<RequestLogEntry>? RequestReceived;

    protected override void Configure(WebApplication app)
    {
        app.Run(async ctx =>
        {
            var body = await RequestReading.ReadBodyAsync(ctx.Request);
            await Task.Delay(DelayMs);

            if (!string.IsNullOrWhiteSpace(TargetUrl))
            {
                var targetUri = new Uri(new Uri(TargetUrl), ctx.Request.Path + ctx.Request.QueryString);
                using var proxyRequest = new HttpRequestMessage(new HttpMethod(ctx.Request.Method), targetUri);
                if (!HttpMethods.IsGet(ctx.Request.Method) && !HttpMethods.IsHead(ctx.Request.Method) && body.Length > 0)
                {
                    proxyRequest.Content = new StringContent(body);
                }

                using var response = await Client.SendAsync(proxyRequest);
                ctx.Response.StatusCode = (int)response.StatusCode;
                await ctx.Response.WriteAsync(await response.Content.ReadAsStringAsync());
            }
            else
            {
                ctx.Response.StatusCode = 200;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.WriteAsync(
                    $$"""{"delayedMs":{{DelayMs}},"method":"{{ctx.Request.Method}}","path":"{{ctx.Request.Path}}"}""");
            }

            RequestReceived?.Invoke(this, new RequestLogEntry(
                DateTime.Now,
                ctx.Request.Method,
                ctx.Request.Path.Value ?? string.Empty,
                ctx.Request.QueryString.Value ?? string.Empty,
                ctx.Connection.RemoteIpAddress?.ToString() ?? string.Empty,
                RequestReading.FormatHeaders(ctx.Request),
                body,
                ctx.Response.StatusCode));
        });
    }
}
