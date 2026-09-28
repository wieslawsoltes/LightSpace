namespace LightSpace.Core;

/// <summary>Allocation-free comparisons for normalized, copy-on-write photo snapshots.</summary>
public static class PhotoStateEquality
{
    public static bool Develop(DevelopSettings a, DevelopSettings b) => ReferenceEquals(a, b) ||
        a.Exposure == b.Exposure && a.Contrast == b.Contrast && a.Highlights == b.Highlights && a.Shadows == b.Shadows
        && a.Whites == b.Whites && a.Blacks == b.Blacks && a.Temperature == b.Temperature && a.Tint == b.Tint
        && a.Vibrance == b.Vibrance && a.Saturation == b.Saturation && a.Texture == b.Texture && a.Clarity == b.Clarity
        && a.Dehaze == b.Dehaze && a.Vignette == b.Vignette && a.Grain == b.Grain && a.Sharpening == b.Sharpening
        && a.NoiseReduction == b.NoiseReduction && a.Monochrome == b.Monochrome && a.Curve == b.Curve
        && a.Channels.ValueEquals(b.Channels) && a.Grading == b.Grading && a.Mixer.AsSpan().SequenceEqual(b.Mixer);

    public static bool Shader(PhotoState a, PhotoState b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (!Develop(a.Develop, b.Develop) || !a.CloneSpots.AsSpan().SequenceEqual(b.CloneSpots)) return false;
        if (ReferenceEquals(a.Masks, b.Masks)) return true;
        if (a.Masks.Length != b.Masks.Length) return false;
        for (var i = 0; i < a.Masks.Length; i++) if (!a.Masks[i].PixelEquals(b.Masks[i])) return false;
        return true;
    }
    public static bool Pixels(PhotoState a, PhotoState b) => a.Crop == b.Crop && Shader(a, b);
    public static bool All(PhotoState a, PhotoState b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a.Rating != b.Rating || a.Flag != b.Flag || a.Label != b.Label || a.Caption != b.Caption
            || !a.Keywords.AsSpan().SequenceEqual(b.Keywords) || !Pixels(a, b)) return false;
        for (var i = 0; i < a.Masks.Length; i++)
        {
            var x = a.Masks[i]; var y = b.Masks[i];
            if (x.Id != y.Id || x.Name != y.Name || x.Strokes.Length != y.Strokes.Length) return false;
            for (var j = 0; j < x.Strokes.Length; j++) if (x.Strokes[j].Id != y.Strokes[j].Id) return false;
        }
        return true;
    }
}
