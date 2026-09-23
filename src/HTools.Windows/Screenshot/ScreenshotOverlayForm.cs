using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;
using HTools.Windows.Interop;

namespace HTools.Windows.Screenshot;

/// <summary>
/// A borderless, topmost window sized to the whole virtual desktop (so selection can cross monitor
/// boundaries), showing a frozen screenshot as its background. Drag out a region, optionally resize it
/// via the corner/edge handles, then annotate (rectangle/ellipse/arrow/freehand/text/mosaic) before
/// copying or saving. Annotations are "burned in" to a working bitmap on each commit, with a plain
/// undo stack of bitmap snapshots — simpler than tracking a vector object model and works uniformly
/// for pixel-level tools like mosaic.
/// </summary>
public sealed class ScreenshotOverlayForm : Form
{
    private const int HandleSize = 8;
    private const int ToolbarHeight = 40;
    private const int MinSelectionSize = 4;

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

    private AnnotationTool _currentTool = AnnotationTool.None;
    private Color _annotationColor = Color.FromArgb(255, 235, 64, 52);
    private bool _isAnnotating;
    private Point _annotationStart;
    private Point _annotationCurrent;
    private readonly List<Point> _freehandPoints = [];

    private FlowLayoutPanel? _toolbar;
    private TextBox? _textEditor;
    private readonly Dictionary<AnnotationTool, Button> _toolButtons = new();
    private readonly Dictionary<Color, Button> _colorButtons = new();
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
                DrawDim(g, Rectangle.Empty);
                DrawHint(g);
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

