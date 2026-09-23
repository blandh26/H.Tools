using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;
using HTools.Core.Models;
using HTools.Windows.Interop;

namespace HTools.Windows.Effects;

/// <summary>
/// Layered, click-through overlay window that paints a fading trail of ghost silhouettes following
/// the cursor, triggered by <see cref="MouseShakeDetector"/>. Ported from a reference arrow-cursor
/// "mouse finder" overlay. Supports swapping the ghost shape between a plum blossom (radially
/// symmetric, so each ghost centers directly on its trail point) and the original cursor arrow
/// (asymmetric, so it needs the original hotspot offset).
/// </summary>
public sealed class MouseTrailOverlayForm : Form
{
    private const int MaxGhosts = 50;
    private const int OverlaySize = 900;

    private const float BlossomPathSize = 100f;
    private const float BlossomRenderSize = 46f;

    private const float ArrowPathWidth = 120f;
    private const float ArrowPathHeight = 165f;
    private const float ArrowRenderWidth = 58f;
    private const float ArrowRenderHeight = 82f;

    private readonly GraphicsPath _blossomPath = CreateBlossomPath();
    private readonly GraphicsPath _arrowPath = CreateArrowPath();
    private readonly SolidBrush _petalBrush = new(Color.FromArgb(255, EffectColor.PlumPink.Color));
    private readonly Bitmap _frameBitmap = new(OverlaySize, OverlaySize, PixelFormat.Format32bppPArgb);
    private readonly Graphics _frameGraphics;
    private readonly System.Windows.Forms.Timer _hideTimer;
    private readonly float[] _ghostX = new float[MaxGhosts];
    private readonly float[] _ghostY = new float[MaxGhosts];
    private readonly float[] _ghostOpacity = new float[MaxGhosts];
    private readonly bool[] _ghostVisible = new bool[MaxGhosts];

    private bool _isTracking;
    private int _visibleGhostLimit = 20;
    private Color _ghostColor = EffectColor.PlumPink.Color;
    private MouseGhostShape _shape = MouseGhostShape.PlumBlossom;

    public MouseTrailOverlayForm()
    {
        _frameGraphics = Graphics.FromImage(_frameBitmap);

        AutoScaleMode = AutoScaleMode.None;
        BackColor = Color.Black;
        ClientSize = new Size(OverlaySize, OverlaySize);
        ControlBox = false;
        FormBorderStyle = FormBorderStyle.None;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        TopMost = true;

        _hideTimer = new System.Windows.Forms.Timer { Interval = 900 };
        _hideTimer.Tick += (_, _) =>
        {
            _hideTimer.Stop();
            _isTracking = false;
            CollapseGhosts();
            Hide();
            ProcessMemory.TrimWorkingSet();
        };
    }

