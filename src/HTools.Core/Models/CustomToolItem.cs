namespace HTools.Core.Models;

public sealed class CustomToolItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string Name { get; set; } = string.Empty;

    public string Target { get; set; } = string.Empty;

    /// <summary>Either "exe" or "url".</summary>
    public string Kind { get; set; } = "exe";

    public string? IconPngBase64 { get; set; }
}
