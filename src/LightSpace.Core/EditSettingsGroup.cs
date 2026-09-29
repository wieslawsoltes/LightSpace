namespace LightSpace.Core;

/// <summary>Independent development groups. Catalog metadata is deliberately never transferred.</summary>
[Flags]
public enum EditSettingsGroup
{
    None = 0,
    Light = 1 << 0,
    WhiteBalance = 1 << 1,
    Color = 1 << 2,
    Curves = 1 << 3,
    ColorMixer = 1 << 4,
    ColorGrading = 1 << 5,
    Effects = 1 << 6,
    Detail = 1 << 7,
    Optics = 1 << 8,
    Geometry = 1 << 9,
    Crop = 1 << 10,
    Masks = 1 << 11,
    CloneSpots = 1 << 12,
    All = (1 << 13) - 1,
    Global = Light | WhiteBalance | Color | Curves | ColorMixer | ColorGrading | Effects | Detail | Optics
}
