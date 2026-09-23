namespace HTools.App.Models;

/// <summary>Static metadata for one tool card. <see cref="IsAvailable"/> false renders a "coming soon" page.</summary>
public sealed record ToolDescriptor(
    string Id,
    string GroupKey,
    string Icon,
    string NameKey,
    string DescriptionKey,
    bool IsAvailable);