    public bool IsTracking => _isTracking;

    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ExStyle |= NativeMethods.WS_EX_TRANSPARENT
                | NativeMethods.WS_EX_TOOLWINDOW
                | NativeMethods.WS_EX_LAYERED
                | NativeMethods.WS_EX_NOACTIVATE;
            return cp;
        }
    }

    private GraphicsPath CurrentPath => _shape == MouseGhostShape.Arrow ? _arrowPath : _blossomPath;

    private float CurrentPathWidth => _shape == MouseGhostShape.Arrow ? ArrowPathWidth : BlossomPathSize;

    private float CurrentPathHeight => _shape == MouseGhostShape.Arrow ? ArrowPathHeight : BlossomPathSize;

    private float CurrentRenderWidth => _shape == MouseGhostShape.Arrow ? ArrowRenderWidth : BlossomRenderSize;

    private float CurrentRenderHeight => _shape == MouseGhostShape.Arrow ? ArrowRenderHeight : BlossomRenderSize;

    // The arrow's tip/hotspot isn't at its shape's center, so ghosts need an offset to land on the
    // cursor point; the radially symmetric blossom needs none.
    private float CurrentOffsetX => _shape == MouseGhostShape.Arrow ? ArrowRenderWidth * 0.18f : 0f;

    private float CurrentOffsetY => _shape == MouseGhostShape.Arrow ? ArrowRenderHeight * 0.12f : 0f;

    public void SetShape(MouseGhostShape shape)
    {
        _shape = shape;
        if (!_isTracking)
        {
            CollapseGhosts();
        }
    }

    public void SetVisibleGhostLimit(int count)
    {
        _visibleGhostLimit = Math.Clamp(count, 1, MaxGhosts);
        if (!_isTracking)
        {
            CollapseGhosts();
        }
    }

    public void SetEffectDuration(int milliseconds)
    {
        _hideTimer.Interval = Math.Clamp(milliseconds, 300, 3000);
    }

    public void SetGhostColor(Color color)
    {
        _ghostColor = color;
    }

    public void Pulse(ScreenPoint point, TrailSample[] trailPoints, int trailCount)
    {
        if (!Visible)
        {
            Show();
        }

        _isTracking = true;
        Follow(point, trailPoints, trailCount);
    }

    public void Follow(ScreenPoint point, TrailSample[] trailPoints, int trailCount)
    {
        if (!_isTracking)
        {
            return;
        }

        MoveTopMost(point);
        RenderGhostTrail(point, trailPoints, trailCount);

        _hideTimer.Stop();
        _hideTimer.Start();
    }

    public void StopTracking()
    {
        _hideTimer.Stop();
        _isTracking = false;
        CollapseGhosts();
        Hide();
        ProcessMemory.TrimWorkingSet();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _hideTimer.Dispose();
            _blossomPath.Dispose();
            _arrowPath.Dispose();
            _petalBrush.Dispose();
            _frameGraphics.Dispose();
            _frameBitmap.Dispose();
        }

        base.Dispose(disposing);
    }

    private void RenderLayeredFrame(ScreenPoint centerPoint)
    {
        var graphics = _frameGraphics;
        graphics.CompositingMode = CompositingMode.SourceCopy;
        graphics.Clear(Color.Transparent);
        graphics.CompositingMode = CompositingMode.SourceOver;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.CompositingQuality = CompositingQuality.HighSpeed;
        graphics.InterpolationMode = InterpolationMode.Low;
        graphics.PixelOffsetMode = PixelOffsetMode.HighSpeed;

        var path = CurrentPath;
        var scaleX = CurrentRenderWidth / CurrentPathWidth;
        var scaleY = CurrentRenderHeight / CurrentPathHeight;

        for (var index = 0; index < MaxGhosts; index++)
        {
            if (!_ghostVisible[index])
            {
                continue;
            }

            var alpha = (int)Math.Clamp(_ghostOpacity[index] * 255f, 0f, 255f);
            _petalBrush.Color = Color.FromArgb(alpha, _ghostColor);

            graphics.ResetTransform();
            graphics.TranslateTransform(_ghostX[index], _ghostY[index]);
            graphics.ScaleTransform(scaleX, scaleY);
            graphics.FillPath(_petalBrush, path);
        }

        graphics.ResetTransform();
        UpdateLayeredFrame(centerPoint);
    }

    private void MoveTopMost(ScreenPoint point)
    {
        NativeMethods.SetWindowPos(
            Handle,
            NativeMethods.HWND_TOPMOST,
            point.X - Width / 2,
            point.Y - Height / 2,
            Width,
            Height,
            NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_SHOWWINDOW);
    }

    private void RenderGhostTrail(ScreenPoint currentScreenPoint, TrailSample[] trailPoints, int trailCount)
    {
        CollapseGhosts();

        var activeGhosts = Math.Clamp(_visibleGhostLimit, 1, MaxGhosts);
        var boundedTrailCount = Math.Clamp(trailCount, 0, trailPoints.Length);
        var includeCurrentPoint = boundedTrailCount == 0 || trailPoints[boundedTrailCount - 1].Point != currentScreenPoint;
        var sourceCount = Math.Min(boundedTrailCount, includeCurrentPoint ? activeGhosts - 1 : activeGhosts);
        var outputCount = Math.Max(1, sourceCount + (includeCurrentPoint ? 1 : 0));
        var sourceStart = Math.Max(0, boundedTrailCount - sourceCount);
        var previous = default(ScreenPoint);
        var hasPrevious = false;
        var rendered = 0;

        for (var index = sourceStart; index < boundedTrailCount && rendered < activeGhosts; index++)
        {
            var sample = trailPoints[index];
            var screenPoint = sample.Point;
            if (hasPrevious && screenPoint == previous)
            {
                continue;
            }

            previous = screenPoint;
            hasPrevious = true;
            rendered = RenderGhostAt(rendered, outputCount, sample, currentScreenPoint);
        }

        if (includeCurrentPoint && (!hasPrevious || previous != currentScreenPoint) && rendered < activeGhosts)
        {
            RenderGhostAt(rendered, outputCount, new TrailSample(currentScreenPoint, 1f), currentScreenPoint);
        }

        RenderLayeredFrame(currentScreenPoint);
    }

    private int RenderGhostAt(int renderIndex, int outputCount, TrailSample sample, ScreenPoint currentScreenPoint)
    {
        var screenPoint = sample.Point;
        var x = screenPoint.X - currentScreenPoint.X + Width / 2f;
        var y = screenPoint.Y - currentScreenPoint.Y + Height / 2f;

        if (x < 4 || y < 4 || x > Width - 12 || y > Height - 12)
        {
            return renderIndex;
        }

        var newestWeight = MathF.Pow((renderIndex + 1f) / outputCount, 1.35f);
        _ghostX[renderIndex] = x - CurrentOffsetX;
        _ghostY[renderIndex] = y - CurrentOffsetY;
        _ghostOpacity[renderIndex] = (0.08f + newestWeight * 0.45f) * sample.Life;
        _ghostVisible[renderIndex] = true;

        return renderIndex + 1;
    }

    private void CollapseGhosts()
    {
        Array.Clear(_ghostVisible);
        Array.Clear(_ghostOpacity);
    }

    private void UpdateLayeredFrame(ScreenPoint centerPoint)
    {
        var screenDc = NativeMethods.GetDC(0);
        var memoryDc = NativeMethods.CreateCompatibleDC(screenDc);
        var bitmapHandle = _frameBitmap.GetHbitmap(Color.FromArgb(0));
        var oldBitmap = NativeMethods.SelectObject(memoryDc, bitmapHandle);

        try
        {
            var destination = new NativeMethods.POINT
            {
                X = centerPoint.X - Width / 2,
                Y = centerPoint.Y - Height / 2,
            };
            var size = new NativeMethods.SIZE { Cx = Width, Cy = Height };
            var source = new NativeMethods.POINT();
            var blend = new NativeMethods.BLENDFUNCTION
            {
                BlendOp = NativeMethods.AC_SRC_OVER,
                BlendFlags = 0,
                SourceConstantAlpha = 255,
                AlphaFormat = NativeMethods.AC_SRC_ALPHA,
            };

            NativeMethods.UpdateLayeredWindow(
                Handle,
                screenDc,
                ref destination,
                ref size,
                memoryDc,
                ref source,
                0,
                ref blend,
                NativeMethods.ULW_ALPHA);
        }
        finally
        {
            NativeMethods.SelectObject(memoryDc, oldBitmap);
            NativeMethods.DeleteObject(bitmapHandle);
            NativeMethods.DeleteDC(memoryDc);
            NativeMethods.ReleaseDC(0, screenDc);
        }
    }

    /// <summary>Five petals (a teardrop bezier rotated 72 degrees at a time around the origin) plus a
    /// center dot, in a local coordinate space roughly -40..40 on both axes.</summary>
    private static GraphicsPath CreateBlossomPath()
    {
        using var petal = new GraphicsPath(FillMode.Winding);
        petal.AddBezier(new PointF(0, 0), new PointF(-14, -10), new PointF(-20, -30), new PointF(0, -42));
        petal.AddBezier(new PointF(0, -42), new PointF(20, -30), new PointF(14, -10), new PointF(0, 0));
        petal.CloseFigure();

        var blossom = new GraphicsPath(FillMode.Winding);
        for (var i = 0; i < 5; i++)
        {
            using var rotated = (GraphicsPath)petal.Clone();
            using var matrix = new Matrix();
            matrix.RotateAt(i * 72f, PointF.Empty);
            rotated.Transform(matrix);
            blossom.AddPath(rotated, false);
        }

        blossom.AddEllipse(-8f, -8f, 16f, 16f);
        return blossom;
    }

    /// <summary>The original reference app's cursor-arrow silhouette.</summary>
    private static GraphicsPath CreateArrowPath()
    {
        var path = new GraphicsPath(FillMode.Winding);
        path.AddPolygon(
        [
            new PointF(0, 0),
            new PointF(0, 146),
            new PointF(38, 110),
            new PointF(62, 165),
            new PointF(90, 153),
            new PointF(66, 100),
            new PointF(120, 100),
        ]);
        return path;
    }
}
