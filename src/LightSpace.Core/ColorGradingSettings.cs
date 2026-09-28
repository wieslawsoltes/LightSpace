using System.Numerics;
namespace LightSpace.Core;

public enum GradingRange { Shadows, Midtones, Highlights, Global }

/// <summary>Hue in degrees, saturation in percent, and luminance in relative stops.</summary>
public sealed record GradingTone(float Hue = 0, float Saturation = 0, float Luminance = 0)
{
    public bool IsNeutral => Saturation == 0 && Luminance == 0;
    public GradingTone Normalize()
    {
        var hue = Numeric.Angle(Hue); var saturation = Numeric.Clamp(Saturation, 0, 100);
        var luminance = Numeric.Clamp(Luminance, -100, 100);
        return hue == Hue && saturation == Saturation && luminance == Luminance
            ? this : new(hue, saturation, luminance);
    }
    /// <summary>Zero-luminance tint vector in the linear sRGB working space.</summary>
    public Vector3 TintVector()
    {
        var tone = Normalize(); var h = tone.Hue / 60; var x = 1 - MathF.Abs(h % 2 - 1);
        var rgb = (int)h switch
        {
            0 => new Vector3(1, x, 0), 1 => new(x, 1, 0), 2 => new(0, 1, x),
            3 => new(0, x, 1), 4 => new(x, 0, 1), _ => new(1, 0, x)
        };
        return (rgb - new Vector3(Vector3.Dot(rgb, new(.2126f, .7152f, .0722f)))) * (tone.Saturation * .0012f);
    }
}

/// <summary>Original split-toning model; neutral by default. Not Adobe's processing algorithm.</summary>
public sealed record ColorGradingSettings
{
    public GradingTone Shadows { get; init; } = new();
    public GradingTone Midtones { get; init; } = new();
    public GradingTone Highlights { get; init; } = new();
    public GradingTone Global { get; init; } = new();
    public float Blending { get; init; } = 50;
    public float Balance { get; init; }
    public bool IsNeutral => Shadows.IsNeutral && Midtones.IsNeutral && Highlights.IsNeutral && Global.IsNeutral;
    public ColorGradingSettings Normalize()
    {
        var s = (Shadows ?? new()).Normalize(); var m = (Midtones ?? new()).Normalize();
        var h = (Highlights ?? new()).Normalize(); var g = (Global ?? new()).Normalize();
        var blend = Numeric.Clamp(Blending, 0, 100); var balance = DevelopSettings.Percent(Balance);
        return ReferenceEquals(s, Shadows) && ReferenceEquals(m, Midtones) && ReferenceEquals(h, Highlights)
            && ReferenceEquals(g, Global) && blend == Blending && balance == Balance ? this
            : this with { Shadows = s, Midtones = m, Highlights = h, Global = g, Blending = blend, Balance = balance };
    }
    public GradingTone Get(GradingRange range) => range switch
    {
        GradingRange.Shadows => Shadows, GradingRange.Midtones => Midtones,
        GradingRange.Highlights => Highlights, _ => Global
    };
    public ColorGradingSettings Set(GradingRange range, GradingTone tone) => range switch
    {
        GradingRange.Shadows => this with { Shadows = tone.Normalize() },
        GradingRange.Midtones => this with { Midtones = tone.Normalize() },
        GradingRange.Highlights => this with { Highlights = tone.Normalize() },
        _ => this with { Global = tone.Normalize() }
    };
    public Vector3 Weights(float luminance)
    {
        var l = Numeric.Unit(luminance - Balance * .0025f);
        var width = .08f + Blending * .0032f;
        var shadow = 1 - Numeric.Smooth(.32f - width, .32f + width, l);
        var highlight = Numeric.Smooth(.68f - width, .68f + width, l);
        var mid = MathF.Max(0, 1 - shadow - highlight);
        return new Vector3(shadow, mid, highlight) / MathF.Max(.00001f, shadow + mid + highlight);
    }
}
