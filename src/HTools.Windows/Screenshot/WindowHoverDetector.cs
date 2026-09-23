using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;

namespace HTools.Windows.Screenshot;

/// <summary>
/// Finds the top-level application window under a screen point, so the capture overlay can highlight
/// (and let the user single-click select) a whole window instead of always requiring a hand-dragged
/// rectangle — this mirrors the "hover to auto-detect window bounds" feature from the H_Assistant
/// reference project's screenshot tool.
///
/// The reference implementation walks sibling windows via FindWindowEx because a plain WindowFromPoint
/// call would just return its own capture overlay (which is itself topmost and covers the whole
/// screen). We get the same effect more simply with EnumWindows: it visits top-level windows in Z-order
/// from front to back, so skipping our own handle and returning the first remaining window whose
/// rectangle contains the point gives us "the window the user would actually see at that pixel".
/// </summary>
internal static class WindowHoverDetector
{
    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TOOLWINDOW = 0x00000080;

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    // EnumWindows takes a managed delegate as a native callback pointer, which needs classic
    // [DllImport] marshalling rather than the source-generated [LibraryImport] used elsewhere in this
    // project — LibraryImport doesn't support marshalling arbitrary instance-closure delegates.
    private delegate bool EnumWindowsProc(nint hWnd, nint lParam);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, nint lParam);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(nint hWnd);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(nint hWnd, out Rect rect);

    [DllImport("user32.dll")]
    private static extern int GetWindowTextLength(nint hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(nint hWnd, StringBuilder text, int count);

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(nint hWnd, int nIndex);

    /// <summary>
    /// Returns the bounds (in screen coordinates) and title of the topmost visible, "real" window that
    /// contains <paramref name="screenPoint"/>, or null if none qualifies. <paramref name="excludeHandle"/>
    /// must be the capture overlay's own HWND, otherwise it would always match itself first.
    /// </summary>
    public static (Rectangle Bounds, string Title)? FindWindowAt(Point screenPoint, nint excludeHandle)
    {
        (Rectangle Bounds, string Title)? found = null;

        EnumWindows((hWnd, _) =>
        {
            if (hWnd == excludeHandle || !IsWindowVisible(hWnd))
            {
                return true; // not a match — keep enumerating
            }

            // Tool windows (floating palettes, IME popups, etc.) aren't "a window" a user would want to
            // snap-select as a screenshot region, so skip them like the reference implementation's
            // visible-window-only filter effectively does.
            if ((GetWindowLong(hWnd, GWL_EXSTYLE) & WS_EX_TOOLWINDOW) != 0)
            {
                return true;
            }

            if (!GetWindowRect(hWnd, out var rect))
            {
                return true;
            }

            if (rect.Right - rect.Left <= 0 || rect.Bottom - rect.Top <= 0)
            {
                return true;
            }

            if (screenPoint.X < rect.Left || screenPoint.X >= rect.Right ||
                screenPoint.Y < rect.Top || screenPoint.Y >= rect.Bottom)
            {
                return true;
            }

            var length = GetWindowTextLength(hWnd);
            var titleBuilder = new StringBuilder(length + 1);
            GetWindowText(hWnd, titleBuilder, titleBuilder.Capacity);

            found = (Rectangle.FromLTRB(rect.Left, rect.Top, rect.Right, rect.Bottom), titleBuilder.ToString());
            return false; // stop enumerating: first match in front-to-back Z-order is the visible one
        }, 0);

        return found;
    }
}
