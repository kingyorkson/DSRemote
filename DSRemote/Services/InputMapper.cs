using System.Runtime.InteropServices;
using DSRemote.Models;

namespace DSRemote.Services;

public class InputMapper
{
    [DllImport("user32.dll")]
    private static extern bool ClientToScreen(IntPtr hWnd, ref POINT lpPoint);

    [DllImport("user32.dll")]
    private static extern uint SendInput(uint cInputs, INPUT[] pInputs, int cbSize);

    [DllImport("user32.dll")]
    private static extern IntPtr GetMessageExtraInfo();

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT { public int X; public int Y; }

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public uint type;
        public InputUnion u;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)] public MOUSEINPUT mi;
        [FieldOffset(0)] public KEYBDINPUT ki;
    }

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

    private const uint INPUT_KEYBOARD = 1;
    private const uint INPUT_MOUSE = 0;
    private const uint KEYEVENTF_KEYUP = 0x0002;
    private const uint KEYEVENTF_SCANCODE = 0x0008;
    private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    private const uint MOUSEEVENTF_LEFTUP = 0x0004;
    private const uint MOUSEEVENTF_MOVE = 0x0001;
    private const uint MOUSEEVENTF_ABSOLUTE = 0x8000;

    private Dictionary<int, int> _buttonMap = new();
    private Dictionary<int, int> _dpadMap = new();
    private ScreenCaptureService? _capture;

    public void LoadConfig(AppConfig config)
    {
        _buttonMap = new Dictionary<int, int>(config.ButtonMappings);
        _dpadMap = new Dictionary<int, int>(config.DPadMappings);
    }

    public void SetCaptureService(ScreenCaptureService capture)
    {
        _capture = capture;
    }

    public void SendButtonDown(IntPtr hWnd, int buttonId)
    {
        if (_buttonMap.TryGetValue(buttonId, out var vk))
            SendKeyboardInput((ushort)vk, false);
    }

    public void SendButtonUp(IntPtr hWnd, int buttonId)
    {
        if (_buttonMap.TryGetValue(buttonId, out var vk))
            SendKeyboardInput((ushort)vk, true);
    }

    public void SendDPad(IntPtr hWnd, int direction)
    {
        if (!_dpadMap.TryGetValue(direction, out var vk)) return;
        SendKeyboardInput((ushort)vk, false);
        Thread.Sleep(50);
        SendKeyboardInput((ushort)vk, true);
    }

    public void SendJoystickMove(IntPtr hWnd, float x, float y)
    {
        const int VK_LEFT = 0x25, VK_RIGHT = 0x27, VK_UP = 0x26, VK_DOWN = 0x28;
        const int threshold = 30;

        SendKeyboardInput(VK_LEFT, true);
        SendKeyboardInput(VK_RIGHT, true);
        SendKeyboardInput(VK_UP, true);
        SendKeyboardInput(VK_DOWN, true);

        if (x < -threshold) SendKeyboardInput(VK_LEFT, false);
        if (x > threshold) SendKeyboardInput(VK_RIGHT, false);
        if (y < -threshold) SendKeyboardInput(VK_UP, false);
        if (y > threshold) SendKeyboardInput(VK_DOWN, false);
    }

    public void SendTouchDown(IntPtr hWnd, float nx, float ny)
    {
        var (screenX, screenY) = MapTouchToScreen(hWnd, nx, ny);
        if (screenX < 0 || screenY < 0) return;

        // Absolute move + click via SendInput
        var inputs = new INPUT[2];
        inputs[0] = CreateMouseMove(screenX, screenY);
        inputs[1] = CreateMouseButton(MOUSEEVENTF_LEFTDOWN);
        SendInput(2, inputs, Marshal.SizeOf<INPUT>());
    }

    public void SendTouchMove(IntPtr hWnd, float nx, float ny)
    {
        var (screenX, screenY) = MapTouchToScreen(hWnd, nx, ny);
        if (screenX < 0 || screenY < 0) return;

        var input = CreateMouseMove(screenX, screenY);
        SendInput(1, new[] { input }, Marshal.SizeOf<INPUT>());
    }

    public void SendTouchUp(IntPtr hWnd)
    {
        var input = CreateMouseButton(MOUSEEVENTF_LEFTUP);
        SendInput(1, new[] { input }, Marshal.SizeOf<INPUT>());
    }

    private static void SendKeyboardInput(ushort vk, bool keyUp)
    {
        var input = new INPUT
        {
            type = INPUT_KEYBOARD,
            u = new InputUnion
            {
                ki = new KEYBDINPUT
                {
                    wVk = vk,
                    wScan = 0,
                    dwFlags = keyUp ? KEYEVENTF_KEYUP : 0u,
                    time = 0,
                    dwExtraInfo = GetMessageExtraInfo()
                }
            }
        };
        SendInput(1, new[] { input }, Marshal.SizeOf<INPUT>());
    }

    private static INPUT CreateMouseMove(int x, int y)
    {
        // Normalize to absolute coordinates (0-65535)
        int absX = x * 65535 / GetSystemMetrics(0);
        int absY = y * 65535 / GetSystemMetrics(1);
        return new INPUT
        {
            type = INPUT_MOUSE,
            u = new InputUnion
            {
                mi = new MOUSEINPUT
                {
                    dx = absX,
                    dy = absY,
                    dwFlags = MOUSEEVENTF_MOVE | MOUSEEVENTF_ABSOLUTE,
                    mouseData = 0,
                    time = 0,
                    dwExtraInfo = GetMessageExtraInfo()
                }
            }
        };
    }

    private static INPUT CreateMouseButton(uint flags)
    {
        return new INPUT
        {
            type = INPUT_MOUSE,
            u = new InputUnion
            {
                mi = new MOUSEINPUT
                {
                    dx = 0,
                    dy = 0,
                    dwFlags = flags,
                    mouseData = 0,
                    time = 0,
                    dwExtraInfo = GetMessageExtraInfo()
                }
            }
        };
    }

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);

    private const int SM_CXSCREEN = 0;
    private const int SM_CYSCREEN = 1;

    private (int X, int Y) MapTouchToScreen(IntPtr hWnd, float nx, float ny)
    {
        if (_capture == null) return (-1, -1);

        var bottom = _capture.LastBottomScreenRect;
        var winSize = _capture.LastWindowSize;
        if (bottom.IsEmpty || winSize.IsEmpty) return (-1, -1);

        nx = Math.Clamp(nx, 0f, 1f);
        ny = Math.Clamp(ny, 0f, 1f);

        int clientX = bottom.X + (int)(nx * bottom.Width);
        int clientY = bottom.Y + (int)(ny * bottom.Height);

        var pt = new POINT { X = clientX, Y = clientY };
        if (!ClientToScreen(hWnd, ref pt))
            return (-1, -1);

        return (pt.X, pt.Y);
    }
}
