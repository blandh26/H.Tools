using HTools.Windows.Interop;
using HTools.Windows.Screenshot;

namespace HTools.Windows.Services;

/// <summary>Captures the whole virtual desktop (all monitors) and shows the interactive selection/
/// annotation overlay on the shared native loop thread, matching the pattern used by the mouse-trail
/// and tray-icon features.</summary>
public sealed class ScreenshotService
{
    private readonly NativeMessageLoop _loop;
    private ScreenshotOverlayForm? _activeOverlay;

    public ScreenshotService(NativeMessageLoop loop)
    {
        _loop = loop;
    }

    public void Capture(Action<ScreenshotResult> onCompleted, ScreenshotOverlayTexts? texts = null)
    {
        _loop.Invoke(() =>
        {
            if (_activeOverlay is not null)
            {
                return;
            }

            var bounds = ScreenCapture.GetVirtualScreenBounds();
            var screenshot = ScreenCapture.CaptureVirtualScreen();
            var overlay = new ScreenshotOverlayForm(screenshot, bounds, texts);
            _activeOverlay = overlay;

            overlay.Completed += (_, result) => onCompleted(result);
            overlay.FormClosed += (_, _) =>
            {
                _activeOverlay = null;
                overlay.Dispose();
            };

            overlay.Show();
        });
    }
}
