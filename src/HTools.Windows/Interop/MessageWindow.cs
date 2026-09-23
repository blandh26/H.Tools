using System.Windows.Forms;

namespace HTools.Windows.Interop;

/// <summary>
/// A never-shown WinForms window used purely to receive WM_HOTKEY / WM_CLIPBOARDUPDATE messages.
/// Accessing <see cref="Control.Handle"/> creates the underlying HWND without ever calling Show().
/// </summary>
internal sealed class MessageWindow : Form
{
    public event Action<int>? HotkeyPressed;

    public event Action? ClipboardChanged;

    public MessageWindow()
    {
        ShowInTaskbar = false;
        FormBorderStyle = FormBorderStyle.None;
        Opacity = 0;
        Width = 0;
        Height = 0;
        StartPosition = FormStartPosition.Manual;
        Location = new System.Drawing.Point(-4000, -4000);
    }

    protected override void WndProc(ref Message m)
    {
        switch (m.Msg)
        {
            case NativeMethods.WM_HOTKEY:
                HotkeyPressed?.Invoke(m.WParam.ToInt32());
                break;
            case NativeMethods.WM_CLIPBOARDUPDATE:
                ClipboardChanged?.Invoke();
                break;
        }

        base.WndProc(ref m);
    }
}
