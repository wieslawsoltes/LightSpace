namespace LightSpace.Core;

public enum PhotoFlag { None, Pick, Reject }
public sealed record PhotoState
{
    public DevelopSettings Develop { get; init; } = new();
    public CropSettings Crop { get; init; } = new();
    public LocalMask[] Masks { get; init; } = [];
    public CloneSpot[] CloneSpots { get; init; } = [];
    public int Rating { get; init; }
    public PhotoFlag Flag { get; init; }
    public string Label { get; init; } = "";
    public string[] Keywords { get; init; } = [];
    public string Caption { get; init; } = "";
    public PhotoState Normalize() => this with
    {
        Develop = (Develop ?? new()).Normalize(), Crop = (Crop ?? new()).Normalize(),
        Masks = (Masks ?? []).Take(8).Select(m => m.Normalize()).ToArray(),
        CloneSpots = (CloneSpots ?? []).Take(64).Select(s => s.Normalize()).ToArray(), Rating = Math.Clamp(Rating, 0, 5),
        Keywords = (Keywords ?? []).Where(k => !string.IsNullOrWhiteSpace(k)).Select(k => k.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Take(100).ToArray()
    };
}

public sealed class PhotoDocument
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "Untitled";
    public byte[] Original { get; set; } = [];
    public int Width { get; set; }
    public int Height { get; set; }
    public DateTimeOffset ImportedAt { get; set; } = DateTimeOffset.UtcNow;
    public string Camera { get; set; } = "";
    public string Lens { get; set; } = "";
    public string ExposureInfo { get; set; } = "";
    public PhotoState State { get; set; } = new();
    public List<NamedVersion> Versions { get; set; } = [];
    [System.Text.Json.Serialization.JsonIgnore] public long Revision { get; set; }
}
public sealed record NamedVersion(string Name, PhotoState State, DateTimeOffset CreatedAt);
public sealed class Album
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "New album";
    public List<Guid> Photos { get; set; } = [];
}
public sealed class CatalogDocument
{
    public int SchemaVersion { get; set; } = 1;
    public Guid ActivePhoto { get; set; }
    public List<PhotoDocument> Photos { get; set; } = [];
    public List<Album> Albums { get; set; } = [];
}

public sealed record DevelopPreset(string Name, string Group, DevelopSettings Settings);
public static class BuiltInPresets
{
    public static IReadOnlyList<DevelopPreset> All { get; } = [
        new("Original", "Essentials", new()),
        new("Alpine light", "Landscape", new() { Exposure = .2f, Contrast = 14, Highlights = -32, Shadows = 24, Vibrance = 22, Clarity = 10 }),
        new("Golden hour", "Landscape", new() { Temperature = 28, Tint = 4, Highlights = -20, Shadows = 12, Vignette = -18, Vibrance = 16 }),
        new("Nordic blue", "Landscape", new() { Temperature = -18, Saturation = -8, Contrast = 18, Shadows = 16, Curve = new(.025f, .20f, .50f, .80f, .98f) }),
        new("Soft portrait", "Portrait", new() { Exposure = .25f, Contrast = -10, Highlights = -18, Texture = -22, Temperature = 7, Vibrance = 8 }),
        new("Editorial", "Creative", new() { Contrast = 24, Highlights = -24, Blacks = -10, Saturation = -20, Grain = 12, Curve = new(.045f, .21f, .5f, .78f, .98f) }),
        new("Matte film", "Creative", new() { Saturation = -18, Temperature = 10, Grain = 22, Curve = new(.075f, .27f, .52f, .76f, .94f) }),
        new("Silver / B&W", "Black & white", new() { Monochrome = true, Contrast = 25, Highlights = -25, Shadows = 16, Grain = 12 }),
        new("High key", "Black & white", new() { Monochrome = true, Exposure = .65f, Blacks = 18, Contrast = -12 }),
        new("Nightfall", "Creative", new() { Exposure = -.4f, Temperature = -20, Highlights = -28, Shadows = 18, Saturation = -10, Vignette = -32 })
    ];
}
