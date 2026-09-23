using System.Drawing;
using Svg;

namespace HTools.Windows.Screenshot;

/// <summary>
/// Renders the capture toolbar's colorful icons from small hand-authored inline SVG markup (via the
/// Svg.NET package) instead of shipping bitmap image assets or falling back to plain Unicode glyphs —
/// this keeps every icon crisp at any size/DPI and easy to read/tweak as plain text right here, with no
/// external files to manage. Each icon uses its own accent color so the toolbar reads as a set of
/// distinct colorful actions rather than a wall of identical monochrome symbols.
/// </summary>
internal static class ScreenshotIcons
{
    // Rendered bitmaps are cached per (name, pixel size) — the same icon is only ever requested at one
    // size per button, but the capture overlay (and therefore its toolbar) can be built and torn down
    // repeatedly in one app session, so caching avoids re-parsing/re-rasterizing the SVG every time.
    private static readonly Dictionary<(string Name, int Size), Bitmap> Cache = [];

    private static readonly Dictionary<string, string> Markup = new()
    {
        ["rectangle"] = """
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24">
              <rect x="4" y="6" width="16" height="12" rx="1.5" fill="none" stroke="#3B82F6" stroke-width="2.4"/>
            </svg>
            """,
        ["rectangle-filled"] = """
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24">
              <rect x="4" y="6" width="16" height="12" rx="1.5" fill="#3B82F6"/>
            </svg>
            """,
        ["ellipse"] = """
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24">
              <ellipse cx="12" cy="12" rx="8" ry="6" fill="none" stroke="#A855F7" stroke-width="2.4"/>
            </svg>
            """,
        ["ellipse-filled"] = """
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24">
              <ellipse cx="12" cy="12" rx="8" ry="6" fill="#A855F7"/>
            </svg>
            """,
        ["line"] = """
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24">
              <line x1="5" y1="19" x2="19" y2="5" stroke="#14B8A6" stroke-width="2.6" stroke-linecap="round"/>
            </svg>
            """,
        ["arrow"] = """
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24">
              <path d="M5 19 L17 7" stroke="#F97316" stroke-width="2.6" stroke-linecap="round" fill="none"/>
              <path d="M11 6 L18 6 L18 13 Z" fill="#F97316"/>
            </svg>
            """,
        ["freehand"] = """
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24">
              <path d="M4 20 L4 16 L14 6 L18 10 L8 20 Z" fill="#22C55E"/>
              <path d="M14 6 L18 2.5 L21.5 6 L18 10 Z" fill="#16A34A"/>
            </svg>
            """,
        ["text"] = """
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24">
              <path d="M5 6 H19 M12 6 V19" stroke="#EF4444" stroke-width="2.6" stroke-linecap="round" fill="none"/>
            </svg>
            """,
        ["mosaic"] = """
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24">
              <rect x="3" y="3" width="8" height="8" fill="#F59E0B"/>
              <rect x="13" y="3" width="8" height="8" fill="#3B82F6"/>
              <rect x="3" y="13" width="8" height="8" fill="#3B82F6"/>
              <rect x="13" y="13" width="8" height="8" fill="#F59E0B"/>
            </svg>
            """,
        ["undo"] = """
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24">
              <path d="M7 7 H15 A5 5 0 1 1 10 15" fill="none" stroke="#9CA3AF" stroke-width="2.4" stroke-linecap="round"/>
              <polygon points="7,3 7,11 2,7" fill="#9CA3AF"/>
            </svg>
            """,
        ["pin"] = """
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24">
              <circle cx="12" cy="8" r="5" fill="#EF4444"/>
              <rect x="11" y="12" width="2" height="9" rx="1" fill="#B91C1C"/>
            </svg>
            """,
        ["copy"] = """
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24">
              <rect x="7" y="4" width="12" height="16" rx="2" fill="#3B82F6"/>
              <rect x="9.5" y="2" width="7" height="4" rx="1" fill="#93C5FD"/>
              <rect x="9.5" y="9" width="7" height="1.6" fill="white"/>
              <rect x="9.5" y="12.5" width="7" height="1.6" fill="white"/>
              <rect x="9.5" y="16" width="4.5" height="1.6" fill="white"/>
            </svg>
            """,
        ["save"] = """
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24">
              <rect x="4" y="4" width="16" height="16" rx="2" fill="#22C55E"/>
              <rect x="7" y="4" width="10" height="6" fill="#166534"/>
              <rect x="7" y="13" width="10" height="5" fill="white"/>
            </svg>
            """,
        ["check"] = """
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24">
              <circle cx="12" cy="12" r="9" fill="#22C55E"/>
              <path d="M7.5 12.5 L10.5 15.5 L16.5 9" stroke="white" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round" fill="none"/>
            </svg>
            """,
        ["cancel"] = """
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24">
              <circle cx="12" cy="12" r="9" fill="#EF4444"/>
              <path d="M8.5 8.5 L15.5 15.5 M15.5 8.5 L8.5 15.5" stroke="white" stroke-width="2.2" stroke-linecap="round"/>
            </svg>
            """,
        ["palette"] = """
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24">
              <path d="M12 3 C6.5 3 2 7 2 12 C2 16.5 5.5 19 9 19 C10 19 10.5 18.3 10.5 17.5 C10.5 17 10.2 16.6 10.2 16.1 C10.2 15.2 10.9 14.6 11.8 14.6 H14 C18 14.6 21 12 21 8.5 C21 5.4 17 3 12 3 Z" fill="#E5E7EB"/>
              <circle cx="7" cy="9" r="1.6" fill="#EF4444"/>
              <circle cx="12" cy="6.5" r="1.6" fill="#F59E0B"/>
              <circle cx="17" cy="9" r="1.6" fill="#3B82F6"/>
              <circle cx="8" cy="14" r="1.6" fill="#22C55E"/>
            </svg>
            """,
    };

    /// <summary>Rasterizes the named icon to a square <paramref name="size"/>x<paramref name="size"/>
    /// bitmap with alpha transparency, caching the result. Returned bitmaps are cache-owned — callers
    /// must not dispose them (they're reused for the lifetime of the process, and every button paints
    /// from the same shared instance rather than each holding its own copy).</summary>
    public static Bitmap Get(string name, int size)
    {
        var key = (name, size);
        if (Cache.TryGetValue(key, out var cached))
        {
            return cached;
        }

        var document = SvgDocument.FromSvg<SvgDocument>(Markup[name]);
        var bitmap = document.Draw(size, size);
        Cache[key] = bitmap;
        return bitmap;
    }
}
