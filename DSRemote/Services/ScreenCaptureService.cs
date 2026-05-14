using System.Drawing;
using System.Drawing.Imaging;
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

    [DllImport("user32.dll")]
    private static extern bool PrintWindow(IntPtr hWnd, IntPtr hdcBlt, int nFlags);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT { public int Left; public int Top; public int Right; public int Bottom; }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT { public int X; public int Y; }

    private const uint SRCCOPY = 0x00CC0020;
    private const int PW_CLIENTONLY = 1;

    private readonly ScreenRegionDetector _detector = new();
    public Rectangle LastBottomScreenRect { get; private set; }
    public Size LastWindowSize { get; private set; }

    public byte[]? CaptureWindow(IntPtr hWnd)
    {
        if (hWnd == IntPtr.Zero) return null;

        if (!GetClientRect(hWnd, out var rect)) return null;
        var width = rect.Right - rect.Left;
        var height = rect.Bottom - rect.Top;
        if (width <= 0 || height <= 0) return null;

        using var bitmap = new Bitmap(width, height);
        using var gDest = Graphics.FromImage(bitmap);
        var hdcDest = gDest.GetHdc();

        // Try PrintWindow first (works on hidden/minimized windows)
        bool success = PrintWindow(hWnd, hdcDest, PW_CLIENTONLY);

        // Fallback to BitBlt if PrintWindow fails
        if (!success)
        {
            var topLeft = new POINT { X = rect.Left, Y = rect.Top };
            ClientToScreen(hWnd, ref topLeft);

            using var gSrc = Graphics.FromHwnd(IntPtr.Zero);
            var hdcSrc = gSrc.GetHdc();
            BitBlt(hdcDest, 0, 0, width, height, hdcSrc, topLeft.X, topLeft.Y, SRCCOPY);
            gSrc.ReleaseHdc(hdcSrc);
        }

        gDest.ReleaseHdc(hdcDest);

        using var ms = new MemoryStream();
        bitmap.Save(ms, ImageFormat.Jpeg);
        return ms.ToArray();
    }

    public string CaptureWindowAsBase64(IntPtr hWnd)
    {
        var bytes = CaptureWindow(hWnd);
        if (bytes == null) return string.Empty;
        return "IMAGE:" + Convert.ToBase64String(bytes);
    }

    public (string Top, string Bottom) CaptureScreensAsBase64(IntPtr hWnd)
    {
        if (hWnd == IntPtr.Zero) return (string.Empty, string.Empty);

        if (!GetClientRect(hWnd, out var rect)) return (string.Empty, string.Empty);
        var width = rect.Right - rect.Left;
        var height = rect.Bottom - rect.Top;
        if (width <= 0 || height <= 0) return (string.Empty, string.Empty);

        using var fullBitmap = new Bitmap(width, height);

        using (var gDest = Graphics.FromImage(fullBitmap))
        {
            var hdcDest = gDest.GetHdc();

            // Try PrintWindow first
            bool success = PrintWindow(hWnd, hdcDest, PW_CLIENTONLY);

            if (!success)
            {
                var topLeft = new POINT { X = rect.Left, Y = rect.Top };
                ClientToScreen(hWnd, ref topLeft);

                using var gSrc = Graphics.FromHwnd(IntPtr.Zero);
                var hdcSrc = gSrc.GetHdc();
                BitBlt(hdcDest, 0, 0, width, height, hdcSrc, topLeft.X, topLeft.Y, SRCCOPY);
                gSrc.ReleaseHdc(hdcSrc);
            }

            gDest.ReleaseHdc(hdcDest);
        }

        var (topRect, bottomRect) = _detector.Detect(fullBitmap);
        LastBottomScreenRect = bottomRect;
        LastWindowSize = new Size(width, height);

        string EncodeRegion(Bitmap source, Rectangle region)
        {
            region.Intersect(new Rectangle(0, 0, source.Width, source.Height));
            if (region.Width <= 0 || region.Height <= 0) return string.Empty;
            using var cropped = source.Clone(region, PixelFormat.Format24bppRgb);
            using var ms = new MemoryStream();
            cropped.Save(ms, ImageFormat.Jpeg);
            return Convert.ToBase64String(ms.ToArray());
        }

        var topB64 = EncodeRegion(fullBitmap, topRect);
        var bottomB64 = EncodeRegion(fullBitmap, bottomRect);

        return ("TOP_IMAGE:" + topB64, "BOTTOM_IMAGE:" + bottomB64);
    }
}
