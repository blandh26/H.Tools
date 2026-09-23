namespace HTools.Server.Modules;

public sealed record MockApiRule(string Method, string Path, int StatusCode, string ContentType, string Body);
