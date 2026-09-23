using HTools.Server.Hosting;
using HTools.Server.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace HTools.Server.Modules;

/// <summary>Forwards every request to a configurable target base URL and streams the response back;
/// a small hand-rolled proxy rather than a full reverse-proxy library, which is plenty for local dev use.</summary>
public sealed class ReverseProxyModule : ServerModuleBase
{
    // Hop-by-hop headers must not be copied between the two connections: Kestrel manages its own
    // framing (Transfer-Encoding/Content-Length/Connection) for the outgoing response, and blindly
    // forwarding the upstream's copy corrupts the byte stream (e.g. double chunked-encoding framing).
    private static readonly HashSet<string> HopByHopHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Transfer-Encoding", "Connection", "Keep-Alive", "Proxy-Authenticate",
        "Proxy-Authorization", "TE", "Trailers", "Upgrade", "Content-Length",
    };

    private static readonly HttpClient Client = new();

    public string TargetBaseUrl { get; set; } = string.Empty;

    public event EventHandler<RequestLogEntry>? RequestReceived;

    protected override void Configure(WebApplication app)
    {
        app.Run(async ctx =>
        {
            var targetUri = new Uri(new Uri(TargetBaseUrl), ctx.Request.Path + ctx.Request.QueryString);
            using var proxyRequest = new HttpRequestMessage(new HttpMethod(ctx.Request.Method), targetUri);

            if (!HttpMethods.IsGet(ctx.Request.Method) && !HttpMethods.IsHead(ctx.Request.Method))
            {
                proxyRequest.Content = new StreamContent(ctx.Request.Body);
                if (ctx.Request.ContentType is { } contentType)
                {
                    proxyRequest.Content.Headers.TryAddWithoutValidation("Content-Type", contentType);
                }
            }

            foreach (var header in ctx.Request.Headers)
            {
                if (header.Key.Equals("Host", StringComparison.OrdinalIgnoreCase) || HopByHopHeaders.Contains(header.Key))
                {
                    continue;
                }

                proxyRequest.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
            }

            using var response = await Client.SendAsync(proxyRequest, HttpCompletionOption.ResponseHeadersRead);
            ctx.Response.StatusCode = (int)response.StatusCode;

            foreach (var header in response.Headers)
            {
                if (HopByHopHeaders.Contains(header.Key))
                {
                    continue;
                }

                ctx.Response.Headers[header.Key] = header.Value.ToArray();
            }

            foreach (var header in response.Content.Headers)
            {
                if (HopByHopHeaders.Contains(header.Key))
                {
                    continue;
                }

                ctx.Response.Headers[header.Key] = header.Value.ToArray();
            }

            await response.Content.CopyToAsync(ctx.Response.Body);

            RequestReceived?.Invoke(this, new RequestLogEntry(
                DateTime.Now,
                ctx.Request.Method,
                ctx.Request.Path.Value ?? string.Empty,
                ctx.Request.QueryString.Value ?? string.Empty,
                ctx.Connection.RemoteIpAddress?.ToString() ?? string.Empty,
                RequestReading.FormatHeaders(ctx.Request),
                $"→ {targetUri}",
                ctx.Response.StatusCode));
        });
    }
}
