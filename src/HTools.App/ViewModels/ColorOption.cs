using Avalonia.Media;

namespace HTools.App.ViewModels;

public sealed record ColorOption(string Key, string DisplayName, IBrush Swatch)
{
    public static ColorOption Create(string key, string displayName, Color color) =>
        new(key, displayName, new SolidColorBrush(color));
}
