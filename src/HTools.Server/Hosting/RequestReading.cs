using System.Text;
using Microsoft.AspNetCore.Http;

namespace HTools.Server.Hosting;

internal static class RequestReading
{
    private const int MaxBodyBytes = 64 * 1024;

    public static async Task<string> ReadBodyAsync(HttpRequest request)
    {
        if (!request.Body.CanRead)
        {
            return string.Empty;
        }

        request.EnableBuffering();
        using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
        var buffer = new char[MaxBodyBytes];
        var read = await reader.ReadBlockAsync(buffer, 0, buffer.Length);
        request.Body.Position = 0;
        return new string(buffer, 0, read);
    }

    public static string FormatHeaders(HttpRequest request)
    {
        var sb = new StringBuilder();
        foreach (var header in request.Headers)
        {
            sb.AppendLine($"{header.Key}: {string.Join(", ", header.Value.ToArray())}");
        }

        return sb.ToString().TrimEnd();
    }
}
