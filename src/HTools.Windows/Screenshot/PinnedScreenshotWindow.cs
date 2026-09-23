using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using HTools.Windows.Interop;

namespace HTools.Windows.Screenshot;

/// <summary>
/// A small, always-on-top, borderless window that keeps a captured region visible on the desktop —
/// the "pin to screen" action from the reference project's FrmOut. It takes ownership of the bitmap
/// it's given (disposes it when closed). Left/middle-drag anywhere on the image moves the window, the
/// mouse wheel zooms it (re-centered on the cursor so the point under the mouse doesn't jump), a
/// double-click toggles between the original size and a small thumbnail, and a right-click closes it.
/// </summary>
public sealed class PinnedScreenshotWindow : Form
{
    private const float ZoomStep = 0.1f;
    private const float MinScale = 0.2f;
    private const float MaxScale = 5f;
    private const float ThumbnailScale = 0.3f;

    private readonly Bitmap _image;
    private readonly Size _originalSize;
    private float _scale = 1f;
    private bool _dragging;
    private Point _dragAnchor;

    public PinnedScreenshotWindow(Bitmap image)
    {
        _image = image;
        _originalSize = image.Size;

        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        TopMost = true;
        ShowInTaskbar = false;
        DoubleBuffered = true;
        BackColor = Color.Black;
        Location = Cursor.Position;
        ClientSize = _originalSize;
    }

    protected override CreateParams CreateParams
    {
        get
        {
            // Keep this out of the taskbar and Alt+Tab — it's a floating annotation aid, not a real
            // application window, the same treatment the capture overlay itself gets.
            var cp = base.CreateParams;
            cp.ExStyle |= NativeMethods.WS_EX_TOOLWINDOW;
            return cp;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _image.Dispose();
        }

        base.Dispose(disposing);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        // Nearest-neighbor when zoomed in keeps pixel edges crisp (matches how the magnifier and the
        // reference project's own zoom both render); bilinear only kicks in once shrinking below 1:1,
        // where crisp-but-aliased downscaling would look noisy.
        e.Graphics.InterpolationMode = _scale >= 1f ? InterpolationMode.NearestNeighbor : InterpolationMode.HighQualityBilinear;
        e.Graphics.DrawImage(_image, new Rectangle(Point.Empty, ClientSize));

        using var border = new Pen(Color.FromArgb(180, 255, 255, 255), 1f);
        e.Graphics.DrawRectangle(border, 0, 0, ClientSize.Width - 1, ClientSize.Height - 1);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button is MouseButtons.Left or MouseButtons.Middle)
        {
            _dragging = true;
            _dragAnchor = e.Location;
        }
        else if (e.Button == MouseButtons.Right)
        {
            Close();
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (_dragging)
        {
            Location = new Point(Location.X + e.X - _dragAnchor.X, Location.Y + e.Y - _dragAnchor.Y);
        }
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        _dragging = false;
    }

    protected override void OnMouseDoubleClick(MouseEventArgs e)
    {
        base.OnMouseDoubleClick(e);
        _scale = _scale == 1f ? ThumbnailScale : 1f;
        ApplyScale();
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        base.OnMouseWheel(e);

        var newScale = Math.Clamp(_scale + (e.Delta > 0 ? ZoomStep : -ZoomStep), MinScale, MaxScale);
        if (Math.Abs(newScale - _scale) < 0.001f)
        {
            return;
        }

        // Re-anchor on the cursor: without this, zooming would grow/shrink the window from its
        // top-left corner and the pixel the user is pointing at would visibly drift away from the cursor.
        var cursorScreen = PointToScreen(e.Location);
        var ratio = newScale / _scale;
        var newLocation = new Point(
            (int)(cursorScreen.X - ((cursorScreen.X - Location.X) * ratio)),
            (int)(cursorScreen.Y - ((cursorScreen.Y - Location.Y) * ratio)));

        _scale = newScale;
        Location = newLocation;
        ApplyScale();
    }

    private void ApplyScale()
    {
        ClientSize = new Size((int)(_originalSize.Width * _scale), (int)(_originalSize.Height * _scale));
        Invalidate();
    }
}
