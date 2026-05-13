using System.Runtime.InteropServices;

namespace DSRemote.Services;

public class InputMapper
{
    [DllImport("user32.dll")]
    private static extern bool PostMessage(IntPtr hWnd, uint msg, nint wParam, nint lParam);

    private const uint WM_KEYDOWN = 0x0100;
    private const uint WM_KEYUP = 0x0101;

    // MelonDS default key mappings: button ID -> virtual key code
    public Dictionary<int, int> ButtonMap { get; set; } = new()
    {
        { 0, (int)'Z' },     // A
        { 1, (int)'X' },     // B
        { 2, (int)'S' },     // X
        { 3, (int)'A' },     // Y
        { 4, (int)'Q' },     // L
        { 5, (int)'W' },     // R
        { 6, (int)'Y' },     // Start (will map to Return below if needed)
        { 7, (int)'U' },     // Select
    };

    // Special key overrides for keys that aren't simple letters
    private static readonly Dictionary<int, int> SpecialKeys = new()
    {
        { 6, 0x0D },  // Start -> Enter
        { 7, 0x08 },  // Select -> Backspace
    };

    // D-Pad
    public Dictionary<int, int> DPadMap { get; set; } = new()
    {
        { 0, 0x26 },  // Up
        { 1, 0x28 },  // Down
        { 2, 0x25 },  // Left
        { 3, 0x27 },  // Right
    };

    public void SendButtonDown(IntPtr hWnd, int buttonId)
    {
        var vk = SpecialKeys.TryGetValue(buttonId, out var special) ? special : ButtonMap.GetValueOrDefault(buttonId);
        if (vk != 0)
            PostMessage(hWnd, WM_KEYDOWN, vk, 0);
    }

    public void SendButtonUp(IntPtr hWnd, int buttonId)
    {
        var vk = SpecialKeys.TryGetValue(buttonId, out var special) ? special : ButtonMap.GetValueOrDefault(buttonId);
        if (vk != 0)
            PostMessage(hWnd, WM_KEYUP, vk, 0);
    }

    public void SendDPad(IntPtr hWnd, int direction)
    {
        var vk = DPadMap.GetValueOrDefault(direction);
        if (vk == 0) return;
        PostMessage(hWnd, WM_KEYDOWN, vk, 0);
        Task.Delay(50).ContinueWith(_ => PostMessage(hWnd, WM_KEYUP, vk, 0));
    }

    public void SendJoystickMove(IntPtr hWnd, float x, float y)
    {
        // Map joystick to arrow keys (simple version)
        const int VK_LEFT = 0x25, VK_RIGHT = 0x27, VK_UP = 0x26, VK_DOWN = 0x28;
        const int threshold = 30;

        // Release all direction keys first
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
