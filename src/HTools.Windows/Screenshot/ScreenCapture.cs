using System.Drawing;
using System.Drawing.Imaging;
using HTools.Windows.Interop;

namespace HTools.Windows.Screenshot;

public static class ScreenCapture
{
    /// <summary>The bounding box of the full virtual desktop — the union of every attached monitor,
    /// which is what makes cross-screen selection possible. Can have a negative X/Y when a monitor is
    /// positioned above/left of the primary one.</summary>
    public static Rectangle GetVirtualScreenBounds()
    {
        var x = NativeMethods.GetSystemMetrics(NativeMethods.SM_XVIRTUALSCREEN);
        var y = NativeMethods.GetSystemMetrics(NativeMethods.SM_YVIRTUALSCREEN);
        var width = NativeMethods.GetSystemMetrics(NativeMethods.SM_CXVIRTUALSCREEN);
        var height = NativeMethods.GetSystemMetrics(NativeMethods.SM_CYVIRTUALSCREEN);
        return new Rectangle(x, y, width, height);
    }

    public static Bitmap CaptureVirtualScreen()
    {
        var bounds = GetVirtualScreenBounds();
        var bitmap = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size, CopyPixelOperation.SourceCopy);
        return bitmap;
    }
}
