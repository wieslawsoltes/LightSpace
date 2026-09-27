using SkiaSharp;
namespace LightSpace.Rendering.Skia;

public sealed record Histogram(int[] Red, int[] Green, int[] Blue, int[] Luminance, int Count)
{
    public static Histogram Empty { get; } = new(new int[256], new int[256], new int[256], new int[256], 0);
    public static Histogram FromPixels(IEnumerable<SKColor> pixels)
    {
        var r = new int[256]; var g = new int[256]; var b = new int[256]; var l = new int[256]; var count = 0;
        foreach (var p in pixels)
        {
            if (p.Alpha == 0) continue; r[p.Red]++; g[p.Green]++; b[p.Blue]++;
            l[(int)Math.Clamp(Math.Round(.2126 * p.Red + .7152 * p.Green + .0722 * p.Blue), 0, 255)]++; count++;
        }
        return new(r, g, b, l, count);
    }
}
