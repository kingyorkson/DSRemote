using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace DSRemote.Services;

public class ScreenRegionDetector
{
    private Rectangle? _topScreen;
    private Rectangle? _bottomScreen;
    private Size _lastWindowSize;

    public (Rectangle Top, Rectangle Bottom) Detect(Bitmap screenshot)
    {
        if (_topScreen.HasValue && _bottomScreen.HasValue && _lastWindowSize == screenshot.Size)
            return (_topScreen.Value, _bottomScreen.Value);

        var width = screenshot.Width;
        var height = screenshot.Height;

        var rowBrightness = ComputeRowBrightness(screenshot, width, height);
        var threshold = rowBrightness.Max() * 0.25;
        var regions = FindRowRegions(rowBrightness, threshold, height);

        regions = regions.OrderByDescending(r => r.End - r.Start).Take(2)
                         .OrderBy(r => r.Start).ToList();

        Rectangle top, bottom;
        if (regions.Count >= 2)
        {
            var topCols = FindColumnBounds(screenshot, width, regions[0]);
            var bottomCols = FindColumnBounds(screenshot, width, regions[1]);
            top = new Rectangle(topCols.Left, regions[0].Start, topCols.Right - topCols.Left + 1, regions[0].End - regions[0].Start + 1);
            bottom = new Rectangle(bottomCols.Left, regions[1].Start, bottomCols.Right - bottomCols.Left + 1, regions[1].End - regions[1].Start + 1);
        }
        else if (regions.Count == 1)
        {
            var half = (regions[0].Start + regions[0].End) / 2;
            var topCols = FindColumnBounds(screenshot, width, (regions[0].Start, half));
            var bottomCols = FindColumnBounds(screenshot, width, (half + 1, regions[0].End));
            top = new Rectangle(topCols.Left, regions[0].Start, topCols.Right - topCols.Left + 1, half - regions[0].Start + 1);
            bottom = new Rectangle(bottomCols.Left, half + 1, bottomCols.Right - bottomCols.Left + 1, regions[0].End - half);
        }
        else
        {
            var half = height / 2;
            top = new Rectangle(0, 0, width, half);
            bottom = new Rectangle(0, half, width, height - half);
        }

        _topScreen = top;
        _bottomScreen = bottom;
        _lastWindowSize = screenshot.Size;
        return (top, bottom);
    }

    private static double[] ComputeRowBrightness(Bitmap bmp, int width, int height)
    {
        var result = new double[height];
        var rect = new Rectangle(0, 0, width, height);
        var bd = bmp.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
        try
        {
            int stride = bd.Stride;
            int strideOffset = stride - width * 3;
            var pixels = new byte[stride * height];
            Marshal.Copy(bd.Scan0, pixels, 0, pixels.Length);

            int offset = 0;
            for (int y = 0; y < height; y++)
            {
                long sum = 0;
                for (int x = 0; x < width; x++)
                {
                    var b = pixels[offset++];
                    var g = pixels[offset++];
                    var r = pixels[offset++];
                    sum += (r * 299 + g * 587 + b * 114) / 1000;
                }
                offset += strideOffset;
                result[y] = sum / (double)width;
            }
        }
        finally
        {
            bmp.UnlockBits(bd);
        }
        return result;
    }

    private static List<(int Start, int End)> FindRowRegions(double[] rowBrightness, double threshold, int height)
    {
        var regions = new List<(int Start, int End)>();
        int? start = null;
        for (int y = 0; y < height; y++)
        {
            if (rowBrightness[y] > threshold)
            {
                if (start == null) start = y;
            }
            else if (start.HasValue)
            {
                if (y - start.Value >= 30)
                    regions.Add((start.Value, y - 1));
                start = null;
            }
        }
        if (start.HasValue && height - start.Value >= 30)
            regions.Add((start.Value, height - 1));
        return regions;
    }

    private static (int Left, int Right) FindColumnBounds(Bitmap bmp, int width, (int Start, int End) rowRegion)
    {
        var rect = new Rectangle(0, rowRegion.Start, width, rowRegion.End - rowRegion.Start + 1);
        var bd = bmp.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
        try
        {
            int stride = bd.Stride;
            int height = rect.Height;
            var pixels = new byte[stride * height];
            Marshal.Copy(bd.Scan0, pixels, 0, pixels.Length);

            var colBrightness = new double[width];
            int offset = 0;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var b = pixels[offset++];
                    var g = pixels[offset++];
                    var r = pixels[offset++];
                    colBrightness[x] += (r * 299 + g * 587 + b * 114) / 1000.0;
                }
                offset += stride - width * 3;
            }

            for (int x = 0; x < width; x++)
                colBrightness[x] /= height;

            var threshold = colBrightness.Max() * 0.2;
            int left = 0, right = width - 1;
            for (int x = 0; x < width; x++)
            {
                if (colBrightness[x] > threshold) { left = x; break; }
            }
            for (int x = width - 1; x >= 0; x--)
            {
                if (colBrightness[x] > threshold) { right = x; break; }
            }
            return (left, right);
        }
        finally
        {
            bmp.UnlockBits(bd);
        }
    }
}
