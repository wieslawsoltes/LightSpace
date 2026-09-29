namespace LightSpace.Core;

/// <summary>Manual, original-model lens corrections; not a camera/lens calibration profile.</summary>
public sealed record LensCorrectionSettings(float Distortion = 0, float Vignetting = 0, float RedCyan = 0, float BlueYellow = 0)
{
    public bool IsNeutral => Distortion == 0 && Vignetting == 0 && RedCyan == 0 && BlueYellow == 0;
    public LensCorrectionSettings Normalize()
    {
        var d = DevelopSettings.Percent(Distortion); var v = DevelopSettings.Percent(Vignetting);
        var r = DevelopSettings.Percent(RedCyan); var b = DevelopSettings.Percent(BlueYellow);
        return d == Distortion && v == Vignetting && r == RedCyan && b == BlueYellow ? this : new(d, v, r, b);
    }
    public float Get(string name) => name switch
    {
        nameof(Distortion) => Distortion, nameof(Vignetting) => Vignetting,
        nameof(RedCyan) => RedCyan, nameof(BlueYellow) => BlueYellow,
        _ => throw new ArgumentException("Unknown optical adjustment: " + name, nameof(name))
    };
    public LensCorrectionSettings Set(string name, float value) => (name switch
    {
        nameof(Distortion) => this with { Distortion = value }, nameof(Vignetting) => this with { Vignetting = value },
        nameof(RedCyan) => this with { RedCyan = value }, nameof(BlueYellow) => this with { BlueYellow = value },
        _ => throw new ArgumentException("Unknown optical adjustment: " + name, nameof(name))
    }).Normalize();
}

/// <summary>CPU counterpart of the optical sampling transform. No image readback is needed for hit testing.</summary>
public static class LensMapping
{
    public static PointD ToSource(PointD corrected, LensCorrectionSettings optics, double aspect)
    {
        var x = (corrected.X - .5) * aspect; var y = corrected.Y - .5;
        var radiusSquared = 4 * (x * x + y * y) / (aspect * aspect + 1);
        var denominator = 1 - optics.Normalize().Distortion * .002 * radiusSquared;
        if (!double.IsFinite(denominator) || denominator <= .000001) return new(double.NaN, double.NaN);
        return new(.5 + x / denominator / aspect, .5 + y / denominator);
    }
    public static PointD FromSource(PointD source, LensCorrectionSettings optics, double aspect)
    {
        var x = (source.X - .5) * aspect; var y = source.Y - .5;
        var radiusSquared = 4 * (x * x + y * y) / (aspect * aspect + 1);
        var discriminant = 1 + 4 * optics.Normalize().Distortion * .002 * radiusSquared;
        if (!double.IsFinite(discriminant) || discriminant <= 0) return new(double.NaN, double.NaN);
        var factor = 2 / (1 + Math.Sqrt(discriminant));
        return new(.5 + x * factor / aspect, .5 + y * factor);
    }
    /// <summary>A conservative centered rectangle whose optical/channel samples remain inside the source.</summary>
    public static double SafeExtent(LensCorrectionSettings optics)
    {
        var o = optics.Normalize();
        return (1 - Math.Max(0, o.Distortion * .002)) / (1 + Math.Max(Math.Abs(o.RedCyan), Math.Abs(o.BlueYellow)) * .0001);
    }
}
