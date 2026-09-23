using System.Threading;
using System.Windows.Forms;

namespace HTools.Windows.Interop;

/// <summary>
/// Owns a dedicated STA thread running a WinForms message pump, so global hotkeys, clipboard
/// change notifications and (later) the mouse-hook overlay all share one hidden window/thread
/// instead of each needing their own.
/// </summary>
public sealed class NativeMessageLoop : IDisposable
{
    private readonly ManualResetEventSlim _ready = new(false);
    private Thread? _thread;
    private MessageWindow? _window;

    public event EventHandler<int>? HotkeyPressed;

    public event EventHandler? ClipboardChanged;

    public void Start()
    {
        if (_thread is not null)
        {
            return;
        }

        _thread = new Thread(RunLoop)
        {
            IsBackground = true,
            Name = "HTools.NativeMessageLoop",
        };
        _thread.SetApartmentState(ApartmentState.STA);
        _thread.Start();
        _ready.Wait();
    }

    public bool RegisterHotkey(int id, uint modifiers, uint virtualKey)
    {
        return RunOnLoopThread(() => NativeMethods.RegisterHotKey(_window!.Handle, id, modifiers, virtualKey));
    }

    public bool UnregisterHotkey(int id)
    {
        return RunOnLoopThread(() => NativeMethods.UnregisterHotKey(_window!.Handle, id));
    }

    /// <summary>Runs an action on the loop thread, used by anything (tray icon, mouse overlay) that
    /// needs to create or touch WinForms objects, which must live on the thread that pumps their messages.</summary>
    public void Invoke(Action action)
    {
        if (_window is null)
        {
            return;
        }

        if (_window.InvokeRequired)
        {
            _window.Invoke(action);
        }
        else
        {
            action();
        }
    }

    public T Invoke<T>(Func<T> func)
    {
        if (_window is null)
        {
            return default!;
        }

        return _window.InvokeRequired ? _window.Invoke(func) : func();
    }

    public void Dispose()
    {
        if (_window is { IsDisposed: false } window)
        {
            try
            {
                window.Invoke(() =>
                {
                    NativeMethods.RemoveClipboardFormatListener(window.Handle);
                    Application.ExitThread();
                });
            }
            catch (InvalidOperationException)
            {
                // Thread/handle already gone.
            }
        }

        _thread?.Join(TimeSpan.FromSeconds(2));
        _ready.Dispose();
    }

    private bool RunOnLoopThread(Func<bool> action)
    {
        if (_window is null)
        {
            return false;
        }

        if (_window.InvokeRequired)
        {
            return (bool)_window.Invoke(action);
        }

        return action();
    }

    private void RunLoop()
    {
        _window = new MessageWindow();
        _window.HotkeyPressed += id => HotkeyPressed?.Invoke(this, id);
        _window.ClipboardChanged += () => ClipboardChanged?.Invoke(this, EventArgs.Empty);

        var handle = _window.Handle; // force HWND creation without showing the window
        NativeMethods.AddClipboardFormatListener(handle);

        _ready.Set();
        Application.Run();
    }
}
