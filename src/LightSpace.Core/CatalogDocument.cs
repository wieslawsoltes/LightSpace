namespace LightSpace.Core;

public enum PhotoFlag { None, Pick, Reject }

public sealed record PhotoState
{
    public DevelopSettings Develop { get; init; } = new();
    public CropSettings Crop { get; init; } = new();
    public GeometrySettings Geometry { get; init; } = new();
    public LensCorrectionSettings Optics { get; init; } = new();
    public LocalMask[] Masks { get; init; } = [];
    public CloneSpot[] CloneSpots { get; init; } = [];
    public int Rating { get; init; }
    public PhotoFlag Flag { get; init; }
    public string Label { get; init; } = "";
    public string[] Keywords { get; init; } = [];
    public string Caption { get; init; } = "";
    public PhotoState Normalize()
    {
        var normalized = this with
        {
            Develop = (Develop ?? new()).Normalize(), Crop = (Crop ?? new()).Normalize(),
            Geometry = (Geometry ?? new()).Normalize(), Optics = (Optics ?? new()).Normalize(),
            Masks = NormalizeItems(Masks, 8, static m => m.Normalize()),
            CloneSpots = NormalizeItems(CloneSpots, 32, static s => s.Normalize()),
            Rating = Math.Clamp(Rating, 0, 5), Flag = Enum.IsDefined(Flag) ? Flag : PhotoFlag.None,
            Label = (Label ?? "")[..Math.Min(Label?.Length ?? 0, 64)],
            Caption = (Caption ?? "")[..Math.Min(Caption?.Length ?? 0, 16384)], Keywords = NormalizeKeywords(Keywords)
        };
        return normalized == this ? this : normalized;
    }
    private static T[] NormalizeItems<T>(T[]? items, int max, Func<T, T> normalize) where T : class
    {
        if (items is null) return [];
        T[]? copy = null;
        if (items.Length > max || Array.IndexOf(items, null) >= 0)
            return items.Where(item => item is not null).Take(max).Select(normalize).ToArray();
        for (var i = 0; i < items.Length; i++)
        {
            var item = normalize(items[i]); if (ReferenceEquals(item, items[i])) continue;
            copy ??= (T[])items.Clone(); copy[i] = item;
        }
        return copy ?? items;
    }
    private static string[] NormalizeKeywords(string[]? keywords)
    {
        if (keywords is null) return [];
        var valid = keywords.Length <= 100;
        for (var i = 0; valid && i < keywords.Length; i++)
        {
            var word = keywords[i];
            if (string.IsNullOrWhiteSpace(word) || word.Length > 200 || word != word.Trim()) { valid = false; break; }
            for (var j = 0; j < i; j++) if (StringComparer.OrdinalIgnoreCase.Equals(word, keywords[j])) { valid = false; break; }
        }
        if (valid) return keywords;
        return keywords.Where(word => !string.IsNullOrWhiteSpace(word)).Select(word => word.Trim())
            .Select(word => word[..Math.Min(word.Length, 200)]).Distinct(StringComparer.OrdinalIgnoreCase).Take(100).ToArray();
    }
}
public sealed class PhotoDocument
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "Untitled";
    public Guid? MasterPhotoId { get; set; }
    public string CopyName { get; set; } = "";
    [System.Text.Json.Serialization.JsonIgnore] public bool IsVirtualCopy => MasterPhotoId.HasValue;
    [System.Text.Json.Serialization.JsonIgnore] public string DisplayName => IsVirtualCopy ? $"{Name} · {CopyName}" : Name;
    /// <summary>Copies the record and version list; immutable original/state arrays remain shared.</summary>
    public PhotoDocument CopyRecord() => new()
    {
        Id = Id, Name = Name, MasterPhotoId = MasterPhotoId, CopyName = CopyName,
        Original = Original, Width = Width, Height = Height, ImportedAt = ImportedAt,
        Camera = Camera, Lens = Lens, ExposureInfo = ExposureInfo, State = State,
        Versions = [.. Versions], Revision = Revision
    };
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
    public const int CurrentSchemaVersion = 6;
    public int SchemaVersion { get; set; } = CurrentSchemaVersion;
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
        new("Nightfall", "Creative", new() { Exposure = -.4f, Temperature = -20, Highlights = -28, Shadows = 18, Saturation = -10, Vignette = -32 }),
        new("Teal & amber", "Color grading", new() { Grading = new() { Shadows = new(195, 22), Highlights = new(40, 18), Blending = 60 } }),
        new("Warm silver", "Color grading", new() { Monochrome = true, Grading = new() { Shadows = new(220, 8), Highlights = new(38, 14), Balance = 12 } }),
        new("Soft cinema", "RGB curves", new() { Channels = new() { Master = new() { Points = [new(0,.035f),new(.25f,.2f),new(.75f,.8f),new(1,.97f)] }, Blue = new() { Points = [new(0,.025f),new(.5f,.5f),new(1,.98f)] } } }),
        new("Crisp contrast", "RGB curves", new() { Channels = new() { Master = new() { Points = [new(0,0),new(.22f,.12f),new(.78f,.88f),new(1,1)] } } })
    ];
}
