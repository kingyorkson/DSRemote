using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace DSRemote.Services;

public class ScreenRegionDetector
{
    private Rectangle _topScreen;
    private Rectangle _bottomScreen;
    private Size _lastWindowSize;

    public (Rectangle Top, Rectangle Bottom) Detect(Bitmap screenshot)
    {
        if (_topScreen != Rectangle.Empty && _bottomScreen != Rectangle.Empty
            && _lastWindowSize == screenshot.Size)
            return (_topScreen, _bottomScreen);

        var width = screenshot.Width;
        var height = screenshot.Height;

        // Try to find two bright screen regions
        var rowBrightness = ComputeRowBrightness(screenshot, width, height);
        var maxBright = rowBrightness.Max();

        // Try multiple threshold levels in case one doesn't work
        double[] thresholds = { maxBright * 0.15, maxBright * 0.10, maxBright * 0.05 };
        List<(int Start, int End)> regions = new();

        foreach (var thresh in thresholds)
        {
            regions = FindRowRegions(rowBrightness, thresh, height);
            regions = regions.Where(r => (r.End - r.Start) >= 30).ToList();

            if (regions.Count >= 2)
            {
                // Found enough regions — keep the two largest
                regions = regions.OrderByDescending(r => r.End - r.Start).Take(2)
                                 .OrderBy(r => r.Start).ToList();
                break;
            }
        }

        // If still no luck, try the full height sorted by brightness percentile
        if (regions.Count < 2)
        {
            var sorted = rowBrightness.OrderByDescending(b => b).ToArray();
            var median = sorted[sorted.Length / 2];
            var adaptiveThresh = (maxBright + median) / 2;
            regions = FindRowRegions(rowBrightness, adaptiveThresh, height);
            regions = regions.Where(r => (r.End - r.Start) >= 30)
                             .OrderByDescending(r => r.End - r.Start)
                             .Take(2)
                             .OrderBy(r => r.Start)
                             .ToList();
        }

        Rectangle top, bottom;

        if (regions.Count >= 2)
        {
            var topRegion = regions[0];
            var bottomRegion = regions[1];

            var topCols = FindColumnBounds(screenshot, width, topRegion);
            var bottomCols = FindColumnBounds(screenshot, width, bottomRegion);

            top = new Rectangle(
                topCols.Left, topRegion.Start,
                topCols.Right - topCols.Left + 1,
                topRegion.End - topRegion.Start + 1);

            bottom = new Rectangle(
                bottomCols.Left, bottomRegion.Start,
                bottomCols.Right - bottomCols.Left + 1,
                bottomRegion.End - bottomRegion.Start + 1);
        }
        else if (regions.Count == 1)
        {
            // Only one region found — split into top 60% / bottom 40%
            var region = regions[0];
            var regionH = region.End - region.Start + 1;
            var splitY = region.Start + (int)(regionH * 0.55);

            var topCols = FindColumnBounds(screenshot, width, (region.Start, splitY));
            var bottomCols = FindColumnBounds(screenshot, width, (splitY + 1, region.End));

            top = new Rectangle(
                topCols.Left, region.Start,
                topCols.Right - topCols.Left + 1,
                splitY - region.Start + 1);

            bottom = new Rectangle(
                bottomCols.Left, splitY + 1,
                bottomCols.Right - bottomCols.Left + 1,
                region.End - splitY);
        }
        else
        {
            // Fallback: split window in half
            var half = height / 2;
            top = new Rectangle(0, 0, width, half);
            bottom = new Rectangle(0, half, width, height - half);
        }

        // Ensure rectangles are within bounds
        top.Intersect(new Rectangle(0, 0, width, height));
        bottom.Intersect(new Rectangle(0, 0, width, height));

        if (top.Width <= 0 || top.Height <= 0)
            top = new Rectangle(0, 0, width, height / 2);
        if (bottom.Width <= 0 || bottom.Height <= 0)
            bottom = new Rectangle(0, height / 2, width, height - height / 2);

        _topScreen = top;
        _bottomScreen = bottom;
        _lastWindowSize = screenshot.Size;
        return (top, bottom);
    }

    private static double[] ComputeRowBrightness(Bitmap bmp, int width, int height)
    {
        var result = new double[height];
        var rect = new Rectangle(0, 0, width, height);

        try
        {
            var bd = bmp.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            try
            {
                int stride = bd.Stride;
                int stridePadding = stride - width * 3;
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
                    offset += stridePadding;
                    result[y] = sum / (double)width;
                }
            }
            finally
            {
                bmp.UnlockBits(bd);
            }
        }
        catch
        {
            // If LockBits fails, fallback to GetPixel (slower but works)
            for (int y = 0; y < height; y++)
            {
                long sum = 0;
                for (int x = 0; x < width; x++)
                {
                    var p = bmp.GetPixel(x, y);
                    sum += (p.R * 299 + p.G * 587 + p.B * 114) / 1000;
                }
                result[y] = sum / (double)width;
            }
        }

        return result;
    }

    private static List<(int Start, int End)> FindRowRegions(
        double[] rowBrightness, double threshold, int height)
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
                if (y - start.Value >= 20)
                    regions.Add((start.Value, y - 1));
                start = null;
            }
        }

        if (start.HasValue && height - start.Value >= 20)
            regions.Add((start.Value, height - 1));

        return regions;
    }

    private static (int Left, int Right) FindColumnBounds(
        Bitmap bmp, int width, (int Start, int End) rowRegion)
    {
        var rowH = rowRegion.End - rowRegion.Start + 1;
        if (rowH <= 0) return (0, width - 1);

        var rect = new Rectangle(0, rowRegion.Start, width, rowH);
        var colBrightness = new double[width];

        try
        {
            var bd = bmp.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            try
            {
                int stride = bd.Stride;
                int stridePadding = stride - width * 3;
                var pixels = new byte[stride * rowH];
                Marshal.Copy(bd.Scan0, pixels, 0, pixels.Length);

                int offset = 0;
                for (int y = 0; y < rowH; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        var b = pixels[offset++];
                        var g = pixels[offset++];
                        var r = pixels[offset++];
                        colBrightness[x] += (r * 299 + g * 587 + b * 114) / 1000.0;
                    }
                    offset += stridePadding;
                }
            }
            finally
            {
                bmp.UnlockBits(bd);
            }
        }
        catch
        {
            for (int y = rowRegion.Start; y <= rowRegion.End; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var p = bmp.GetPixel(x, y);
                    colBrightness[x] += (p.R * 299 + p.G * 587 + p.B * 114) / 1000.0;
                }
            }
        }

        for (int x = 0; x < width; x++)
            colBrightness[x] /= rowH;

        var maxCol = colBrightness.Max();
        var colThresh = maxCol * 0.15;
        int left = 0, right = width - 1;

        for (int x = 0; x < width; x++)
        {
            if (colBrightness[x] > colThresh) { left = x; break; }
        }
        for (int x = width - 1; x >= 0; x--)
        {
            if (colBrightness[x] > colThresh) { right = x; break; }
        }

        return (left, right);
    }
}
