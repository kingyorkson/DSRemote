using System.IO;
using System.Runtime.InteropServices;

namespace DSRemote.Services;

public class ScreenCaptureService
{
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    private static extern bool ClientToScreen(IntPtr hWnd, ref POINT lpPoint);

    [DllImport("gdi32.dll")]
    private static extern bool BitBlt(IntPtr hdc, int nXDest, int nYDest, int nWidth, int nHeight,
        IntPtr hdcSrc, int nXSrc, int nYSrc, uint dwRop);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT { public int Left; public int Top; public int Right; public int Bottom; }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT { public int X; public int Y; }

    private const uint SRCCOPY = 0x00CC0020;

    public byte[]? CaptureWindow(IntPtr hWnd)
    {
        if (hWnd == IntPtr.Zero) return null;

        if (!GetClientRect(hWnd, out var rect)) return null;
        var width = rect.Right - rect.Left;
        var height = rect.Bottom - rect.Top;
        if (width <= 0 || height <= 0) return null;

        var topLeft = new POINT { X = rect.Left, Y = rect.Top };
        ClientToScreen(hWnd, ref topLeft);

        using var bitmap = new System.Drawing.Bitmap(width, height);
        using var gDest = System.Drawing.Graphics.FromImage(bitmap);
        var hdcDest = gDest.GetHdc();

        using var gSrc = System.Drawing.Graphics.FromHwnd(IntPtr.Zero);
        var hdcSrc = gSrc.GetHdc();

        BitBlt(hdcDest, 0, 0, width, height, hdcSrc, topLeft.X, topLeft.Y, SRCCOPY);

        gSrc.ReleaseHdc(hdcSrc);
        gDest.ReleaseHdc(hdcDest);

        using var ms = new MemoryStream();
        bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
        return ms.ToArray();
    }

    public string CaptureWindowAsBase64(IntPtr hWnd)
    {
        var bytes = CaptureWindow(hWnd);
        if (bytes == null) return string.Empty;
        return "IMAGE:" + Convert.ToBase64String(bytes);
    }
}
