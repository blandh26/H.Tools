using HTools.Server.Hosting;
using HTools.Server.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace HTools.Server.Modules;

/// <summary>The "server" half of the plain mock-server tool: one configurable status/body for every
/// request, regardless of path, with a live log of what came in.</summary>
public sealed class SimpleEchoServerModule : ServerModuleBase
{
    public int ResponseStatusCode { get; set; } = 200;

    public string ResponseBody { get; set; } = "OK";

    public string ResponseContentType { get; set; } = "text/plain; charset=utf-8";

    public event EventHandler<RequestLogEntry>? RequestReceived;

    protected override void Configure(WebApplication app)
    {
        app.Run(async ctx =>
        {
            var body = await RequestReading.ReadBodyAsync(ctx.Request);
            ctx.Response.StatusCode = ResponseStatusCode;
            ctx.Response.ContentType = ResponseContentType;
            await ctx.Response.WriteAsync(ResponseBody);

            RequestReceived?.Invoke(this, new RequestLogEntry(
                DateTime.Now,
                ctx.Request.Method,
                ctx.Request.Path.Value ?? string.Empty,
                ctx.Request.QueryString.Value ?? string.Empty,
                ctx.Connection.RemoteIpAddress?.ToString() ?? string.Empty,
                RequestReading.FormatHeaders(ctx.Request),
                body,
                ResponseStatusCode));
        });
    }
}
