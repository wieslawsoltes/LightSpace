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
        var right = Numeric.Clamp(Right, left + .01f, 1); var bottom = Numeric.Clamp(Bottom, top + .01f, 1);
        var q = ((QuarterTurns % 4) + 4) % 4;
        return left == Left && top == Top && right == Right && bottom == Bottom && q == QuarterTurns ? this
            : this with { Left = left, Top = top, Right = right, Bottom = bottom, QuarterTurns = q };
    }
    public (int Width, int Height) OutputSize(int width, int height)
    {
        var c = Normalize(); var w = Math.Max(1, (int)Math.Round(width * c.Width)); var h = Math.Max(1, (int)Math.Round(height * c.Height));
        return c.QuarterTurns % 2 == 0 ? (w, h) : (h, w);
    }
}

public enum MaskKind { Radial, Linear, LuminanceRange, Brush }
public sealed record LocalMask
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Name { get; init; } = "Radial gradient";
    public MaskKind Kind { get; init; }
    public float X { get; init; } = .5f;
    public float Y { get; init; } = .5f;
    public float RadiusX { get; init; } = .25f;
    public float RadiusY { get; init; } = .25f;
    public float Angle { get; init; }
    public float Feather { get; init; } = .75f;
    public float Exposure { get; init; } = .5f;
    public float Saturation { get; init; }
    public float Contrast { get; init; }
    public float Temperature { get; init; }
    public float Tint { get; init; }
    public bool Inverted { get; init; }
    public bool Enabled { get; init; } = true;
    public float Opacity { get; init; } = 1;
    public bool RangeEnabled { get; init; }
    public float RangeMin { get; init; }
    public float RangeMax { get; init; } = 1;
    public float RangeSmoothness { get; init; } = .1f;
    public BrushStroke[] Strokes { get; init; } = [];

    public LocalMask Normalize()
    {
        var low = MathF.Min(Numeric.Unit(RangeMin), Numeric.Unit(RangeMax));
        var high = MathF.Max(Numeric.Unit(RangeMin), Numeric.Unit(RangeMax));
        var normalized = this with
        {
            Name = string.IsNullOrWhiteSpace(Name) ? "Mask" : Name[..Math.Min(Name.Length, 100)],
            Kind = Enum.IsDefined(Kind) ? Kind : MaskKind.Radial,
            X = Numeric.Unit(X), Y = Numeric.Unit(Y), RadiusX = Numeric.Clamp(RadiusX, .005f, 100), RadiusY = Numeric.Clamp(RadiusY, .005f, 100),
            Angle = Numeric.Angle(Angle), Feather = Numeric.Clamp(Feather, .01f, 1),
            Exposure = Numeric.Clamp(Exposure, -5, 5), Saturation = DevelopSettings.Percent(Saturation),
            Contrast = DevelopSettings.Percent(Contrast), Temperature = DevelopSettings.Percent(Temperature), Tint = DevelopSettings.Percent(Tint),
            Opacity = Numeric.Unit(Opacity), RangeMin = low, RangeMax = high, RangeSmoothness = Numeric.Clamp(RangeSmoothness, .001f, 1),
            Strokes = BrushStroke.NormalizeAll(Strokes)
        };
        return normalized == this ? this : normalized;
    }
    /// <summary>Coverage in source coordinates. Brushing modifies spatial coverage before range restriction and inversion.</summary>
    public float Weight(float x, float y, float sourceLuminance = .5f, float aspect = 1)
    {
        var m = Normalize(); if (!m.Enabled) return 0;
        aspect = MathF.Max(.0001f, aspect);
        var a = m.Angle * MathF.PI / 180; var cos = MathF.Cos(a); var sin = MathF.Sin(a);
        var dx = (x - m.X) * aspect; var dy = y - m.Y;
        var qx = cos * dx + sin * dy; var qy = -sin * dx + cos * dy;
        var value = m.Kind == MaskKind.Brush ? 0f : 1f;
        if (m.Kind == MaskKind.Radial)
        {
            var rx = qx / (m.RadiusX * aspect); var ry = qy / m.RadiusY;
            value = 1 - Numeric.Smooth(1 - m.Feather, 1, MathF.Sqrt(rx * rx + ry * ry));
        }
        else if (m.Kind == MaskKind.Linear) value = Numeric.Smooth(-m.RadiusY, m.RadiusY, qy);
        if (m.Strokes.Length > 0) value = BrushStroke.Apply(value, m.Strokes, x, y, aspect);
        if (m.RangeEnabled || m.Kind == MaskKind.LuminanceRange)
        {
            var lower = m.RangeMin <= 0 ? 1 : Numeric.Smooth(m.RangeMin - m.RangeSmoothness, m.RangeMin, sourceLuminance);
            var upper = m.RangeMax >= 1 ? 1 : 1 - Numeric.Smooth(m.RangeMax, m.RangeMax + m.RangeSmoothness, sourceLuminance);
            value *= lower * upper;
        }
        return (m.Inverted ? 1 - value : value) * m.Opacity;
    }
    public PointD LocalToSource(float dx, float dy, float aspect)
    {
        var a = Angle * Math.PI / 180; var c = Math.Cos(a); var s = Math.Sin(a);
        return new(X + (c * dx - s * dy) / Math.Max(.0001, aspect), Y + s * dx + c * dy);
    }
    public bool PixelEquals(LocalMask other) => ReferenceEquals(this, other) ||
        Kind == other.Kind && X == other.X && Y == other.Y && RadiusX == other.RadiusX && RadiusY == other.RadiusY
        && Angle == other.Angle && Feather == other.Feather && Exposure == other.Exposure && Saturation == other.Saturation
        && Contrast == other.Contrast && Temperature == other.Temperature && Tint == other.Tint && Inverted == other.Inverted
        && Enabled == other.Enabled && Opacity == other.Opacity && RangeEnabled == other.RangeEnabled
        && RangeMin == other.RangeMin && RangeMax == other.RangeMax && RangeSmoothness == other.RangeSmoothness
        && BrushStroke.SequenceEquals(Strokes, other.Strokes);
}

public sealed record CloneSpot(float X, float Y, float SourceX, float SourceY, float Radius = .03f)
{
    public CloneSpot Normalize()
    {
        var x = Numeric.Unit(X); var y = Numeric.Unit(Y); var sx = Numeric.Unit(SourceX); var sy = Numeric.Unit(SourceY); var r = Numeric.Clamp(Radius, .002f, .25f);
        return x == X && y == Y && sx == SourceX && sy == SourceY && r == Radius ? this : new(x, y, sx, sy, r);
    }
}
