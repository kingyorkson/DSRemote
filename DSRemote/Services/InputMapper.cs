using System.Runtime.InteropServices;
using DSRemote.Models;

namespace DSRemote.Services;

public class InputMapper
{
    [DllImport("user32.dll")]
    private static extern bool PostMessage(IntPtr hWnd, uint msg, nint wParam, nint lParam);

    [DllImport("user32.dll")]
    private static extern bool ClientToScreen(IntPtr hWnd, ref POINT lpPoint);

    [DllImport("user32.dll")]
    private static extern bool SetCursorPos(int X, int Y);

    [DllImport("user32.dll")]
    private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, nint dwExtraInfo);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT { public int X; public int Y; }

    private const uint WM_KEYDOWN = 0x0100;
    private const uint WM_KEYUP = 0x0101;
    private const uint MOUSEEVENTF_LEFTDOWN = 0x02;
    private const uint MOUSEEVENTF_LEFTUP = 0x04;

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
            PostMessage(hWnd, WM_KEYDOWN, vk, 0);
    }

    public void SendButtonUp(IntPtr hWnd, int buttonId)
    {
        if (_buttonMap.TryGetValue(buttonId, out var vk))
            PostMessage(hWnd, WM_KEYUP, vk, 0);
    }

    public void SendDPad(IntPtr hWnd, int direction)
    {
        if (!_dpadMap.TryGetValue(direction, out var vk)) return;
        PostMessage(hWnd, WM_KEYDOWN, vk, 0);
        Task.Delay(50).ContinueWith(_ => PostMessage(hWnd, WM_KEYUP, vk, 0));
    }

    public void SendJoystickMove(IntPtr hWnd, float x, float y)
    {
        const int VK_LEFT = 0x25, VK_RIGHT = 0x27, VK_UP = 0x26, VK_DOWN = 0x28;
        const int threshold = 30;

        PostMessage(hWnd, WM_KEYUP, VK_LEFT, 0);
        PostMessage(hWnd, WM_KEYUP, VK_RIGHT, 0);
        PostMessage(hWnd, WM_KEYUP, VK_UP, 0);
        PostMessage(hWnd, WM_KEYUP, VK_DOWN, 0);

        if (x < -threshold) PostMessage(hWnd, WM_KEYDOWN, VK_LEFT, 0);
        if (x > threshold) PostMessage(hWnd, WM_KEYDOWN, VK_RIGHT, 0);
        if (y < -threshold) PostMessage(hWnd, WM_KEYDOWN, VK_UP, 0);
        if (y > threshold) PostMessage(hWnd, WM_KEYDOWN, VK_DOWN, 0);
    }

    public void SendTouchDown(IntPtr hWnd, float nx, float ny)
    {
        var (screenX, screenY) = MapTouchToScreen(hWnd, nx, ny);
        if (screenX < 0 || screenY < 0) return;
        SetCursorPos(screenX, screenY);
        mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
    }

    public void SendTouchMove(IntPtr hWnd, float nx, float ny)
    {
        var (screenX, screenY) = MapTouchToScreen(hWnd, nx, ny);
        if (screenX < 0 || screenY < 0) return;
        SetCursorPos(screenX, screenY);
    }

    public void SendTouchUp(IntPtr hWnd)
    {
        mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
    }

    private (int X, int Y) MapTouchToScreen(IntPtr hWnd, float nx, float ny)
    {
        if (_capture == null) return (-1, -1);

        var bottom = _capture.LastBottomScreenRect;
        var winSize = _capture.LastWindowSize;
        if (bottom.IsEmpty || winSize.IsEmpty) return (-1, -1);

        // Clamp normalized coords
        nx = Math.Clamp(nx, 0f, 1f);
        ny = Math.Clamp(ny, 0f, 1f);

        // Map to pixel coords within the bottom screen rect in client area
        int clientX = bottom.X + (int)(nx * bottom.Width);
        int clientY = bottom.Y + (int)(ny * bottom.Height);

        // Convert client coords to screen coords
        var pt = new POINT { X = clientX, Y = clientY };
        if (!ClientToScreen(hWnd, ref pt))
            return (-1, -1);

        return (pt.X, pt.Y);
    }
}
