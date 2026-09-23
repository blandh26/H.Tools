using System.Drawing;
using System.Windows.Forms;
using HTools.Core.Models;
using HTools.Windows.Effects;
using HTools.Windows.Interop;

namespace HTools.Windows.Services;

/// <summary>
/// Owns the mouse-trail effect: polls the cursor position on a timer (on the shared native loop
/// thread), feeds it through <see cref="MouseShakeDetector"/>, and drives the
/// <see cref="MouseTrailOverlayForm"/> accordingly. Disabled by default; the settings page turns it on.
/// </summary>
public sealed class MouseEffectService : IDisposable
{
    private const int MaxTrailPoints = 50;
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(8);

    private readonly NativeMessageLoop _loop;
    private readonly TrailSample[] _trailBuffer = new TrailSample[MaxTrailPoints];

    private MouseShakeDetector? _shakeDetector;
    private MouseTrailOverlayForm? _overlay;
    private System.Windows.Forms.Timer? _timer;
    private DateTime _lastVisualFollowTime = DateTime.MinValue;

    public MouseEffectService(NativeMessageLoop loop)
    {
        _loop = loop;
    }

    public bool IsEnabled { get; private set; }

    public void SetEnabled(bool enabled)
    {
        IsEnabled = enabled;
        _loop.Invoke(() =>
        {
            EnsureCreated();
            if (enabled)
            {
                _timer!.Start();
            }
            else
            {
                _timer!.Stop();
                _overlay!.StopTracking();
            }
        });
    }

    public void SetGhostCount(int count) => _loop.Invoke(() =>
    {
        EnsureCreated();
        _overlay!.SetVisibleGhostLimit(count);
    });

    public void SetEffectDuration(int milliseconds) => _loop.Invoke(() =>
    {
        EnsureCreated();
        _overlay!.SetEffectDuration(milliseconds);
        _shakeDetector!.TrailWindow = TimeSpan.FromMilliseconds(milliseconds);
    });

    public void SetColor(Color color) => _loop.Invoke(() =>
    {
        EnsureCreated();
        _overlay!.SetGhostColor(color);
    });

    public void SetShape(MouseGhostShape shape) => _loop.Invoke(() =>
    {
        EnsureCreated();
        _overlay!.SetShape(shape);
    });

    public void TestPulse() => _loop.Invoke(() =>
    {
        EnsureCreated();
        if (!NativeMethods.GetCursorPos(out var point))
        {
            return;
        }

        var screenPoint = new ScreenPoint(point.X, point.Y);
        _trailBuffer[0] = new TrailSample(screenPoint, 1f);
        _overlay!.Pulse(screenPoint, _trailBuffer, 1);
    });

    public void Dispose() => _loop.Invoke(() =>
    {
        _timer?.Stop();
        _timer?.Dispose();
        _overlay?.Dispose();
    });

    private void EnsureCreated()
    {
        if (_overlay is not null)
        {
            return;
        }

        _shakeDetector = new MouseShakeDetector();
        _overlay = new MouseTrailOverlayForm();
        _timer = new System.Windows.Forms.Timer { Interval = (int)PollInterval.TotalMilliseconds };
        _timer.Tick += (_, _) => ProcessLatestMousePoint();
    }

    private void ProcessLatestMousePoint()
    {
        if (!NativeMethods.GetCursorPos(out var cursor))
        {
            return;
        }

        var point = new ScreenPoint(cursor.X, cursor.Y);

        if (_shakeDetector!.RegisterMove(point))
        {
            _lastVisualFollowTime = DateTime.UtcNow;
            var trailCount = _shakeDetector.CopyRecentSamples(_trailBuffer);
            _overlay!.Pulse(point, _trailBuffer, trailCount);
            return;
        }

        var now = DateTime.UtcNow;
        if (_overlay!.IsTracking && now - _lastVisualFollowTime >= PollInterval)
        {
            _lastVisualFollowTime = now;
            var trailCount = _shakeDetector.CopyRecentSamples(_trailBuffer);
            if (trailCount > 0)
            {
                _overlay.Follow(point, _trailBuffer, trailCount);
            }
            else
            {
                _overlay.StopTracking();
            }
        }
    }
}
