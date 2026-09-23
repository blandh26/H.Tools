using HTools.Server.Hosting;
using HTools.Server.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace HTools.Server.Modules;

/// <summary>Accepts anything on any path/method, always answers 200, and logs every call in full —
/// for pointing a third-party webhook/callback at while developing.</summary>
public sealed class WebhookReceiverModule : ServerModuleBase
{
    public string ResponseBody { get; set; } = "OK";

    public event EventHandler<RequestLogEntry>? RequestReceived;

    protected override void Configure(WebApplication app)
    {
        app.Run(async ctx =>
        {
            var body = await RequestReading.ReadBodyAsync(ctx.Request);
            ctx.Response.StatusCode = 200;
            await ctx.Response.WriteAsync(ResponseBody);

            RequestReceived?.Invoke(this, new RequestLogEntry(
                DateTime.Now,
                ctx.Request.Method,
                ctx.Request.Path.Value ?? string.Empty,
                ctx.Request.QueryString.Value ?? string.Empty,
                ctx.Connection.RemoteIpAddress?.ToString() ?? string.Empty,
                RequestReading.FormatHeaders(ctx.Request),
                body,
                200));
        });
    }
}
