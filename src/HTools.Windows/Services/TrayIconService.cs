using System.Drawing;
using System.Windows.Forms;
using HTools.Windows.Interop;

namespace HTools.Windows.Services;

/// <summary>System tray icon shown while the app is minimized, with a context menu to restore or exit.</summary>
public sealed class TrayIconService : IDisposable
{
    private readonly NativeMessageLoop _loop;
    private NotifyIcon? _notifyIcon;
    private ToolStripMenuItem? _showItem;
    private ToolStripMenuItem? _exitItem;

    public TrayIconService(NativeMessageLoop loop)
    {
        _loop = loop;
    }

    public event EventHandler? ShowRequested;

    public event EventHandler? ExitRequested;

    public void Initialize(string tooltipText, string showLabel, string exitLabel)
    {
        _loop.Invoke(() =>
        {
            var menu = new ContextMenuStrip();
            _showItem = new ToolStripMenuItem(showLabel);
            _showItem.Click += (_, _) => ShowRequested?.Invoke(this, EventArgs.Empty);
            _exitItem = new ToolStripMenuItem(exitLabel);
            _exitItem.Click += (_, _) => ExitRequested?.Invoke(this, EventArgs.Empty);

            menu.Items.Add(_showItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(_exitItem);

            _notifyIcon = new NotifyIcon
            {
                Icon = TryGetAppIcon(),
                Text = Truncate(tooltipText, 63),
                Visible = true,
                ContextMenuStrip = menu,
            };
            _notifyIcon.DoubleClick += (_, _) => ShowRequested?.Invoke(this, EventArgs.Empty);
        });
    }

    public void UpdateLabels(string showLabel, string exitLabel)
    {
        _loop.Invoke(() =>
        {
            if (_showItem is not null)
            {
                _showItem.Text = showLabel;
            }

            if (_exitItem is not null)
            {
                _exitItem.Text = exitLabel;
            }
        });
    }

    public void Dispose()
    {
        _loop.Invoke(() =>
        {
            if (_notifyIcon is not null)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
                _notifyIcon = null;
            }
        });
    }

    private static Icon TryGetAppIcon()
    {
        try
        {
            var path = Environment.ProcessPath;
            if (path is not null)
            {
                var extracted = Icon.ExtractAssociatedIcon(path);
                if (extracted is not null)
                {
                    return extracted;
                }
            }
        }
        catch (Exception)
        {
            // Fall through to the system default below.
        }

        return SystemIcons.Application;
    }

    private static string Truncate(string text, int maxLength)
    {
        return text.Length <= maxLength ? text : text[..maxLength];
    }
}
