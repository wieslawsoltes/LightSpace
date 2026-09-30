namespace LightSpace.Core;

public enum CropGuide { Thirds, Grid, GoldenRatio, Diagonals, Triangle, None }
public readonly record struct CropGuideLine(PointD Start, PointD End);

/// <summary>Bounded, allocation-free composition guides in view pixels; never part of exported photo processing.</summary>
public static class CropGuides
{
    public const int MaximumLines = 16;
    public static int Write(CropGuide guide, RectD rect, bool reversed, Span<CropGuideLine> destination)
    {
        if (!Enum.IsDefined(guide)) throw new ArgumentOutOfRangeException(nameof(guide));
        var count = guide switch { CropGuide.None => 0, CropGuide.Grid => 16, CropGuide.Triangle => 3, _ => 4 };
        if (destination.Length < count) throw new ArgumentException("Insufficient guide-line capacity.", nameof(destination));
        if (!double.IsFinite(rect.X) || !double.IsFinite(rect.Y) || !double.IsFinite(rect.Width) || !double.IsFinite(rect.Height)
            || rect.Width <= 0 || rect.Height <= 0) return 0;
        if (guide == CropGuide.None) return 0;
        if (guide is CropGuide.Thirds or CropGuide.Grid or CropGuide.GoldenRatio)
        {
            var divisions = guide == CropGuide.Grid ? 9 : 3;
            for (var i = 1; i < divisions; i++)
            {
                var fraction = guide == CropGuide.GoldenRatio ? (i == 1 ? .3819660112501051 : .6180339887498949) : (double)i / divisions;
                var x = rect.X + rect.Width * fraction; var y = rect.Y + rect.Height * fraction;
                destination[(i - 1) * 2] = new(new(x, rect.Y), new(x, rect.Bottom));
                destination[(i - 1) * 2 + 1] = new(new(rect.X, y), new(rect.Right, y));
            }
        }
        else if (guide == CropGuide.Diagonals)
        {
            var d = Math.Min(rect.Width, rect.Height);
            destination[0] = new(new(rect.X, rect.Y), new(rect.X + d, rect.Y + d));
            destination[1] = new(new(rect.Right, rect.Y), new(rect.Right - d, rect.Y + d));
            destination[2] = new(new(rect.X, rect.Bottom), new(rect.X + d, rect.Bottom - d));
            destination[3] = new(new(rect.Right, rect.Bottom), new(rect.Right - d, rect.Bottom - d));
        }
        else
        {
            var w = rect.Width; var h = rect.Height; var denominator = w * w + h * h;
            var top = new PointD(rect.Right, rect.Y); var bottom = new PointD(rect.X, rect.Bottom);
            var a = new PointD(rect.X, rect.Y); var b = new PointD(rect.Right, rect.Bottom);
            var p = new PointD(rect.X + w * w * w / denominator, rect.Y + h * w * w / denominator);
            var q = new PointD(rect.X + w * h * h / denominator, rect.Y + h * h * h / denominator);
            destination[0] = new(a, b); destination[1] = new(top, p); destination[2] = new(bottom, q);
            if (reversed)
                for (var i = 0; i < 3; i++)
                {
                    var line = destination[i];
                    destination[i] = new(new(rect.X + rect.Right - line.Start.X, line.Start.Y), new(rect.X + rect.Right - line.End.X, line.End.Y));
                }
        }
        return count;
    }
}