    private void DrawLivePreview(Graphics g)
    {
        if (!_isAnnotating)
        {
            return;
        }

        using var pen = new Pen(_annotationColor, 3f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        switch (_currentTool)
        {
            case AnnotationTool.Rectangle:
                g.DrawRectangle(pen, NormalizedRect(_annotationStart, _annotationCurrent));
                break;
            case AnnotationTool.Ellipse:
                g.DrawEllipse(pen, NormalizedRect(_annotationStart, _annotationCurrent));
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
            (float)(end.X - headLength * Math.Cos(angle - headAngle)),
            (float)(end.Y - headLength * Math.Sin(angle - headAngle)));
        var p2 = new PointF(
            (float)(end.X - headLength * Math.Cos(angle + headAngle)),
            (float)(end.Y - headLength * Math.Sin(angle + headAngle)));

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
        using var pen = new Pen(_annotationColor, 3f) { StartCap = LineCap.Round, EndCap = LineCap.Round };

        switch (_currentTool)
        {
            case AnnotationTool.Rectangle:
                g.DrawRectangle(pen, NormalizedRect(_annotationStart, _annotationCurrent));
                break;
            case AnnotationTool.Ellipse:
                g.DrawEllipse(pen, NormalizedRect(_annotationStart, _annotationCurrent));
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

    private void ApplyMosaic(Rectangle area)
    {
        area = Rectangle.Intersect(area, new Rectangle(Point.Empty, _workingBitmap.Size));
        if (area.Width < 2 || area.Height < 2)
        {
            return;
        }

        const int blockSize = 14;
        var smallWidth = Math.Max(1, area.Width / blockSize);
        var smallHeight = Math.Max(1, area.Height / blockSize);

        using var region = _workingBitmap.Clone(area, _workingBitmap.PixelFormat);
        using var small = new Bitmap(smallWidth, smallHeight);
        using (var smallGraphics = Graphics.FromImage(small))
        {
            smallGraphics.InterpolationMode = InterpolationMode.Bilinear;
            smallGraphics.DrawImage(region, new Rectangle(0, 0, smallWidth, smallHeight));
        }

        using var g = Graphics.FromImage(_workingBitmap);
        g.InterpolationMode = InterpolationMode.NearestNeighbor;
        g.PixelOffsetMode = PixelOffsetMode.Half;
        g.DrawImage(small, area);
    }

    private void BeginTextEntry(Point location)
    {
        _textEditor?.Dispose();

        var editor = new TextBox
        {
            Location = location,
            MinimumSize = new Size(120, 24),
            Font = new Font("Microsoft YaHei UI", 12f),
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
        var location = editor.Location;
        Controls.Remove(editor);
        editor.Dispose();

        if (!string.IsNullOrWhiteSpace(text))
        {
            PushUndoSnapshot();
            using var g = Graphics.FromImage(_workingBitmap);
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
            using var font = new Font("Microsoft YaHei UI", 14f, FontStyle.Bold);
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
        var panel = new FlowLayoutPanel
        {
            AutoSize = true,
            BackColor = Color.FromArgb(240, 32, 32, 32),
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(4),
            Visible = false,
        };

        AddToolButton(panel, AnnotationTool.Rectangle, "▭", _texts.ToolRectangle);
        AddToolButton(panel, AnnotationTool.Ellipse, "◯", _texts.ToolEllipse);
        AddToolButton(panel, AnnotationTool.Arrow, "↗", _texts.ToolArrow);
        AddToolButton(panel, AnnotationTool.Freehand, "✎", _texts.ToolFreehand);
        AddToolButton(panel, AnnotationTool.Text, "T", _texts.ToolText);
        AddToolButton(panel, AnnotationTool.Mosaic, "▦", _texts.ToolMosaic);

        AddSeparator(panel);

        foreach (var color in new[] { Color.FromArgb(255, 235, 64, 52), Color.FromArgb(255, 250, 173, 20), Color.FromArgb(255, 82, 196, 26), Color.FromArgb(255, 24, 144, 255), Color.Black })
        {
            AddColorButton(panel, color);
        }

        AddSeparator(panel);

        AddActionButton(panel, "↺", _texts.ActionUndo, (_, _) => Undo());
        AddActionButton(panel, "📋", _texts.ActionCopy, (_, _) => CopyToClipboardAndFinish());
        AddActionButton(panel, "💾", _texts.ActionSave, (_, _) => SaveToFileAndFinish());
        AddActionButton(panel, "✕", _texts.ActionCancel, (_, _) => Finish(new ScreenshotResult(ScreenshotOutcome.Cancelled)));

        _toolbar = panel;
        Controls.Add(panel);
    }

    private void AddToolButton(FlowLayoutPanel panel, AnnotationTool tool, string glyph, string tooltip)
    {
        var button = MakeButton(glyph);
        _toolTip.SetToolTip(button, tooltip);
        button.Click += (_, _) =>
        {
            _currentTool = _currentTool == tool ? AnnotationTool.None : tool;
            UpdateToolButtonHighlight();
        };
        _toolButtons[tool] = button;
        panel.Controls.Add(button);
    }

    private void AddColorButton(FlowLayoutPanel panel, Color color)
    {
        var button = new Button
        {
            Size = new Size(20, 32),
            BackColor = color,
            FlatStyle = FlatStyle.Flat,
            Margin = new Padding(2, 4, 2, 4),
        };
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.BorderColor = Color.White;
        button.Click += (_, _) =>
        {
            _annotationColor = color;
            UpdateToolButtonHighlight();
        };
        _colorButtons[color] = button;
        panel.Controls.Add(button);
    }

    private void AddActionButton(FlowLayoutPanel panel, string glyph, string tooltip, EventHandler onClick)
    {
        var button = MakeButton(glyph);
        _toolTip.SetToolTip(button, tooltip);
        button.Click += onClick;
        panel.Controls.Add(button);
    }

    private static void AddSeparator(FlowLayoutPanel panel)
    {
        panel.Controls.Add(new Panel { Size = new Size(1, 26), BackColor = Color.FromArgb(255, 90, 90, 90), Margin = new Padding(4, 6, 4, 6) });
    }

    private static Button MakeButton(string glyph)
    {
        return new Button
        {
            Text = glyph,
            Size = new Size(32, 32),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(240, 32, 32, 32),
            ForeColor = Color.White,
            Font = new Font("Segoe UI Symbol", 12f),
            Margin = new Padding(2),
            FlatAppearance = { BorderSize = 0 },
        };
    }

    private void UpdateToolButtonHighlight()
    {
        foreach (var (tool, button) in _toolButtons)
        {
            button.BackColor = tool == _currentTool ? Color.FromArgb(255, 24, 144, 255) : Color.FromArgb(240, 32, 32, 32);
        }

        foreach (var (color, button) in _colorButtons)
        {
            button.FlatAppearance.BorderColor = color == _annotationColor ? Color.FromArgb(255, 24, 144, 255) : Color.White;
            button.FlatAppearance.BorderSize = color == _annotationColor ? 2 : 1;
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
        var y = _selection.Bottom + 8;
        if (y + ToolbarHeight > Height)
        {
            y = _selection.Top - ToolbarHeight - 8;
        }

        if (y < 0)
        {
            y = 0;
        }

        _toolbar.Location = new Point(x, y);
        _toolbar.BringToFront();
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
