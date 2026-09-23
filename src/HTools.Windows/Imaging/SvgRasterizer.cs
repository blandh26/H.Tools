using System.Drawing.Imaging;
using Svg;

namespace HTools.Windows.Imaging;

/// <summary>
/// Rasterizes SVG markup to PNG bytes via Svg.NET (System.Drawing), so the Avalonia UI layer can load
/// the result with its own built-in PNG decoder instead of taking a dependency on an Avalonia-specific
/// SVG package. That matters: Avalonia.Svg.Skia 11.x is compiled against Avalonia 11 and throws a
/// TypeLoadException on every frame under Avalonia 12 (the type
/// Avalonia.Platform.OptionalFeatureProviderExtensions it calls no longer exists), which stopped the
/// whole window from ever painting. Svg.NET has no Avalonia dependency at all, so it can't break that way.
/// </summary>
public static class SvgRasterizer
{
    public static byte[]? ConvertIconToPng(byte[] iconBytes)
    {
        try
        {
            using var stream = new MemoryStream(iconBytes);
            using var icon = new System.Drawing.Icon(stream);
            using var bitmap = icon.ToBitmap();
            using var output = new MemoryStream();
            bitmap.Save(output, ImageFormat.Png);
            return output.ToArray();
        }
        catch
        {
            return null;
        }
    }

    public static byte[]? ExtractExecutableIcon(string executablePath)
    {
        try
        {
            using var icon = System.Drawing.Icon.ExtractAssociatedIcon(executablePath);
            if (icon is null)
            {
                return null;
            }

            using var bitmap = icon.ToBitmap();
            using var stream = new MemoryStream();
            bitmap.Save(stream, ImageFormat.Png);
            return stream.ToArray();
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Renders <paramref name="svgMarkup"/> to a square <paramref name="size"/>-pixel PNG with
    /// an alpha channel.</summary>
    public static byte[] RenderPng(string svgMarkup, int size)
    {
        var document = SvgDocument.FromSvg<SvgDocument>(svgMarkup);
        using var bitmap = document.Draw(size, size);
        using var stream = new MemoryStream();
        bitmap.Save(stream, ImageFormat.Png);
        return stream.ToArray();
    }
}
