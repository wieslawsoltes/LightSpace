namespace LightSpace.Core;

/// <summary>
/// A captured, selectively applicable look. Normalized snapshot arrays remain
/// read-only by contract, like the editor's undo snapshots. No source bytes or
/// catalog metadata are copied or serialized during an application.
/// </summary>
public sealed class EditSettingsTransfer
{
    public PhotoState Source { get; }
    public EditSettingsGroup Groups { get; }

    public EditSettingsTransfer(PhotoState source, EditSettingsGroup groups)
    {
        ArgumentNullException.ThrowIfNull(source);
        if ((groups & ~EditSettingsGroup.All) != 0) throw new ArgumentOutOfRangeException(nameof(groups));
        Source = source.Normalize(); Groups = groups;
    }

    public PhotoState Apply(PhotoState target)
    {
        ArgumentNullException.ThrowIfNull(target);
        if (Groups == EditSettingsGroup.None) return target;
        var source = Source.Develop; var develop = target.Develop;
        bool Has(EditSettingsGroup group) => (Groups & group) != 0;
        if (Has(EditSettingsGroup.Light)) develop = develop with
        { Exposure = source.Exposure, Contrast = source.Contrast, Highlights = source.Highlights,
            Shadows = source.Shadows, Whites = source.Whites, Blacks = source.Blacks };
        if (Has(EditSettingsGroup.WhiteBalance)) develop = develop with { Temperature = source.Temperature, Tint = source.Tint };
        if (Has(EditSettingsGroup.Color)) develop = develop with { Vibrance = source.Vibrance, Saturation = source.Saturation, Monochrome = source.Monochrome };
        if (Has(EditSettingsGroup.Curves)) develop = develop with { Curve = source.Curve, Channels = source.Channels };
        if (Has(EditSettingsGroup.ColorMixer)) develop = develop with { Mixer = source.Mixer };
        if (Has(EditSettingsGroup.ColorGrading)) develop = develop with { Grading = source.Grading };
        if (Has(EditSettingsGroup.Effects)) develop = develop with
        { Texture = source.Texture, Clarity = source.Clarity, Dehaze = source.Dehaze, Vignette = source.Vignette, Grain = source.Grain };
        if (Has(EditSettingsGroup.Detail)) develop = develop with { Sharpening = source.Sharpening, NoiseReduction = source.NoiseReduction };
        var result = target with
        {
            Develop = develop,
            Optics = Has(EditSettingsGroup.Optics) ? Source.Optics : target.Optics,
            Geometry = Has(EditSettingsGroup.Geometry) ? Source.Geometry : target.Geometry,
            Crop = Has(EditSettingsGroup.Crop) ? Source.Crop : target.Crop,
            Masks = Has(EditSettingsGroup.Masks) ? Source.Masks : target.Masks,
            CloneSpots = Has(EditSettingsGroup.CloneSpots) ? Source.CloneSpots : target.CloneSpots
        };
        return PhotoStateEquality.All(result, target) ? target : result.Normalize();
    }
}
