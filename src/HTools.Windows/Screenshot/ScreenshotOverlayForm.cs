using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using HTools.Windows.Interop;

namespace HTools.Windows.Screenshot;

/// <summary>
/// A borderless, topmost window sized to the whole virtual desktop (so selection can cross monitor
/// boundaries), showing a frozen screenshot as its background. The interaction model is rebuilt to
/// match the "core experience" of the H_Assistant reference project's screenshot tool:
///   - before a region is picked, hovering highlights the window under the cursor (click it to select
///     it outright) and a pixel-level magnifier follows the cursor;
///   - once a region is picked, 8 resize handles let it be adjusted before committing to annotation;
///   - annotations (rectangle/ellipse — hollow or filled, line, arrow, freehand, text, mosaic) are
///     "burned in" to a working bitmap on each commit, with undo via a plain stack of full-bitmap
///     snapshots — simpler than a vector object model and works uniformly for pixel-level tools like
///     mosaic (this part matches the reference project's own approach, not just a coincidence);
///   - a secondary options row (16 preset colors + a custom-color picker, 5 pen sizes) appears under
///     the toolbar whenever a drawing tool is active;
///   - finishing actions are undo / pin-to-screen (floating always-on-top copy) / copy-to-clipboard /
///     save-to-file / cancel.
/// Deliberately NOT reproduced from the reference project (by explicit scope decision, not oversight):
/// its "repeat last region" auto-capture mode, its "contrast/diff" floating compare window, the V/T
/// visible-window-only toggle hotkeys, the WASD cursor-nudge keys, and its known bugs (e.g. a virtual-
/// screen Y-origin calculation that silently no-ops).
/// </summary>
public sealed class ScreenshotOverlayForm : Form
{
    private const int HandleSize = 8;
    private const int ToolbarGap = 8;
    private const int OptionsGap = 4;
    private const int MinSelectionSize = 4;

    // Every toolbar/options-row control is sized+margined to occupy exactly this much vertical space
    // (ButtonSize + its own top/bottom margin), so a FlowLayoutPanel row — whose height is driven by
    // its tallest child — comes out to one consistent height instead of jittering per-row depending on
    // which controls (buttons vs. the separator) happen to be tallest that row.
    private const int ButtonSize = 30;
    private const int ButtonCellHeight = 36; // ButtonSize + 3px top/bottom margin
    private const int CornerRadius = 6;
    private static readonly Color ToolbarBackground = Color.FromArgb(248, 30, 30, 32);

    // WinForms Button.BackColor ignores alpha (it always paints opaque), so "blending into the panel"
    // means literally matching the panel's own background color rather than using a transparent one.
    private static readonly Color ButtonIdleColor = ToolbarBackground;
    private static readonly Color ButtonHoverColor = Color.FromArgb(255, 58, 58, 64);
    private static readonly Color ButtonActiveColor = Color.FromArgb(255, 24, 144, 255);

    /// <summary>Pen widths offered by the size picker — mirrors the reference project's five fixed
    /// brush sizes (its size picker doubles as the text tool's font-size increment, see <see cref="BeginTextEntry"/>).</summary>
    private static readonly int[] PenSizes = [1, 3, 5, 8, 12];

    /// <summary>The 16-swatch preset palette, in the same order as the reference project's color box.</summary>
    private static readonly Color[] PresetColors =
    [
        Color.Black, Color.DimGray, Color.DarkRed, Color.DarkGoldenrod, Color.DarkGreen, Color.DarkBlue, Color.DarkViolet, Color.DarkCyan,
        Color.White, Color.DarkGray, Color.Red, Color.Yellow, Color.LightGreen, Color.Blue, Color.Fuchsia, Color.Cyan,
    ];

    private enum State
    {
        Idle,
        Dragging,
        Selected,
    }

    private enum ResizeHandle
    {
        None,
        TopLeft,
        Top,
        TopRight,
        Right,
        BottomRight,
        Bottom,
        BottomLeft,
        Left,
    }

    private readonly Rectangle _virtualBounds;
    private readonly Bitmap _screenshot;
    private Bitmap _workingBitmap;
    private readonly Stack<Bitmap> _undoStack = new();

    private State _state = State.Idle;
    private Rectangle _selection;
    private Point _dragStart;
    private Point _dragCurrent;
    private ResizeHandle _activeHandle = ResizeHandle.None;
    private Rectangle _handleStartSelection;

    // Window-hover auto-detection state, live only while State.Idle (see UpdateHoverWindow). Retained
    // through a click (not re-queried during the drag itself) so a plain click — mouse-up at the same
    // spot as mouse-down — can snap-select whatever window was under the cursor at that moment.
    private Rectangle? _hoverBounds;
    private string _hoverTitle = string.Empty;
    private Point _lastCursorPosition;

    private AnnotationTool _currentTool = AnnotationTool.None;
    private Color _annotationColor = Color.Red;
    private int _annotationSize = 5;
    private bool _isAnnotating;
    private Point _annotationStart;
    private Point _annotationCurrent;
    private readonly List<Point> _freehandPoints = [];

    private FlowLayoutPanel? _toolbar;
    private FlowLayoutPanel? _optionsPanel;
    private TextBox? _textEditor;
    private readonly Dictionary<AnnotationTool, Button> _toolButtons = new();
    private readonly List<(Color Color, Button Button)> _colorButtons = [];
    private readonly List<(int Size, Button Button)> _sizeButtons = [];
    private readonly ToolTip _toolTip = new();
    private readonly ScreenshotOverlayTexts _texts;

