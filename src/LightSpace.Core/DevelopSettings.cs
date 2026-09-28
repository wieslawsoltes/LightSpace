namespace LightSpace.Core;

public sealed record DevelopSettings
{
    public float Exposure { get; init; }
    public float Contrast { get; init; }
    public float Highlights { get; init; }
    public float Shadows { get; init; }
    public float Whites { get; init; }
    public float Blacks { get; init; }
    public float Temperature { get; init; }
    public float Tint { get; init; }
    public float Vibrance { get; init; }
    public float Saturation { get; init; }
    public float Texture { get; init; }
    public float Clarity { get; init; }
    public float Dehaze { get; init; }
    public float Vignette { get; init; }
    public float Grain { get; init; }
    public float Sharpening { get; init; }
    public float NoiseReduction { get; init; }
    public bool Monochrome { get; init; }
    public ToneCurve Curve { get; init; } = new();
    public ChannelCurves Channels { get; init; } = new();
    public ColorBand[] Mixer { get; init; } = Enumerable.Range(0, 8).Select(_ => new ColorBand()).ToArray();
    public ColorGradingSettings Grading { get; init; } = new();
    public static DevelopSettings Default { get; } = new();

    public DevelopSettings Normalize()
    {
        ColorBand[]? mixer = Mixer;
        if (mixer is null || mixer.Length != 8)
            mixer = Enumerable.Range(0, 8).Select(i => Mixer is not null && i < Mixer.Length ? Mixer[i] ?? new() : new ColorBand()).ToArray();
        for (var i = 0; i < 8; i++)
        {
            var band = (mixer[i] ?? new()).Normalize();
            if (ReferenceEquals(band, mixer[i])) continue;
            if (ReferenceEquals(mixer, Mixer)) mixer = (ColorBand[])mixer.Clone();
            mixer[i] = band;
        }
        var result = this with
        {
            Exposure = Numeric.Clamp(Exposure, -5, 5), Contrast = Percent(Contrast), Highlights = Percent(Highlights),
            Shadows = Percent(Shadows), Whites = Percent(Whites), Blacks = Percent(Blacks), Temperature = Percent(Temperature),
            Tint = Percent(Tint), Vibrance = Percent(Vibrance), Saturation = Percent(Saturation), Texture = Percent(Texture),
            Clarity = Percent(Clarity), Dehaze = Percent(Dehaze), Vignette = Percent(Vignette),
            Grain = Numeric.Clamp(Grain, 0, 100), Sharpening = Numeric.Clamp(Sharpening, 0, 100), NoiseReduction = Numeric.Clamp(NoiseReduction, 0, 100),
            Curve = (Curve ?? new()).Normalize(), Channels = (Channels ?? new()).Normalize(), Mixer = mixer, Grading = (Grading ?? new()).Normalize()
        };
        return result == this ? this : result;
    }
    public static float Percent(float value) => Numeric.Clamp(value, -100, 100);
    public float Get(string key) => key switch
    {
        nameof(Exposure) => Exposure, nameof(Contrast) => Contrast, nameof(Highlights) => Highlights,
        nameof(Shadows) => Shadows, nameof(Whites) => Whites, nameof(Blacks) => Blacks,
        nameof(Temperature) => Temperature, nameof(Tint) => Tint, nameof(Vibrance) => Vibrance,
        nameof(Saturation) => Saturation, nameof(Texture) => Texture, nameof(Clarity) => Clarity,
        nameof(Dehaze) => Dehaze, nameof(Vignette) => Vignette, nameof(Grain) => Grain,
        nameof(Sharpening) => Sharpening, nameof(NoiseReduction) => NoiseReduction,
        _ => throw new ArgumentException($"Unknown adjustment: {key}", nameof(key))
    };
    public DevelopSettings Set(string key, float value) => (key switch
    {
        nameof(Exposure) => this with { Exposure = value }, nameof(Contrast) => this with { Contrast = value },
        nameof(Highlights) => this with { Highlights = value }, nameof(Shadows) => this with { Shadows = value },
        nameof(Whites) => this with { Whites = value }, nameof(Blacks) => this with { Blacks = value },
        nameof(Temperature) => this with { Temperature = value }, nameof(Tint) => this with { Tint = value },
        nameof(Vibrance) => this with { Vibrance = value }, nameof(Saturation) => this with { Saturation = value },
        nameof(Texture) => this with { Texture = value }, nameof(Clarity) => this with { Clarity = value },
        nameof(Dehaze) => this with { Dehaze = value }, nameof(Vignette) => this with { Vignette = value },
        nameof(Grain) => this with { Grain = value }, nameof(Sharpening) => this with { Sharpening = value },
        nameof(NoiseReduction) => this with { NoiseReduction = value },
        _ => throw new ArgumentException($"Unknown adjustment: {key}", nameof(key))
    }).Normalize();
}

public sealed record ColorBand(float Hue = 0, float Saturation = 0, float Luminance = 0)
{
    public ColorBand Normalize()
    {
        var h = DevelopSettings.Percent(Hue); var s = DevelopSettings.Percent(Saturation); var l = DevelopSettings.Percent(Luminance);
        return h == Hue && s == Saturation && l == Luminance ? this : new(h, s, l);
    }
}
public sealed record ToneCurve(float Black = 0, float Shadow = .25f, float Mid = .5f, float Light = .75f, float White = 1)
{
    public bool IsIdentity => Black == 0 && Shadow == .25f && Mid == .5f && Light == .75f && White == 1;
    public ToneCurve Normalize()
    {
        var b = Numeric.Unit(Black); var s = Numeric.Unit(Shadow); var m = Numeric.Unit(Mid); var l = Numeric.Unit(Light); var w = Numeric.Unit(White);
        return b == Black && s == Shadow && m == Mid && l == Light && w == White ? this : new(b, s, m, l, w);
    }
    public float Evaluate(float x)
    {
        x = Numeric.Unit(x) * 4;
        var (a, b, t) = x switch
        {
            < 1 => (Black, Shadow, x), < 2 => (Shadow, Mid, x - 1),
            < 3 => (Mid, Light, x - 2), _ => (Light, White, x - 3)
        };
        return a + (b - a) * t;
    }
}
public static class Numeric
{
    public static float Clamp(float value, float min, float max) => Math.Clamp(float.IsFinite(value) ? value : 0, min, max);
    public static float Unit(float value) => Clamp(value, 0, 1);
    public static float Angle(float value) => float.IsFinite(value) ? (value % 360 + 360) % 360 : 0;
    public static float Smooth(float a, float b, float value)
    {
        var t = Unit((value - a) / MathF.Max(.000001f, b - a)); return t * t * (3 - 2 * t);
    }
}
