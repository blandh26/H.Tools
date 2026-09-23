namespace HTools.Server.Models;

public sealed record RequestLogEntry(
    DateTime ReceivedAt,
    string Method,
    string Path,
    string QueryString,
    string RemoteAddress,
    string Headers,
    string Body,
    int ResponseStatusCode);
