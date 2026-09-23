using HTools.Server.Hosting;
using HTools.Server.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace HTools.Server.Modules;

/// <summary>Routes requests against a live-editable list of method+path rules; each rule's response
/// can be changed while the server keeps running.</summary>
public sealed class MockApiServerModule : ServerModuleBase
{
    private readonly object _lock = new();
    private List<MockApiRule> _rules = [];

    public event EventHandler<RequestLogEntry>? RequestReceived;

    public void SetRules(IEnumerable<MockApiRule> rules)
    {
        lock (_lock)
        {
            _rules = rules.ToList();
        }
    }

    protected override void Configure(WebApplication app)
    {
        app.Run(async ctx =>
        {
            var body = await RequestReading.ReadBodyAsync(ctx.Request);

            MockApiRule? match;
            lock (_lock)
            {
                match = _rules.FirstOrDefault(r =>
                    string.Equals(r.Method, ctx.Request.Method, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(r.Path, ctx.Request.Path.Value, StringComparison.OrdinalIgnoreCase));
            }

            if (match is not null)
            {
                ctx.Response.StatusCode = match.StatusCode;
                ctx.Response.ContentType = match.ContentType;
                await ctx.Response.WriteAsync(match.Body);
            }
            else
            {
                ctx.Response.StatusCode = 404;
                await ctx.Response.WriteAsync("No mock rule matched this method/path.");
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
