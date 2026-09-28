namespace LightSpace.Core;

public enum CurveChannel { Master, Red, Green, Blue }
public enum CurveInterpolation { Linear, Smooth }
public readonly record struct CurvePoint(float X, float Y);

/// <summary>Copy-on-write control points in normalized, display-encoded RGB space.</summary>
public sealed record PointCurve
{
    public const int MaximumPoints = 32;
    public const float MinimumSeparation = .0001f;
    public CurvePoint[] Points { get; init; } = [new(0, 0), new(1, 1)];
    public CurveInterpolation Interpolation { get; init; } = CurveInterpolation.Smooth;
    public bool IsIdentity => Points.All(point => point.X == point.Y);

    public PointCurve Normalize()
    {
        var valid = Points is { Length: >= 2 and <= MaximumPoints } && Enum.IsDefined(Interpolation);
        if (valid)
        {
            valid = Points![0].X == 0 && Points[^1].X == 1;
            for (var i = 0; valid && i < Points.Length; i++)
                valid = float.IsFinite(Points[i].X) && float.IsFinite(Points[i].Y)
                    && Points[i].X is >= 0 and <= 1 && Points[i].Y is >= 0 and <= 1
                    && (i == 0 || Points[i].X - Points[i - 1].X >= MinimumSeparation);
        }
        if (valid) return this;
        var points = (Points ?? []).Where(p => float.IsFinite(p.X) && float.IsFinite(p.Y))
            .Select(p => new CurvePoint(Numeric.Unit(p.X), Numeric.Unit(p.Y))).OrderBy(p => p.X).ToArray();
        var normalized = new List<CurvePoint> { new(0, points.FirstOrDefault(p => p.X == 0).Y) };
        foreach (var point in points)
        {
            if (point.X <= MinimumSeparation || point.X >= 1 - MinimumSeparation || normalized.Count >= MaximumPoints - 1) continue;
            if (point.X - normalized[^1].X >= MinimumSeparation) normalized.Add(point);
        }
        normalized.Add(new(1, points.LastOrDefault(p => p.X == 1, new(1, 1)).Y));
        return this with { Points = normalized.ToArray(), Interpolation = Enum.IsDefined(Interpolation) ? Interpolation : CurveInterpolation.Smooth };
    }
    public bool ValueEquals(PointCurve other) => ReferenceEquals(this, other) ||
        Interpolation == other.Interpolation && Points.AsSpan().SequenceEqual(other.Points);
    public CompiledPointCurve Compile() => new(Normalize());
}

/// <summary>Shape-preserving cubic Hermite interpolation, compiled once per point edit.</summary>
public sealed class CompiledPointCurve
{
    private readonly CurvePoint[] _points;
    private readonly float[] _slopes;
    private readonly bool _linear;
    internal CompiledPointCurve(PointCurve curve)
    {
        _points = (CurvePoint[])curve.Points.Clone(); _slopes = new float[_points.Length];
        _linear = curve.Interpolation == CurveInterpolation.Linear;
        var h = new float[_points.Length - 1]; var d = new float[h.Length];
        for (var i = 0; i < h.Length; i++) { h[i] = _points[i + 1].X - _points[i].X; d[i] = (_points[i + 1].Y - _points[i].Y) / h[i]; }
        if (d.Length == 1) { _slopes[0] = _slopes[1] = d[0]; return; }
        for (var i = 1; i < _slopes.Length - 1; i++)
        {
            if (d[i - 1] * d[i] <= 0) continue;
            var w1 = 2 * h[i] + h[i - 1]; var w2 = h[i] + 2 * h[i - 1];
            _slopes[i] = (w1 + w2) / (w1 / d[i - 1] + w2 / d[i]);
        }
        _slopes[0] = Endpoint(h[0], h[1], d[0], d[1]);
        _slopes[^1] = Endpoint(h[^1], h[^2], d[^1], d[^2]);
    }
    private static float Endpoint(float h0, float h1, float d0, float d1)
    {
        var slope = ((2 * h0 + h1) * d0 - h0 * d1) / (h0 + h1);
        if (slope * d0 <= 0) return 0;
        return d0 * d1 < 0 && MathF.Abs(slope) > 3 * MathF.Abs(d0) ? 3 * d0 : slope;
    }
    public float Evaluate(float input)
    {
        var x = Numeric.Unit(input); var left = 0; var right = _points.Length - 1;
        while (right - left > 1) { var mid = (left + right) / 2; if (x < _points[mid].X) right = mid; else left = mid; }
        var a = _points[left]; var b = _points[right]; var h = b.X - a.X; var t = (x - a.X) / h;
        if (_linear) return a.Y + (b.Y - a.Y) * t;
        var t2 = t * t; var t3 = t2 * t;
        var value = (2 * t3 - 3 * t2 + 1) * a.Y + (t3 - 2 * t2 + t) * h * _slopes[left]
            + (-2 * t3 + 3 * t2) * b.Y + (t3 - t2) * h * _slopes[right];
        return Math.Clamp(value, MathF.Min(a.Y, b.Y), MathF.Max(a.Y, b.Y));
    }
}

public sealed record ChannelCurves
{
    public PointCurve Master { get; init; } = new();
    public PointCurve Red { get; init; } = new();
    public PointCurve Green { get; init; } = new();
    public PointCurve Blue { get; init; } = new();
    public bool IsIdentity => Master.IsIdentity && Red.IsIdentity && Green.IsIdentity && Blue.IsIdentity;
    public PointCurve Get(CurveChannel channel) => channel switch { CurveChannel.Red => Red, CurveChannel.Green => Green, CurveChannel.Blue => Blue, _ => Master };
    public ChannelCurves Set(CurveChannel channel, PointCurve value) => channel switch
    { CurveChannel.Red => this with { Red = value.Normalize() }, CurveChannel.Green => this with { Green = value.Normalize() }, CurveChannel.Blue => this with { Blue = value.Normalize() }, _ => this with { Master = value.Normalize() } };
    public ChannelCurves Normalize()
    {
        var result = this with { Master = (Master ?? new()).Normalize(), Red = (Red ?? new()).Normalize(), Green = (Green ?? new()).Normalize(), Blue = (Blue ?? new()).Normalize() };
        return result == this ? this : result;
    }
    public bool ValueEquals(ChannelCurves other) => ReferenceEquals(this, other) || Master.ValueEquals(other.Master)
        && Red.ValueEquals(other.Red) && Green.ValueEquals(other.Green) && Blue.ValueEquals(other.Blue);
}