    public ScreenshotOverlayForm(Bitmap screenshot, Rectangle virtualBounds, ScreenshotOverlayTexts? texts = null)
    {
        _screenshot = screenshot;
        _virtualBounds = virtualBounds;
        _texts = texts ?? ScreenshotOverlayTexts.Default;
        _workingBitmap = (Bitmap)_screenshot.Clone();

        DoubleBuffered = true;
        AutoScaleMode = AutoScaleMode.None;
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        Location = virtualBounds.Location;
        ClientSize = virtualBounds.Size;
        ShowInTaskbar = false;
        TopMost = true;
        KeyPreview = true;
        Cursor = Cursors.Cross;
        BackColor = Color.Black;

        BuildToolbar();
        BuildOptionsPanel();
    }

    public event EventHandler<ScreenshotResult>? Completed;

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ExStyle |= NativeMethods.WS_EX_TOOLWINDOW;
            return cp;
        }
    }

    protected override bool ShowWithoutActivation => false;

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        Activate();
        Focus();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _workingBitmap.Dispose();
            _screenshot.Dispose();
            _toolTip.Dispose();
            while (_undoStack.Count > 0)
            {
                _undoStack.Pop().Dispose();
            }
        }

        base.Dispose(disposing);
    }

    // ------------------------------------------------------------------ painting

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.DrawImage(_workingBitmap, Point.Empty);

        switch (_state)
        {
            case State.Idle:
                if (_hoverBounds is { } hover)
                {
                    // Un-dim the hovered window's rect (reusing DrawDim's "punch a hole" behavior) and
                    // outline+label it, so it reads as "click here to select this window".
                    DrawDim(g, hover);
                    DrawHoverBorder(g, hover);
                    DrawHoverLabel(g, hover, _hoverTitle);
                }
                else
                {
                    DrawDim(g, Rectangle.Empty);
                    DrawHint(g);
                }

                DrawMagnifier(g, _lastCursorPosition);
                break;
            case State.Dragging:
                var live = NormalizedRect(_dragStart, _dragCurrent);
                DrawDim(g, live);
                DrawSelectionBorder(g, live);
                DrawSizeLabel(g, live);
                break;
            case State.Selected:
                DrawDim(g, _selection);
                DrawSelectionBorder(g, _selection);
                DrawHandles(g, _selection);
                if (!_isAnnotating)
                {
                    DrawSizeLabel(g, _selection);
                }

                DrawLivePreview(g);
                break;
        }
    }

    private void DrawDim(Graphics g, Rectangle hole)
    {
        using var dim = new SolidBrush(Color.FromArgb(120, 0, 0, 0));
        if (hole.IsEmpty)
        {
            g.FillRectangle(dim, ClientRectangle);
            return;
        }

        g.FillRectangle(dim, new Rectangle(0, 0, Width, hole.Top));
        g.FillRectangle(dim, new Rectangle(0, hole.Bottom, Width, Height - hole.Bottom));
        g.FillRectangle(dim, new Rectangle(0, hole.Top, hole.Left, hole.Height));
        g.FillRectangle(dim, new Rectangle(hole.Right, hole.Top, Width - hole.Right, hole.Height));
    }

    private static void DrawSelectionBorder(Graphics g, Rectangle rect)
    {
        using var pen = new Pen(Color.FromArgb(255, 40, 160, 255), 1.5f);
        g.DrawRectangle(pen, rect);
    }

    private static void DrawHoverBorder(Graphics g, Rectangle rect)
    {
        using var pen = new Pen(Color.FromArgb(255, 0, 220, 220), 2f);
        g.DrawRectangle(pen, rect);
    }

    private static void DrawHoverLabel(Graphics g, Rectangle rect, string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return;
        }

        using var font = new Font("Microsoft YaHei UI", 9.5f);
        var size = g.MeasureString(title, font, 400);
        var labelWidth = Math.Min(size.Width + 8, 400);
        var labelY = rect.Top - size.Height - 6;
        if (labelY < 0)
        {
            labelY = rect.Top + 4;
        }

        var backRect = new RectangleF(rect.Left, labelY, labelWidth, size.Height + 2);
        using var backBrush = new SolidBrush(Color.FromArgb(200, 20, 20, 20));
        g.FillRectangle(backBrush, backRect);
        using var textBrush = new SolidBrush(Color.White);
        g.DrawString(title, font, textBrush, new RectangleF(backRect.X + 4, backRect.Y + 1, labelWidth - 8, size.Height));
    }

    private void DrawHint(Graphics g)
    {
        var text = _texts.Hint;
        using var font = new Font("Microsoft YaHei UI", 14f, FontStyle.Regular);
        var size = g.MeasureString(text, font);
        var x = (Width - size.Width) / 2f;
        var y = Height * 0.12f;
        using var shadowBrush = new SolidBrush(Color.FromArgb(180, 0, 0, 0));
        g.DrawString(text, font, shadowBrush, x + 1, y + 1);
        using var brush = new SolidBrush(Color.White);
        g.DrawString(text, font, brush, x, y);
    }

    private static void DrawSizeLabel(Graphics g, Rectangle rect)
    {
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        var text = $"{rect.Width} × {rect.Height}";
        using var font = new Font("Consolas", 9.5f, FontStyle.Regular);
        var size = g.MeasureString(text, font);
        var labelY = rect.Top - size.Height - 6;
        if (labelY < 0)
        {
            labelY = rect.Top + 4;
        }

        var backRect = new RectangleF(rect.Left, labelY, size.Width + 8, size.Height + 2);
        using var backBrush = new SolidBrush(Color.FromArgb(200, 20, 20, 20));
        g.FillRectangle(backBrush, backRect);
        using var textBrush = new SolidBrush(Color.White);
        g.DrawString(text, font, textBrush, backRect.X + 4, backRect.Y + 1);
    }

    private void DrawHandles(Graphics g, Rectangle rect)
    {
        using var brush = new SolidBrush(Color.White);
        using var pen = new Pen(Color.FromArgb(255, 40, 160, 255), 1.5f);
        foreach (var handleRect in EnumerateHandleRects(rect))
        {
            g.FillRectangle(brush, handleRect);
            g.DrawRectangle(pen, handleRect);
        }
    }

    private static IEnumerable<Rectangle> EnumerateHandleRects(Rectangle rect)
    {
        var points = new[]
        {
            new Point(rect.Left, rect.Top),
            new Point(rect.Left + rect.Width / 2, rect.Top),
            new Point(rect.Right, rect.Top),
            new Point(rect.Right, rect.Top + rect.Height / 2),
            new Point(rect.Right, rect.Bottom),
            new Point(rect.Left + rect.Width / 2, rect.Bottom),
            new Point(rect.Left, rect.Bottom),
            new Point(rect.Left, rect.Top + rect.Height / 2),
        };

        foreach (var p in points)
        {
            yield return new Rectangle(p.X - HandleSize / 2, p.Y - HandleSize / 2, HandleSize, HandleSize);
        }
    }

    /// <summary>
    /// Pixel-level color-picker magnifier, shown only before a selection is made (State.Idle). Samples
    /// a small 15x15 block of the frozen screenshot centered on the cursor, upscales it 7x with
    /// nearest-neighbor (so individual source pixels stay crisp blocks rather than blurring), and draws
    /// a crosshair plus an RGB/hex readout for the exact pixel under the cursor — mirrors the reference
    /// project's magnifier, which exists to let the user aim precisely (and eyeball colors) before
    /// committing to a drag.
    /// </summary>
    private void DrawMagnifier(Graphics g, Point cursor)
    {
        const int sampleRadius = 7;
        const int sampleSize = (sampleRadius * 2) + 1;
        const int zoom = 7;

        var sampleRect = new Rectangle(cursor.X - sampleRadius, cursor.Y - sampleRadius, sampleSize, sampleSize);
        var clamped = Rectangle.Intersect(sampleRect, new Rectangle(Point.Empty, _screenshot.Size));
        if (clamped.Width <= 0 || clamped.Height <= 0)
        {
            return;
        }

        using var sample = _screenshot.Clone(clamped, _screenshot.PixelFormat);
        var magnifiedSize = new Size(sampleSize * zoom, sampleSize * zoom);
        var panelWidth = magnifiedSize.Width + 2;
        var panelHeight = magnifiedSize.Height + 2 + 22;

        // Offset the panel from the cursor, but flip to the opposite side whenever it would run off
        // the edge of the (possibly multi-monitor-spanning) virtual desktop.
        var panelX = cursor.X + 20;
        var panelY = cursor.Y + 20;
        if (panelX + panelWidth > Width)
        {
            panelX = cursor.X - 20 - panelWidth;
        }

        if (panelY + panelHeight > Height)
        {
            panelY = cursor.Y - 20 - panelHeight;
        }

        panelX = Math.Max(0, panelX);
        panelY = Math.Max(0, panelY);

        using (var backBrush = new SolidBrush(Color.FromArgb(200, 20, 20, 20)))
        {
            g.FillRectangle(backBrush, panelX, panelY, panelWidth, panelHeight);
        }

        var imageRect = new Rectangle(panelX + 1, panelY + 1, magnifiedSize.Width, magnifiedSize.Height);
        var previousInterpolation = g.InterpolationMode;
        var previousPixelOffset = g.PixelOffsetMode;
        g.InterpolationMode = InterpolationMode.NearestNeighbor;
        g.PixelOffsetMode = PixelOffsetMode.Half;
        g.DrawImage(sample, imageRect);
        g.InterpolationMode = previousInterpolation;
        g.PixelOffsetMode = previousPixelOffset;

        using (var borderPen = new Pen(Color.White, 1f))
        {
            g.DrawRectangle(borderPen, imageRect);
        }

        var centerX = imageRect.X + (imageRect.Width / 2);
        var centerY = imageRect.Y + (imageRect.Height / 2);
        using (var crossPen = new Pen(Color.FromArgb(125, 0, 255, 255), 1f))
        {
            g.DrawLine(crossPen, imageRect.Left, centerY, imageRect.Right, centerY);
            g.DrawLine(crossPen, centerX, imageRect.Top, centerX, imageRect.Bottom);
        }

        var pixelX = Math.Clamp(cursor.X, 0, _screenshot.Width - 1);
        var pixelY = Math.Clamp(cursor.Y, 0, _screenshot.Height - 1);
        var pixelColor = _screenshot.GetPixel(pixelX, pixelY);
        using (var swatchBrush = new SolidBrush(pixelColor))
        using (var swatchPen = new Pen(Color.Cyan, 1f))
        {
            var swatchRect = new Rectangle(imageRect.Right - 12, imageRect.Bottom - 12, 10, 10);
            g.FillRectangle(swatchBrush, swatchRect);
            g.DrawRectangle(swatchPen, swatchRect);
        }

        var readout = $"{pixelColor.R},{pixelColor.G},{pixelColor.B}  #{pixelColor.R:X2}{pixelColor.G:X2}{pixelColor.B:X2}";
        using var font = new Font("Consolas", 9.5f);
        using var textBrush = new SolidBrush(Color.White);
        g.DrawString(readout, font, textBrush, panelX + 4, imageRect.Bottom + 4);
    }

    private void DrawLivePreview(Graphics g)
    {
        if (!_isAnnotating)
        {
            return;
        }

        using var pen = new Pen(_annotationColor, _annotationSize) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        using var brush = new SolidBrush(_annotationColor);
        switch (_currentTool)
        {
            case AnnotationTool.Rectangle:
                g.DrawRectangle(pen, NormalizedRect(_annotationStart, _annotationCurrent));
                break;
            case AnnotationTool.RectangleFilled:
                g.FillRectangle(brush, NormalizedRect(_annotationStart, _annotationCurrent));
                break;
            case AnnotationTool.Ellipse:
                g.DrawEllipse(pen, NormalizedRect(_annotationStart, _annotationCurrent));
                break;
            case AnnotationTool.EllipseFilled:
                g.FillEllipse(brush, NormalizedRect(_annotationStart, _annotationCurrent));
                break;
            case AnnotationTool.Line:
                g.DrawLine(pen, _annotationStart, _annotationCurrent);
                break;
            case AnnotationTool.Arrow:
                DrawArrow(g, pen, _annotationStart, _annotationCurrent);
                break;
            case AnnotationTool.Freehand:
                if (_freehandPoints.Count > 1)
                {
                    g.DrawLines(pen, _freehandPoints.ToArray());
                }

                break;
            case AnnotationTool.Mosaic:
                // The mosaic itself is only computed on commit (see ApplyMosaic) — while dragging we
                // just show a dashed outline of the area that will be pixelated, same as the reference
                // project (its mosaic texture is precomputed once per commit too, not live per-frame).
                using (var dashPen = new Pen(Color.White, 1f) { DashStyle = DashStyle.Dash })
                {
                    g.DrawRectangle(dashPen, NormalizedRect(_annotationStart, _annotationCurrent));
                }

                break;
        }
    }

    private static void DrawArrow(Graphics g, Pen pen, Point start, Point end)
    {
        g.DrawLine(pen, start, end);

        var angle = Math.Atan2(end.Y - start.Y, end.X - start.X);
        const double headAngle = Math.PI / 7;
        const double headLength = 18;

        var p1 = new PointF(
            (float)(end.X - (headLength * Math.Cos(angle - headAngle))),
            (float)(end.Y - (headLength * Math.Sin(angle - headAngle))));
        var p2 = new PointF(
            (float)(end.X - (headLength * Math.Cos(angle + headAngle))),
            (float)(end.Y - (headLength * Math.Sin(angle + headAngle))));

        using var headBrush = new SolidBrush(pen.Color);
        g.FillPolygon(headBrush, [new PointF(end.X, end.Y), p1, p2]);
    }

    private static Rectangle NormalizedRect(Point a, Point b)
    {
        var x = Math.Min(a.X, b.X);
        var y = Math.Min(a.Y, b.Y);
        var w = Math.Abs(a.X - b.X);
        var h = Math.Abs(a.Y - b.Y);
        return new Rectangle(x, y, w, h);
    }

    // ------------------------------------------------------------------ mouse handling

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button != MouseButtons.Left)
        {
            return;
        }

        switch (_state)
        {
            case State.Idle:
                _state = State.Dragging;
                _dragStart = e.Location;
                _dragCurrent = e.Location;
                Invalidate();
                break;

            case State.Selected:
                var handle = HitTestHandle(e.Location);
                if (handle != ResizeHandle.None)
                {
                    _activeHandle = handle;
                    _handleStartSelection = _selection;
                    _dragStart = e.Location;
                    return;
                }

                if (_currentTool != AnnotationTool.None && _selection.Contains(e.Location))
                {
                    StartAnnotation(e.Location);
                }

                break;
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        _lastCursorPosition = e.Location;

        if (_state == State.Idle)
        {
            // No drag/selection in progress yet: keep the hover-window highlight and magnifier live.
            UpdateHoverWindow(e.Location);
            Invalidate();
            return;
        }

        if (_state == State.Dragging)
        {
            _dragCurrent = e.Location;
            Invalidate();
            return;
        }

        if (_activeHandle != ResizeHandle.None)
        {
            ResizeSelection(e.Location);
            Invalidate();
            return;
        }

        if (_isAnnotating)
        {
            _annotationCurrent = ClampToSelection(e.Location);
            if (_currentTool == AnnotationTool.Freehand)
            {
                _freehandPoints.Add(_annotationCurrent);
            }

            Invalidate();
            return;
        }

        if (_state == State.Selected)
        {
            Cursor = HitTestHandle(e.Location) switch
            {
                ResizeHandle.TopLeft or ResizeHandle.BottomRight => Cursors.SizeNWSE,
                ResizeHandle.TopRight or ResizeHandle.BottomLeft => Cursors.SizeNESW,
                ResizeHandle.Top or ResizeHandle.Bottom => Cursors.SizeNS,
                ResizeHandle.Left or ResizeHandle.Right => Cursors.SizeWE,
                _ => _currentTool == AnnotationTool.None ? Cursors.Default : Cursors.Cross,
            };
        }
    }

    /// <summary>Queries <see cref="WindowHoverDetector"/> for the window under the cursor and converts
    /// its screen-coordinate bounds into this form's local (virtual-desktop-relative) coordinate space
    /// — the same space every other rectangle field on this form (selection, drag points, etc.) lives in.</summary>
    private void UpdateHoverWindow(Point clientLocation)
    {
        var screenPoint = new Point(clientLocation.X + _virtualBounds.X, clientLocation.Y + _virtualBounds.Y);
        var found = WindowHoverDetector.FindWindowAt(screenPoint, Handle);
        if (found is { } match)
        {
            var local = new Rectangle(
                match.Bounds.X - _virtualBounds.X,
                match.Bounds.Y - _virtualBounds.Y,
                match.Bounds.Width,
                match.Bounds.Height);
            _hoverBounds = Rectangle.Intersect(local, ClientRectangle);
            _hoverTitle = match.Title;
        }
        else
        {
            _hoverBounds = null;
            _hoverTitle = string.Empty;
        }
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (e.Button != MouseButtons.Left)
        {
            return;
        }

        if (_state == State.Dragging)
        {
            var rect = NormalizedRect(_dragStart, _dragCurrent);
            if (rect.Width < MinSelectionSize || rect.Height < MinSelectionSize)
            {
                if (_hoverBounds is { } hoverAtClick)
                {
                    // A plain click (no meaningful drag) on a window we were highlighting snap-selects
                    // that window's whole bounds, instead of forcing the user to trace its outline.
                    _selection = Rectangle.Intersect(hoverAtClick, ClientRectangle);
                    _state = State.Selected;
                    ShowToolbar();
                    Invalidate();
                    return;
                }

                _state = State.Idle;
                Invalidate();
                return;
            }

            _selection = Rectangle.Intersect(rect, ClientRectangle);
            _state = State.Selected;
            ShowToolbar();
            Invalidate();
            return;
        }

        if (_activeHandle != ResizeHandle.None)
        {
            _activeHandle = ResizeHandle.None;
            PositionToolbar();
            return;
        }

        if (_isAnnotating)
        {
            CommitAnnotation();
        }
    }

    private Point ClampToSelection(Point p) => new(
        Math.Clamp(p.X, _selection.Left, _selection.Right),
        Math.Clamp(p.Y, _selection.Top, _selection.Bottom));

    private ResizeHandle HitTestHandle(Point p)
    {
        var rects = EnumerateHandleRects(_selection).ToArray();
        var order = new[] { ResizeHandle.TopLeft, ResizeHandle.Top, ResizeHandle.TopRight, ResizeHandle.Right, ResizeHandle.BottomRight, ResizeHandle.Bottom, ResizeHandle.BottomLeft, ResizeHandle.Left };
        for (var i = 0; i < rects.Length; i++)
        {
            if (rects[i].Contains(p))
            {
                return order[i];
            }
        }

        return ResizeHandle.None;
    }

    private void ResizeSelection(Point current)
    {
        var s = _handleStartSelection;
        var dx = current.X - _dragStart.X;
        var dy = current.Y - _dragStart.Y;

        var left = s.Left;
        var top = s.Top;
        var right = s.Right;
        var bottom = s.Bottom;

        if (_activeHandle is ResizeHandle.TopLeft or ResizeHandle.Left or ResizeHandle.BottomLeft)
        {
            left = s.Left + dx;
        }

        if (_activeHandle is ResizeHandle.TopLeft or ResizeHandle.Top or ResizeHandle.TopRight)
        {
            top = s.Top + dy;
        }

        if (_activeHandle is ResizeHandle.TopRight or ResizeHandle.Right or ResizeHandle.BottomRight)
        {
            right = s.Right + dx;
        }

        if (_activeHandle is ResizeHandle.BottomLeft or ResizeHandle.Bottom or ResizeHandle.BottomRight)
        {
            bottom = s.Bottom + dy;
        }

        var rect = Rectangle.FromLTRB(
            Math.Min(left, right - MinSelectionSize),
            Math.Min(top, bottom - MinSelectionSize),
            Math.Max(right, left + MinSelectionSize),
            Math.Max(bottom, top + MinSelectionSize));

        _selection = Rectangle.Intersect(rect, ClientRectangle);
    }

    // ------------------------------------------------------------------ annotations

    private void StartAnnotation(Point location)
    {
        if (_currentTool == AnnotationTool.Text)
        {
            BeginTextEntry(location);
            return;
        }

        _isAnnotating = true;
        _annotationStart = location;
        _annotationCurrent = location;
        _freehandPoints.Clear();
        _freehandPoints.Add(location);
    }

    private void CommitAnnotation()
    {
        _isAnnotating = false;

        PushUndoSnapshot();
        using var g = Graphics.FromImage(_workingBitmap);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using var pen = new Pen(_annotationColor, _annotationSize) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        using var brush = new SolidBrush(_annotationColor);

        switch (_currentTool)
        {
            case AnnotationTool.Rectangle:
                g.DrawRectangle(pen, NormalizedRect(_annotationStart, _annotationCurrent));
                break;
            case AnnotationTool.RectangleFilled:
                g.FillRectangle(brush, NormalizedRect(_annotationStart, _annotationCurrent));
                break;
            case AnnotationTool.Ellipse:
                g.DrawEllipse(pen, NormalizedRect(_annotationStart, _annotationCurrent));
                break;
            case AnnotationTool.EllipseFilled:
                g.FillEllipse(brush, NormalizedRect(_annotationStart, _annotationCurrent));
                break;
            case AnnotationTool.Line:
                g.DrawLine(pen, _annotationStart, _annotationCurrent);
                break;
            case AnnotationTool.Arrow:
                DrawArrow(g, pen, _annotationStart, _annotationCurrent);
                break;
            case AnnotationTool.Freehand:
                if (_freehandPoints.Count > 1)
                {
                    g.DrawLines(pen, _freehandPoints.ToArray());
                }

                break;
            case AnnotationTool.Mosaic:
                ApplyMosaic(NormalizedRect(_annotationStart, _annotationCurrent));
                break;
        }

        _freehandPoints.Clear();
        Invalidate();
    }

    /// <summary>
    /// Block-average pixelation: for every 10x10 block within the dragged area, every pixel in that
    /// block is replaced with the block's average color — the same algorithm (and 10px block size) as
    /// the reference project's ImageHelper.Mosaic. It operates on raw BGRA bytes via LockBits rather
    /// than per-pixel GetPixel/SetPixel calls, which would be far too slow for anything but a tiny area.
    /// Unlike the reference implementation (which precomputes a mosaic of the *entire* captured region
    /// once and reveals slices of it via a texture brush), this recomputes just the dragged area
    /// directly — same visual result for what's actually revealed, without the whole-image precompute.
    /// </summary>
    private void ApplyMosaic(Rectangle area)
    {
        area = Rectangle.Intersect(area, new Rectangle(Point.Empty, _workingBitmap.Size));
        if (area.Width < 2 || area.Height < 2)
        {
            return;
        }

        const int blockSize = 10;
        using var region = _workingBitmap.Clone(area, PixelFormat.Format32bppArgb);
        var data = region.LockBits(new Rectangle(Point.Empty, region.Size), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
        try
        {
            var stride = data.Stride;
            var bytes = new byte[stride * region.Height];
            Marshal.Copy(data.Scan0, bytes, 0, bytes.Length);

            for (var blockY = 0; blockY < region.Height; blockY += blockSize)
            {
                var blockHeight = Math.Min(blockSize, region.Height - blockY);
                for (var blockX = 0; blockX < region.Width; blockX += blockSize)
                {
                    var blockWidth = Math.Min(blockSize, region.Width - blockX);
                    long sumB = 0, sumG = 0, sumR = 0;
                    var pixelCount = blockWidth * blockHeight;

                    // First pass: sum every channel across the block.
                    for (var y = 0; y < blockHeight; y++)
                    {
                        var rowStart = ((blockY + y) * stride) + (blockX * 4);
                        for (var x = 0; x < blockWidth; x++)
                        {
                            var i = rowStart + (x * 4);
                            sumB += bytes[i];
                            sumG += bytes[i + 1];
                            sumR += bytes[i + 2];
                        }
                    }

                    var avgB = (byte)(sumB / pixelCount);
                    var avgG = (byte)(sumG / pixelCount);
                    var avgR = (byte)(sumR / pixelCount);

                    // Second pass: write the block average back to every pixel in the block. Alpha is
                    // left untouched — the crop is always fully opaque, so it doesn't matter here.
                    for (var y = 0; y < blockHeight; y++)
                    {
                        var rowStart = ((blockY + y) * stride) + (blockX * 4);
                        for (var x = 0; x < blockWidth; x++)
                        {
                            var i = rowStart + (x * 4);
                            bytes[i] = avgB;
                            bytes[i + 1] = avgG;
                            bytes[i + 2] = avgR;
                        }
                    }
                }
            }

            Marshal.Copy(bytes, 0, data.Scan0, bytes.Length);
        }
        finally
        {
            region.UnlockBits(data);
        }

        using var g = Graphics.FromImage(_workingBitmap);
        g.DrawImage(region, area.Location);
    }

    private void BeginTextEntry(Point location)
    {
        _textEditor?.Dispose();

        // Font size mirrors the reference project's formula (base 14pt + the selected pen-size number)
        // so the same size picker doubles as a font-size control for the text tool.
        var editor = new TextBox
        {
            Location = location,
            MinimumSize = new Size(120, 24),
            Font = new Font("Microsoft YaHei UI", 14f + _annotationSize),
            ForeColor = _annotationColor,
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
        };
        editor.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                CommitTextEntry();
            }
            else if (e.KeyCode == Keys.Escape)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                CancelTextEntry();
            }
        };
        editor.LostFocus += (_, _) => CommitTextEntry();

        _textEditor = editor;
        Controls.Add(editor);
        editor.BringToFront();
        editor.Focus();
    }

    private void CommitTextEntry()
    {
        var editor = _textEditor;
        _textEditor = null;
        if (editor is null)
        {
            return;
        }

        var text = editor.Text;
        var font = editor.Font;
        var location = editor.Location;
        Controls.Remove(editor);
        editor.Dispose();

        if (!string.IsNullOrWhiteSpace(text))
        {
            PushUndoSnapshot();
            using var g = Graphics.FromImage(_workingBitmap);
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
            using var brush = new SolidBrush(_annotationColor);
            g.DrawString(text, font, brush, location);
        }

        Invalidate();
    }

    private void CancelTextEntry()
    {
        var editor = _textEditor;
        _textEditor = null;
        if (editor is null)
        {
            return;
        }

        Controls.Remove(editor);
        editor.Dispose();
    }

    private void PushUndoSnapshot()
    {
        _undoStack.Push((Bitmap)_workingBitmap.Clone());
    }

    private void Undo()
    {
        if (_undoStack.Count == 0)
        {
            return;
        }

        _workingBitmap.Dispose();
        _workingBitmap = _undoStack.Pop();
        Invalidate();
    }

    // ------------------------------------------------------------------ keyboard

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.KeyCode == Keys.Escape)
        {
            if (_textEditor is not null)
            {
                CancelTextEntry();
                return;
            }

            Finish(new ScreenshotResult(ScreenshotOutcome.Cancelled));
            return;
        }

        if (_textEditor is not null)
        {
            return;
        }

        if (e.KeyCode == Keys.Enter && _state == State.Selected)
        {
            CopyToClipboardAndFinish();
        }
        else if (e.Control && e.KeyCode == Keys.Z)
        {
            Undo();
        }
    }

    // ------------------------------------------------------------------ toolbar

    private void BuildToolbar()
    {
        var panel = CreateBarPanel();

        AddToolButton(panel, AnnotationTool.Rectangle, "▭", _texts.ToolRectangle);
        AddToolButton(panel, AnnotationTool.RectangleFilled, "■", _texts.ToolRectangleFilled);
        AddToolButton(panel, AnnotationTool.Ellipse, "◯", _texts.ToolEllipse);
        AddToolButton(panel, AnnotationTool.EllipseFilled, "●", _texts.ToolEllipseFilled);
        AddToolButton(panel, AnnotationTool.Line, "╱", _texts.ToolLine);
        AddToolButton(panel, AnnotationTool.Arrow, "↗", _texts.ToolArrow);
        AddToolButton(panel, AnnotationTool.Freehand, "✎", _texts.ToolFreehand);
        AddToolButton(panel, AnnotationTool.Text, "T", _texts.ToolText);
        AddToolButton(panel, AnnotationTool.Mosaic, "▦", _texts.ToolMosaic);

        AddSeparator(panel);

        AddActionButton(panel, "↺", _texts.ActionUndo, (_, _) => Undo());
        AddActionButton(panel, "📌", _texts.ActionPin, (_, _) => PinToScreenAndFinish());
        AddActionButton(panel, "📋", _texts.ActionCopy, (_, _) => CopyToClipboardAndFinish());
        AddActionButton(panel, "💾", _texts.ActionSave, (_, _) => SaveToFileAndFinish());
        AddActionButton(panel, "✕", _texts.ActionCancel, (_, _) => Finish(new ScreenshotResult(ScreenshotOutcome.Cancelled)));

        _toolbar = panel;
        Controls.Add(panel);
    }

    /// <summary>The secondary row of pen-size and color swatches, shown under the main toolbar only
    /// while a drawing tool is active (toggled in <see cref="AddToolButton"/>) — matches the reference
    /// project's "panel1", which likewise only appears once an annotation tool is selected.</summary>
    private void BuildOptionsPanel()
    {
        var panel = CreateBarPanel();

        foreach (var size in PenSizes)
        {
            AddSizeButton(panel, size);
        }

        AddSeparator(panel);

        foreach (var color in PresetColors)
        {
            AddColorButton(panel, color);
        }

        AddSeparator(panel);
        AddCustomColorButton(panel);

        _optionsPanel = panel;
        Controls.Add(panel);
    }

    /// <summary>Shared shell for both toolbar rows: a dark rounded-rect panel whose corner radius is
    /// reapplied on every resize (its size isn't known until WinForms lays out the buttons added to it
    /// afterwards, and AutoSize can change it again later as tools/options toggle visibility).</summary>
    private static FlowLayoutPanel CreateBarPanel()
    {
        var panel = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = ToolbarBackground,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(6, 3, 6, 3),
            Visible = false,
        };
        panel.SizeChanged += (_, _) => ApplyRoundedRegion(panel, panel.Width, panel.Height, CornerRadius);
        return panel;
    }

    private void AddToolButton(FlowLayoutPanel panel, AnnotationTool tool, string glyph, string tooltip)
    {
        var button = MakeGlyphButton(glyph);
        _toolTip.SetToolTip(button, tooltip);
        button.Click += (_, _) =>
        {
            _currentTool = _currentTool == tool ? AnnotationTool.None : tool;
            UpdateToolButtonHighlight();
            if (_optionsPanel is not null)
            {
                _optionsPanel.Visible = _currentTool != AnnotationTool.None;
                PositionToolbar();
            }
        };
        _toolButtons[tool] = button;
        panel.Controls.Add(button);
    }

    private void AddSizeButton(FlowLayoutPanel panel, int size)
    {
        var button = MakeSlotButton();

        // Owner-drawn: a filled circle whose diameter scales with the pen size it represents, so the
        // button previews the actual stroke weight instead of needing a numeric label; a ring appears
        // around it while selected, the same selection language the color swatches below use.
        button.Paint += (_, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var diameter = Math.Clamp(4 + size, 8, 22);
            var rect = new Rectangle((button.Width - diameter) / 2, (button.Height - diameter) / 2, diameter, diameter);
            using var brush = new SolidBrush(Color.White);
            e.Graphics.FillEllipse(brush, rect);

            if (size == _annotationSize)
            {
                using var ringPen = new Pen(ButtonActiveColor, 2f);
                e.Graphics.DrawEllipse(ringPen, Rectangle.Inflate(rect, 3, 3));
            }
        };

        _toolTip.SetToolTip(button, $"{size}px");
        button.Click += (_, _) =>
        {
            _annotationSize = size;
            UpdateToolButtonHighlight();
        };
        _sizeButtons.Add((size, button));
        panel.Controls.Add(button);
    }

    private void AddColorButton(FlowLayoutPanel panel, Color color)
    {
        var button = MakeSlotButton();

        // A small centered dot rather than a solid-filled square: it reads as "a color option" instead
        // of a wall of flat rectangles, and leaves room for a selection ring without resizing anything.
        button.Paint += (_, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            const int dotSize = 16;
            var rect = new Rectangle((button.Width - dotSize) / 2, (button.Height - dotSize) / 2, dotSize, dotSize);
            using var brush = new SolidBrush(color);
            e.Graphics.FillEllipse(brush, rect);

            if (color.ToArgb() == _annotationColor.ToArgb())
            {
                using var ringPen = new Pen(ButtonActiveColor, 2f);
                e.Graphics.DrawEllipse(ringPen, Rectangle.Inflate(rect, 3, 3));
            }
            else if (color.GetBrightness() > 0.85f)
            {
                // Near-white swatches would otherwise disappear against the dark toolbar background.
                using var outline = new Pen(Color.FromArgb(120, 255, 255, 255), 1f);
                e.Graphics.DrawEllipse(outline, rect);
            }
        };

        _toolTip.SetToolTip(button, color.Name);
        button.Click += (_, _) =>
        {
            _annotationColor = color;
            UpdateToolButtonHighlight();
        };
        _colorButtons.Add((color, button));
        panel.Controls.Add(button);
    }

    private void AddCustomColorButton(FlowLayoutPanel panel)
    {
        var button = MakeGlyphButton("🎨");
        _toolTip.SetToolTip(button, _texts.CustomColor);
        button.Click += (_, _) =>
        {
            using var dialog = new ColorDialog { Color = _annotationColor, FullOpen = true };
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                _annotationColor = dialog.Color;
                UpdateToolButtonHighlight();
            }
        };
        panel.Controls.Add(button);
    }

    private void AddActionButton(FlowLayoutPanel panel, string glyph, string tooltip, EventHandler onClick)
    {
        var button = MakeGlyphButton(glyph);
        _toolTip.SetToolTip(button, tooltip);
        button.Click += onClick;
        panel.Controls.Add(button);
    }

    /// <summary>A thin divider sized so its total footprint (height + margin) exactly matches every
    /// button's own footprint (<see cref="ButtonCellHeight"/>) — this is what keeps the row a single
    /// consistent height instead of being stretched by whichever control happens to be tallest.</summary>
    private static void AddSeparator(FlowLayoutPanel panel)
    {
        const int lineHeight = ButtonSize - 8;
        const int verticalMargin = (ButtonCellHeight - lineHeight) / 2;
        panel.Controls.Add(new Panel
        {
            Size = new Size(1, lineHeight),
            BackColor = Color.FromArgb(255, 80, 80, 86),
            Margin = new Padding(6, verticalMargin, 6, verticalMargin),
        });
    }

    /// <summary>A uniform-size, rounded-corner slot with no icon of its own — used for the size and
    /// color swatches, which each own-draw their content in <see cref="Control.Paint"/> instead.</summary>
    private static Button MakeSlotButton() => MakeButtonCore(text: string.Empty, fontSize: 0f);

    private static Button MakeGlyphButton(string glyph) => MakeButtonCore(glyph, fontSize: 13f);

    private static Button MakeButtonCore(string text, float fontSize)
    {
        var button = new Button
        {
            Text = text,
            Size = new Size(ButtonSize, ButtonSize),
            FlatStyle = FlatStyle.Flat,
            BackColor = ButtonIdleColor,
            ForeColor = Color.White,
            Margin = new Padding(3),
            TextAlign = ContentAlignment.MiddleCenter,
            FlatAppearance = { BorderSize = 0 },
        };

        if (fontSize > 0f)
        {
            button.Font = new Font("Segoe UI Symbol", fontSize);
        }

        ApplyRoundedRegion(button, ButtonSize, ButtonSize, CornerRadius);

        // Hover feedback: Tag always holds "the color this button should idle at" (set here and
        // whenever UpdateToolButtonHighlight changes a button's state), so leaving the button restores
        // the correct idle/selected color rather than a hardcoded default.
        button.Tag = ButtonIdleColor;
        button.MouseEnter += (_, _) => button.BackColor = ButtonHoverColor;
        button.MouseLeave += (_, _) => button.BackColor = (Color)button.Tag!;

        return button;
    }

    private static void ApplyRoundedRegion(Control control, int width, int height, int radius)
    {
        if (width <= 0 || height <= 0)
        {
            return;
        }

        using var path = new GraphicsPath();
        var d = radius * 2;
        var bounds = new Rectangle(0, 0, width, height);
        path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
        path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
        path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
        path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        control.Region = new Region(path);
    }

    private void UpdateToolButtonHighlight()
    {
        foreach (var (tool, button) in _toolButtons)
        {
            var color = tool == _currentTool ? ButtonActiveColor : ButtonIdleColor;
            button.BackColor = color;
            button.Tag = color;
        }

        // Size/color swatches draw their own selection ring in Paint rather than changing BackColor,
        // so all they need here is a repaint to pick up the new _annotationColor/_annotationSize.
        foreach (var (_, button) in _colorButtons)
        {
            button.Invalidate();
        }

        foreach (var (_, button) in _sizeButtons)
        {
            button.Invalidate();
        }
    }

    private void ShowToolbar()
    {
        if (_toolbar is null)
        {
            return;
        }

        _toolbar.Visible = true;
        _currentTool = AnnotationTool.None;
        UpdateToolButtonHighlight();
        PositionToolbar();
    }

    private void PositionToolbar()
    {
        if (_toolbar is null || !_toolbar.Visible)
        {
            return;
        }

        var x = Math.Clamp(_selection.Left, 0, Math.Max(0, Width - _toolbar.Width));
        var optionsVisible = _optionsPanel is { Visible: true };
        var totalHeight = _toolbar.Height + (optionsVisible ? _optionsPanel!.Height + OptionsGap : 0);

        var y = _selection.Bottom + ToolbarGap;
        if (y + totalHeight > Height)
        {
            // Not enough room below the selection — flip the whole toolbar+options block above it instead.
            y = _selection.Top - ToolbarGap - totalHeight;
        }

        if (y < 0)
        {
            y = 0;
        }

        _toolbar.Location = new Point(x, y);
        _toolbar.BringToFront();

        if (optionsVisible)
        {
            var optionsX = Math.Clamp(x, 0, Math.Max(0, Width - _optionsPanel!.Width));
            _optionsPanel.Location = new Point(optionsX, y + _toolbar.Height + OptionsGap);
            _optionsPanel.BringToFront();
        }
    }

    // ------------------------------------------------------------------ finishing

    private void CopyToClipboardAndFinish()
    {
        using var cropped = CropSelection();
        if (cropped is null)
        {
            return;
        }

        if (ClipboardInterop.SetImage(cropped))
        {
            Finish(new ScreenshotResult(ScreenshotOutcome.CopiedToClipboard));
        }
    }

    private void SaveToFileAndFinish()
    {
        using var cropped = CropSelection();
        if (cropped is null)
        {
            return;
        }

        using var dialog = new SaveFileDialog
        {
            Filter = _texts.SaveDialogFilter,
            FileName = $"screenshot-{DateTime.Now:yyyyMMdd-HHmmss}.png",
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            cropped.Save(dialog.FileName, ImageFormat.Png);
            Finish(new ScreenshotResult(ScreenshotOutcome.SavedToFile, dialog.FileName));
        }
    }

    private void PinToScreenAndFinish()
    {
        var cropped = CropSelection();
        if (cropped is null)
        {
            return;
        }

        // PinnedScreenshotWindow takes ownership of the bitmap (disposes it when the window closes) —
        // deliberately not wrapped in `using` here, since that would dispose it out from under the window.
        var pinned = new PinnedScreenshotWindow(cropped);
        pinned.Show();
        Finish(new ScreenshotResult(ScreenshotOutcome.PinnedToScreen));
    }

    private Bitmap? CropSelection()
    {
        if (_selection.Width <= 0 || _selection.Height <= 0)
        {
            return null;
        }

        var area = Rectangle.Intersect(_selection, new Rectangle(Point.Empty, _workingBitmap.Size));
        if (area.Width <= 0 || area.Height <= 0)
        {
            return null;
        }

        return _workingBitmap.Clone(area, _workingBitmap.PixelFormat);
    }

    private void Finish(ScreenshotResult result)
    {
        Completed?.Invoke(this, result);
        Close();
    }
}
