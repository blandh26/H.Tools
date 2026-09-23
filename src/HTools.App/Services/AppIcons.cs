using Avalonia.Media;
using Avalonia.Svg.Skia;

namespace HTools.App.Services;

/// <summary>
/// Colorful SVG replacements for a handful of tool-card icons that looked flat/low-quality as plain
/// emoji glyphs (see HomeView's card template) — same approach as the screenshot overlay's own toolbar
/// icons (HTools.Windows.Screenshot.ScreenshotIcons), just rendered through Avalonia.Svg.Skia here
/// instead of System.Drawing since this runs in the Avalonia UI layer. Only tools with a genuinely
/// better custom icon are listed here; everything else keeps its plain emoji glyph.
/// </summary>
internal static class AppIcons
{
    private static readonly Dictionary<string, string> Markup = new()
    {
        // Cursor arrow with a fading pink trail behind it — echoes the actual plum-blossom mouse trail
        // effect this tool draws, rather than a generic mouse-device glyph.
        ["mouse-effect"] = """
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24">
              <circle cx="6" cy="17" r="2" fill="#F472B6" opacity="0.35"/>
              <circle cx="9" cy="14" r="2.4" fill="#F472B6" opacity="0.55"/>
              <circle cx="12" cy="11" r="2.8" fill="#EC4899" opacity="0.8"/>
              <path d="M9 3 L9 18 L12.5 14.8 L15 20 L17 19 L14.7 13.8 L19.5 13.2 Z" fill="#374151"/>
            </svg>
            """,

        // A crop-frame (corner brackets, the universal screenshot-tool visual language) around a small
        // captured scene, rather than a pair of scissors that reads more like "cut" than "capture".
        ["screenshot"] = """
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24">
              <path d="M4 8 V4 H8" stroke="#3B82F6" stroke-width="2.4" fill="none" stroke-linecap="round"/>
              <path d="M16 4 H20 V8" stroke="#3B82F6" stroke-width="2.4" fill="none" stroke-linecap="round"/>
              <path d="M20 16 V20 H16" stroke="#3B82F6" stroke-width="2.4" fill="none" stroke-linecap="round"/>
              <path d="M8 20 H4 V16" stroke="#3B82F6" stroke-width="2.4" fill="none" stroke-linecap="round"/>
              <rect x="7" y="7" width="10" height="10" rx="1.5" fill="#93C5FD"/>
              <path d="M7 15 L10 11 L12.5 13.5 L15 10 L17 15 Z" fill="#3B82F6"/>
              <circle cx="10" cy="9.5" r="1.2" fill="#FDE047"/>
            </svg>
            """,
    };

    private static readonly Dictionary<string, IImage> Cache = [];

    /// <summary>Returns the rendered icon for a tool id, or null if that tool has no custom icon (the
    /// caller should fall back to the plain emoji glyph in that case). Cached — every card for the same
    /// tool id shares one rendered image rather than re-parsing the SVG each time.</summary>
    public static IImage? TryGet(string toolId)
    {
        if (Cache.TryGetValue(toolId, out var cached))
        {
            return cached;
        }

        if (!Markup.TryGetValue(toolId, out var svg))
        {
            return null;
        }

        var source = SvgSource.LoadFromSvg(svg);
        var image = new SvgImage { Source = source };
        Cache[toolId] = image;
        return image;
    }
}
