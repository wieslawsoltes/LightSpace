using System.Text.Json;
using System.Text.Json.Serialization;
using LightSpace.Core;
namespace LightSpace.Catalog;

public static class CatalogSerializer
{
    public const int MaxFileBytes = 64 * 1024 * 1024;
    public const int MaxCatalogBytes = 256 * 1024 * 1024;
    public static string Serialize(CatalogDocument document)
    {
        if (document.SchemaVersion != CatalogDocument.CurrentSchemaVersion)
            throw new InvalidDataException("Normalize legacy catalogs through Deserialize before saving new processing settings.");
        return JsonSerializer.Serialize(VirtualCopyCatalog.PortableSnapshot(document), CatalogJsonContext.Default.CatalogDocument);
    }
    public static CatalogDocument Deserialize(string json)
    {
        if (json.Length > MaxCatalogBytes * 1.4) throw new InvalidDataException("Catalog exceeds the 256 MiB safety limit.");
        var result = JsonSerializer.Deserialize(json, CatalogJsonContext.Default.CatalogDocument) ?? throw new InvalidDataException("Empty catalog.");
        return Validate(result);
    }
    public static CatalogDocument Validate(CatalogDocument result)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (result.SchemaVersion is < 1 or > CatalogDocument.CurrentSchemaVersion) throw new InvalidDataException($"Unsupported catalog version {result.SchemaVersion}.");
        if (result.Photos is null || result.Albums is null || result.Photos.Count > 5000 || result.Albums.Count > 5000) throw new InvalidDataException("Invalid catalog structure.");
        long bytes = 0; var ids = new HashSet<Guid>();
        foreach (var photo in result.Photos)
        {
            if (photo is null || !ids.Add(photo.Id)) throw new InvalidDataException("Missing or duplicate photo identity.");
            if (photo.Original is null || photo.Original.Length > MaxFileBytes) throw new InvalidDataException("Invalid photo source.");
            if (!photo.IsVirtualCopy) bytes += photo.Original.Length;
            if (bytes > MaxCatalogBytes || photo.Width <= 0 || photo.Height <= 0 || (long)photo.Width * photo.Height > 100_000_000)
                throw new InvalidDataException("Catalog exceeds image safety limits.");
            photo.Name = string.IsNullOrWhiteSpace(photo.Name) ? "Untitled" : photo.Name;
            photo.State = (photo.State ?? new()).Normalize();
            photo.Versions = (photo.Versions ?? []).Where(v => v?.State is not null).Take(100).Select(v => v with { State = v.State.Normalize() }).ToList();
        }
        VirtualCopyCatalog.ValidateRelationships(result, hydrateSources: true);
        var albums = new HashSet<Guid>();
        foreach (var album in result.Albums)
        {
            if (album is null || !albums.Add(album.Id)) throw new InvalidDataException("Missing or duplicate album identity.");
            album.Name ??= "Album"; album.Photos = (album.Photos ?? []).Where(ids.Contains).Distinct().ToList();
        }
        if (!ids.Contains(result.ActivePhoto)) result.ActivePhoto = result.Photos.FirstOrDefault()?.Id ?? Guid.Empty;
        // Legacy catalogs have no virtual-copy relations. New saves use schema 6
        // so older readers cannot silently discard copy identity/source sharing.
        result.SchemaVersion = CatalogDocument.CurrentSchemaVersion;
        return result;
    }
    public static string SerializeSettings(PhotoState state) => JsonSerializer.Serialize(state, CatalogJsonContext.Default.PhotoState);
    public static PhotoState DeserializeSettings(string json) => (JsonSerializer.Deserialize(json, CatalogJsonContext.Default.PhotoState) ?? new()).Normalize();
}

[JsonSourceGenerationOptions(WriteIndented = false, PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(CatalogDocument))]
[JsonSerializable(typeof(PhotoState))]
internal partial class CatalogJsonContext : JsonSerializerContext;
