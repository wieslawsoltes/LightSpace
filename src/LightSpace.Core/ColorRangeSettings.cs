using System.Numerics;
namespace LightSpace.Core;

/// <summary>Unpremultiplied, transfer-encoded sRGB sampled at an orientation-normalized source position.</summary>
public sealed record ColorSample(float Red, float Green, float Blue, float X = .5f, float Y = .5f)
{
    public ColorSample Normalize()
    {
        var r = Numeric.Unit(Red); var g = Numeric.Unit(Green); var b = Numeric.Unit(Blue);
        var x = Numeric.Unit(X); var y = Numeric.Unit(Y);
        return r == Red && g == Green && b == Blue && x == X && y == Y ? this : new(r, g, b, x, y);
    }
    public Vector3 ToOklab() => ColorRangeSettings.ToOklab(Red, Green, Blue);
    public float Luminance => .2126f * Red + .7152f * Green + .0722f * Blue;
}

/// <summary>A union of up to five perceptual color neighborhoods, intersected with a mask before inversion.</summary>
public sealed record ColorRangeSettings
{
    public const int MaximumSamples = 5;
    public bool Enabled { get; init; }
    public ColorSample[] Samples { get; init; } = [];
    public float Tolerance { get; init; } = .08f;
    public float Smoothness { get; init; } = .06f;

    public ColorRangeSettings Normalize()
    {
        var input = Samples ?? []; var count = Math.Min(input.Length, MaximumSamples);
        var valid = input.Length <= MaximumSamples;
        for (var i = 0; i < count && valid; i++) valid = input[i] is { } sample && ReferenceEquals(sample, sample.Normalize());
        var samples = valid ? input : input.Where(s => s is not null).Take(MaximumSamples).Select(s => s.Normalize()).ToArray();
        var tolerance = Numeric.Clamp(Tolerance, 0, 1); var smoothness = Numeric.Clamp(Smoothness, .001f, 1);
        return ReferenceEquals(samples, Samples) && tolerance == Tolerance && smoothness == Smoothness ? this
            : this with { Samples = samples, Tolerance = tolerance, Smoothness = smoothness };
    }
    public float Weight(ColorSample source, bool forceEnabled = false)
    {
        var settings = Normalize();
        if (!settings.Enabled && !forceEnabled) return 1;
        if (settings.Samples.Length == 0) return 0;
        var lab = source.ToOklab(); var distance = float.PositiveInfinity;
        foreach (var sample in settings.Samples) distance = MathF.Min(distance, Vector3.Distance(lab, sample.ToOklab()));
        return 1 - Numeric.Smooth(settings.Tolerance, settings.Tolerance + settings.Smoothness, distance);
    }
    public bool PixelEquals(ColorRangeSettings other)
    {
        if (ReferenceEquals(this, other)) return true;
        if (Enabled != other.Enabled || Tolerance != other.Tolerance || Smoothness != other.Smoothness || Samples.Length != other.Samples.Length) return false;
        for (var i = 0; i < Samples.Length; i++)
            if (Samples[i].Red != other.Samples[i].Red || Samples[i].Green != other.Samples[i].Green || Samples[i].Blue != other.Samples[i].Blue) return false;
        return true;
    }
    /// <summary>Oklab from linear sRGB, following Björn Ottosson's published matrices (see third-party notices).</summary>
    public static Vector3 ToOklab(float r, float g, float b)
    {
        static float Linear(float v) { v = Numeric.Unit(v); return v <= .04045f ? v / 12.92f : MathF.Pow((v + .055f) / 1.055f, 2.4f); }
        r = Linear(r); g = Linear(g); b = Linear(b);
        var l = MathF.Cbrt(.4122214708f * r + .5363325363f * g + .0514459929f * b);
        var m = MathF.Cbrt(.2119034982f * r + .6806995451f * g + .1073969566f * b);
        var s = MathF.Cbrt(.0883024619f * r + .2817188376f * g + .6299787005f * b);
        return new(.2104542553f * l + .7936177850f * m - .0040720468f * s,
            1.9779984951f * l - 2.4285922050f * m + .4505937099f * s,
            .0259040371f * l + .7827717662f * m - .8086757660f * s);
    }
}
