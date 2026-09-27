namespace LightSpace.Core;

public readonly record struct PointD(double X, double Y);
public readonly record struct RectD(double X, double Y, double Width, double Height)
{
    public double Right => X + Width;
    public double Bottom => Y + Height;
    public bool Contains(PointD p) => p.X >= X && p.X <= Right && p.Y >= Y && p.Y <= Bottom;
}

/// <summary>Source-normalized crop. Quarter turns and flips are applied after cropping.</summary>
public sealed record CropSettings(float Left = 0, float Top = 0, float Right = 1, float Bottom = 1, int QuarterTurns = 0, bool FlipX = false, bool FlipY = false)
{
    public float Width => Right - Left;
    public float Height => Bottom - Top;
    public CropSettings Normalize()
    {
        var left = Numeric.Clamp(Left, 0, .99f); var top = Numeric.Clamp(Top, 0, .99f);
        return this with { Left = left, Top = top, Right = Numeric.Clamp(Right, left + .01f, 1), Bottom = Numeric.Clamp(Bottom, top + .01f, 1), QuarterTurns = ((QuarterTurns % 4) + 4) % 4 };
    }
    public (int Width, int Height) OutputSize(int width, int height)
    {
        var c = Normalize(); var w = Math.Max(1, (int)Math.Round(width * c.Width)); var h = Math.Max(1, (int)Math.Round(height * c.Height));
        return c.QuarterTurns % 2 == 0 ? (w, h) : (h, w);
    }
}

public enum MaskKind { Radial, Linear }
public sealed record LocalMask
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Name { get; init; } = "Radial gradient";
    public MaskKind Kind { get; init; }
    public float X { get; init; } = .5f;
    public float Y { get; init; } = .5f;
    public float RadiusX { get; init; } = .25f;
    public float RadiusY { get; init; } = .25f;
    public float Feather { get; init; } = .75f;
    public float Exposure { get; init; } = .5f;
    public float Saturation { get; init; }
    public bool Inverted { get; init; }
    public LocalMask Normalize() => this with
    {
        X = Numeric.Unit(X), Y = Numeric.Unit(Y), RadiusX = Numeric.Clamp(RadiusX, .005f, 1), RadiusY = Numeric.Clamp(RadiusY, .005f, 1),
        Feather = Numeric.Clamp(Feather, .01f, 1), Exposure = Numeric.Clamp(Exposure, -5, 5), Saturation = DevelopSettings.Percent(Saturation)
    };
    public float Weight(float x, float y)
    {
        var m = Normalize(); float value;
        if (Kind == MaskKind.Radial)
        {
            var distance = MathF.Sqrt(MathF.Pow((x - m.X) / m.RadiusX, 2) + MathF.Pow((y - m.Y) / m.RadiusY, 2));
            value = 1 - Smooth(1 - m.Feather, 1, distance);
        }
        else value = Smooth(m.Y - m.RadiusY, m.Y + m.RadiusY, y);
        return Inverted ? 1 - value : value;
    }
    private static float Smooth(float a, float b, float v) { var t = Numeric.Unit((v - a) / (b - a)); return t * t * (3 - 2 * t); }
}

public sealed record CloneSpot(float X, float Y, float SourceX, float SourceY, float Radius = .03f)
{
    public CloneSpot Normalize() => new(Numeric.Unit(X), Numeric.Unit(Y), Numeric.Unit(SourceX), Numeric.Unit(SourceY), Numeric.Clamp(Radius, .002f, .25f));
}
