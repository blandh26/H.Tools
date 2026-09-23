using System.Diagnostics;
using System.Text;
using HTools.Server.Models;

namespace HTools.Server.Http;

/// <summary>Ad-hoc HTTP client used by the mock-server tool's "client" tab to send one-off requests.</summary>
public sealed class HttpRequestSender
{
    private static readonly HttpClient Client = new()
    {
        Timeout = TimeSpan.FromSeconds(30),
    };

    public async Task<HttpResponseSnapshot> SendAsync(
        string method,
        string url,
        IReadOnlyList<(string Key, string Value)> headers,
        string? body,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            using var request = new HttpRequestMessage(new HttpMethod(method), url);

            if (!string.IsNullOrEmpty(body) && method is not ("GET" or "HEAD"))
            {
                request.Content = new StringContent(body, Encoding.UTF8);
            }

            foreach (var (key, value) in headers)
            {
                if (string.IsNullOrWhiteSpace(key))
                {
                    continue;
                }

                if (!request.Headers.TryAddWithoutValidation(key, value))
                {
                    request.Content?.Headers.TryAddWithoutValidation(key, value);
                }
            }

            using var response = await Client.SendAsync(request, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            stopwatch.Stop();

            var headerText = FormatHeaders(response);
            return new HttpResponseSnapshot(true, (int)response.StatusCode, headerText, responseBody, stopwatch.ElapsedMilliseconds, null);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return new HttpResponseSnapshot(false, 0, string.Empty, string.Empty, stopwatch.ElapsedMilliseconds, ex.Message);
        }
    }

    private static string FormatHeaders(HttpResponseMessage response)
    {
        var sb = new StringBuilder();
        foreach (var header in response.Headers)
        {
            sb.AppendLine($"{header.Key}: {string.Join(", ", header.Value)}");
        }

        foreach (var header in response.Content.Headers)
        {
            sb.AppendLine($"{header.Key}: {string.Join(", ", header.Value)}");
        }

        return sb.ToString().TrimEnd();
    }
}
