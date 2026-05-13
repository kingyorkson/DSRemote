using System.Runtime.InteropServices;
using DSRemote.Models;

namespace DSRemote.Services;

public class InputMapper
{
    [DllImport("user32.dll")]
    private static extern bool PostMessage(IntPtr hWnd, uint msg, nint wParam, nint lParam);

    private const uint WM_KEYDOWN = 0x0100;
    private const uint WM_KEYUP = 0x0101;

    private Dictionary<int, int> _buttonMap = new();
    private Dictionary<int, int> _dpadMap = new();

    public void LoadConfig(AppConfig config)
    {
        _buttonMap = new Dictionary<int, int>(config.ButtonMappings);
        _dpadMap = new Dictionary<int, int>(config.DPadMappings);
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
}
