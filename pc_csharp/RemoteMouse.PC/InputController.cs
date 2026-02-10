using System;
using System.Runtime.InteropServices;
using System.Windows;

namespace RemoteMouse.PC;

public class InputController
{
    private const int INPUT_MOUSE = 0;
    private const int INPUT_KEYBOARD = 1;
    private const uint MOUSEEVENTF_MOVE = 0x0001;
    private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    private const uint MOUSEEVENTF_LEFTUP = 0x0004;
    private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
    private const uint MOUSEEVENTF_RIGHTUP = 0x0010;
    private const uint MOUSEEVENTF_WHEEL = 0x0800;
    private const uint KEYEVENTF_KEYUP = 0x0002;
    private const uint KEYEVENTF_UNICODE = 0x0004;

    [StructLayout(LayoutKind.Sequential)]
    private struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct INPUT_UNION
    {
        [FieldOffset(0)] public MOUSEINPUT mi;
        [FieldOffset(0)] public KEYBDINPUT ki;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public int type;
        public INPUT_UNION u;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, ref INPUT pInputs, int cbSize);

    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    private static void SendKeyInput(ushort vk, bool keyUp)
    {
        var inp = new INPUT
        {
            type = INPUT_KEYBOARD,
            u = new INPUT_UNION
            {
                ki = new KEYBDINPUT
                {
                    wVk = vk,
                    wScan = 0,
                    dwFlags = keyUp ? KEYEVENTF_KEYUP : 0,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };
        SendInput(1, ref inp, Marshal.SizeOf<INPUT>());
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool ExitWindowsEx(uint uFlags, uint dwReason);

    [DllImport("PowrProf.dll")]
    private static extern bool SetSuspendState(bool hibernate, bool forceCritical, bool disableWakeEvent);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetProcessShutdownParameters(uint dwLevel, uint dwFlags);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool LockWorkStation();

    private const uint EWX_LOGOFF = 0;
    private const uint EWX_SHUTDOWN = 0x00000001;
    private const uint EWX_REBOOT = 0x00000002;
    private const byte VK_VOLUME_UP = 0xAF;
    private const byte VK_VOLUME_DOWN = 0xAE;
    private const byte VK_VOLUME_MUTE = 0xAD;

    public void MouseMove(int dx, int dy)
    {
        var inp = new INPUT
        {
            type = INPUT_MOUSE,
            u = new INPUT_UNION
            {
                mi = new MOUSEINPUT
                {
                    dx = dx,
                    dy = dy,
                    mouseData = 0,
                    dwFlags = MOUSEEVENTF_MOVE,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };
        SendInput(1, ref inp, Marshal.SizeOf<INPUT>());
    }

    public void MouseButton(int button, bool down)
    {
        uint flagDown = button switch
        {
            1 => MOUSEEVENTF_LEFTDOWN,
            2 => MOUSEEVENTF_RIGHTDOWN,
            _ => 0
        };
        uint flagUp = button switch
        {
            1 => MOUSEEVENTF_LEFTUP,
            2 => MOUSEEVENTF_RIGHTUP,
            _ => 0
        };
        if (button == 3)
        {
            var inp = new INPUT
            {
                type = INPUT_MOUSE,
                u = new INPUT_UNION
                {
                    mi = new MOUSEINPUT
                    {
                        dx = 0,
                        dy = 0,
                        mouseData = (uint)(down ? 120 : unchecked((int)0xFF88)),
                        dwFlags = MOUSEEVENTF_WHEEL,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };
            SendInput(1, ref inp, Marshal.SizeOf<INPUT>());
            return;
        }
        if (flagDown == 0) return;
        var f = down ? flagDown : flagUp;
        var i = new INPUT
        {
            type = INPUT_MOUSE,
            u = new INPUT_UNION
            {
                mi = new MOUSEINPUT
                {
                    dx = 0,
                    dy = 0,
                    mouseData = 0,
                    dwFlags = f,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };
        SendInput(1, ref i, Marshal.SizeOf<INPUT>());
    }

    public void MouseScroll(int dx, int dy)
    {
        int delta = dy != 0 ? dy : dx;
        var inp = new INPUT
        {
            type = INPUT_MOUSE,
            u = new INPUT_UNION
            {
                mi = new MOUSEINPUT
                {
                    dx = 0,
                    dy = 0,
                    mouseData = (uint)delta,
                    dwFlags = MOUSEEVENTF_WHEEL,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };
        SendInput(1, ref inp, Marshal.SizeOf<INPUT>());
    }

    public void Key(uint vkey, bool down)
    {
        var inp = new INPUT
        {
            type = INPUT_KEYBOARD,
            u = new INPUT_UNION
            {
                ki = new KEYBDINPUT
                {
                    wVk = (ushort)(vkey & 0xFFFF),
                    wScan = 0,
                    dwFlags = down ? 0u : KEYEVENTF_KEYUP,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };
        SendInput(1, ref inp, Marshal.SizeOf<INPUT>());
    }

    public void SendChar(uint codePoint)
    {
        if (codePoint <= 0xFFFF)
        {
            SendUnicodeKey((ushort)codePoint, false);
            SendUnicodeKey((ushort)codePoint, true);
            return;
        }
        uint v = codePoint - 0x10000;
        ushort high = (ushort)(0xD800 + (v >> 10));
        ushort low = (ushort)(0xDC00 + (v & 0x3FF));
        SendUnicodeKey(high, false);
        SendUnicodeKey(high, true);
        SendUnicodeKey(low, false);
        SendUnicodeKey(low, true);
    }

    private static void SendUnicodeKey(ushort ch, bool keyUp)
    {
        var inp = new INPUT
        {
            type = INPUT_KEYBOARD,
            u = new INPUT_UNION
            {
                ki = new KEYBDINPUT
                {
                    wVk = 0,
                    wScan = ch,
                    dwFlags = keyUp ? (KEYEVENTF_KEYUP | KEYEVENTF_UNICODE) : KEYEVENTF_UNICODE,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };
        SendInput(1, ref inp, Marshal.SizeOf<INPUT>());
    }

    public void KeyPress(uint vkey)
    {
        Key(vkey, true);
        Key(vkey, false);
    }

    public void ClipboardSet(string text)
    {
        System.Windows.Clipboard.SetText(text);
    }

    public string ClipboardGet()
    {
        return System.Windows.Clipboard.GetText() ?? "";
    }

    public void PowerShutdown()
    {
        SetProcessShutdownParameters(0x4FF, 0);
        ExitWindowsEx(EWX_SHUTDOWN, 0);
    }

    public void PowerReboot()
    {
        SetProcessShutdownParameters(0x4FF, 0);
        ExitWindowsEx(EWX_REBOOT, 0);
    }

    public void PowerSleep()
    {
        SetSuspendState(false, true, false);
    }

    public void PowerLogout()
    {
        ExitWindowsEx(EWX_LOGOFF, 0);
    }

    public void PowerLock()
    {
        if (LockWorkStation())
            return;
        const ushort VK_LWIN = 0x5B;
        const ushort VK_L = 0x4C;
        SendKeyInput(VK_LWIN, false);
        SendKeyInput(VK_L, false);
        SendKeyInput(VK_L, true);
        SendKeyInput(VK_LWIN, true);
    }

    public void VolumeUp()
    {
        keybd_event(VK_VOLUME_UP, 0, 0, UIntPtr.Zero);
        keybd_event(VK_VOLUME_UP, 0, 2, UIntPtr.Zero);
    }

    public void VolumeDown()
    {
        keybd_event(VK_VOLUME_DOWN, 0, 0, UIntPtr.Zero);
        keybd_event(VK_VOLUME_DOWN, 0, 2, UIntPtr.Zero);
    }

    public void VolumeMute()
    {
        keybd_event(VK_VOLUME_MUTE, 0, 0, UIntPtr.Zero);
        keybd_event(VK_VOLUME_MUTE, 0, 2, UIntPtr.Zero);
    }
}
