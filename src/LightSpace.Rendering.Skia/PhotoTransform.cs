using LightSpace.Core;
using SkiaSharp;
namespace LightSpace.Rendering.Skia;

public static class PhotoTransform
{
    public static SKRect Fit(CropSettings crop, int width, int height, SKRect available, float zoom = 1, float panX = 0, float panY = 0)
    {
        var (w, h) = crop.OutputSize(width, height); var scale = Math.Min(available.Width / w, available.Height / h) * zoom;
        var fw = w * scale; var fh = h * scale;
        return SKRect.Create(available.MidX - fw / 2 + panX, available.MidY - fh / 2 + panY, fw, fh);
    }
    public static SKMatrix SourceToView(CropSettings crop, int width, int height, SKRect destination)
    {
        var c = crop.Normalize(); var cw = c.Width * width; var ch = c.Height * height;
        float a = 1, b = 0, d = 0, e = 1, tx = 0, ty = 0;
        switch (c.QuarterTurns)
        {
            case 1: a = 0; b = -1; d = 1; e = 0; tx = ch; break;
            case 2: a = -1; e = -1; tx = cw; ty = ch; break;
            case 3: a = 0; b = 1; d = -1; e = 0; ty = cw; break;
        }
        var ow = c.QuarterTurns % 2 == 0 ? cw : ch; var oh = c.QuarterTurns % 2 == 0 ? ch : cw;
        tx -= a * c.Left * width + b * c.Top * height; ty -= d * c.Left * width + e * c.Top * height;
        if (c.FlipX) { a = -a; b = -b; tx = ow - tx; }
        if (c.FlipY) { d = -d; e = -e; ty = oh - ty; }
        var sx = destination.Width / ow; var sy = destination.Height / oh;
        return new(a * sx, b * sx, tx * sx + destination.Left, d * sy, e * sy, ty * sy + destination.Top, 0, 0, 1);
    }
}
