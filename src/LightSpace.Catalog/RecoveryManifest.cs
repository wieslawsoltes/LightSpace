using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using LightSpace.Core;
namespace LightSpace.Catalog;

public sealed record RecoverySourceReference(string Key, int Length);
public sealed class RecoveryManifest
{
    public string Format { get; set; } = "LightSpace.Recovery";
    public int Version { get; set; } = 1;
    public long Revision { get; set; }
    public CatalogDocument Catalog { get; set; } = new();
    public Dictionary<Guid, RecoverySourceReference> Sources { get; set; } = [];
    public string Serialize()
    {
        var json = JsonSerializer.Serialize(this, RecoveryJsonContext.Default.RecoveryManifest);
        if (Encoding.UTF8.GetByteCount(json) > 64 * 1024 * 1024) throw new InvalidDataException("Recovery metadata exceeds 64 MiB.");
        return json;
    }
    /// <summary>Validate all metadata and references before any blob reads or allocations.</summary>
    public static RecoveryManifest Parse(string json)
    {
        if (json.Length > 64 * 1024 * 1024 || Encoding.UTF8.GetByteCount(json) > 64 * 1024 * 1024)
            throw new InvalidDataException("Recovery metadata exceeds 64 MiB.");
        var manifest = JsonSerializer.Deserialize(json, RecoveryJsonContext.Default.RecoveryManifest) ?? throw new InvalidDataException("Empty recovery manifest.");
        if (manifest.Format != "LightSpace.Recovery" || manifest.Version != 1 || manifest.Revision < 0)
            throw new InvalidDataException("Unsupported recovery format.");
        if (manifest.Catalog?.Photos is null || manifest.Sources is null || manifest.Catalog.Photos.Count > 5000
            || manifest.Sources.Count != manifest.Catalog.Photos.Count)
            throw new InvalidDataException("Invalid recovery source mapping.");
        long bytes = 0; var lengths = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var photo in manifest.Catalog.Photos)
        {
            if (photo is null || photo.Original is not { Length: 0 } || !manifest.Sources.TryGetValue(photo.Id, out var source)
                || source is null || source.Key is not { Length: 64 } || source.Key.Any(c => !(c is >= '0' and <= '9' or >= 'a' and <= 'f'))
                || source.Length is < 1 or > CatalogSerializer.MaxFileBytes)
                throw new InvalidDataException("Invalid recovery source reference.");
            if (lengths.TryGetValue(source.Key, out var length) && length != source.Length) throw new InvalidDataException("Conflicting source lengths.");
            lengths[source.Key] = source.Length; bytes += source.Length;
            if (bytes > CatalogSerializer.MaxCatalogBytes) throw new InvalidDataException("Recovery originals exceed 256 MiB.");
        }
        CatalogSerializer.Validate(manifest.Catalog);
        return manifest;
    }
}
[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(RecoveryManifest))]
internal partial class RecoveryJsonContext : JsonSerializerContext;
