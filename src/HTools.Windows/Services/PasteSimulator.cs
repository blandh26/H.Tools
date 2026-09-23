using HTools.Windows.Interop;

namespace HTools.Windows.Services;

/// <summary>Simulates a Ctrl+V keystroke via SendInput so the currently focused window pastes.</summary>
public static class PasteSimulator
{
    public static void SendCtrlV()
    {
        var inputs = new NativeMethods.INPUT[4];

        inputs[0] = KeyInput(NativeMethods.VK_CONTROL, keyUp: false);
        inputs[1] = KeyInput(NativeMethods.VK_V, keyUp: false);
        inputs[2] = KeyInput(NativeMethods.VK_V, keyUp: true);
        inputs[3] = KeyInput(NativeMethods.VK_CONTROL, keyUp: true);

        NativeMethods.SendInput((uint)inputs.Length, inputs, System.Runtime.InteropServices.Marshal.SizeOf<NativeMethods.INPUT>());
    }

    private static NativeMethods.INPUT KeyInput(ushort virtualKey, bool keyUp)
    {
        return new NativeMethods.INPUT
        {
            Type = NativeMethods.INPUT_KEYBOARD,
            U = new NativeMethods.InputUnion
            {
                Ki = new NativeMethods.KEYBDINPUT
                {
                    Vk = virtualKey,
                    Scan = 0,
                    Flags = keyUp ? NativeMethods.KEYEVENTF_KEYUP : 0,
                    Time = 0,
                    ExtraInfo = 0,
                },
            },
        };
    }
}
