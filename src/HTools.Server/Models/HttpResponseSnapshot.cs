namespace HTools.Server.Models;

public sealed record HttpResponseSnapshot(
    bool Success,
    int StatusCode,
    string Headers,
    string Body,
    long DurationMs,
    string? Error);
