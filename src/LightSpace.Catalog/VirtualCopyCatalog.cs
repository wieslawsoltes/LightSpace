using LightSpace.Core;
namespace LightSpace.Catalog;

/// <summary>Validates rooted copy families and builds portable records without repeating their source payloads.</summary>
public static class VirtualCopyCatalog
{
    public static long SourceBytes(CatalogDocument catalog) => catalog.Photos.Where(p => !p.IsVirtualCopy).Sum(p => (long)p.Original.Length);

    public static void ValidateRelationships(CatalogDocument catalog, bool hydrateSources = false)
    {
        var photos = new Dictionary<Guid, PhotoDocument>();
        foreach (var photo in catalog.Photos)
            if (photo is null || photo.Id == Guid.Empty || !photos.TryAdd(photo.Id, photo))
                throw new InvalidDataException("Missing or duplicate photo identity.");
        var names = new Dictionary<Guid, HashSet<string>>();
        foreach (var photo in catalog.Photos)
        {
            if (photo.MasterPhotoId is not Guid id)
            {
                if (!string.IsNullOrEmpty(photo.CopyName)) throw new InvalidDataException("An original cannot have a virtual-copy name.");
                if (hydrateSources) photo.CopyName = "";
                continue;
            }
            if (catalog.SchemaVersion < 6 || id == photo.Id || !photos.TryGetValue(id, out var master) || master.IsVirtualCopy)
                throw new InvalidDataException("Virtual copies must reference an existing original, not another copy.");
            string name;
            try { name = VirtualCopyNames.Validate(photo.CopyName); }
            catch (ArgumentException error) { throw new InvalidDataException("Invalid virtual-copy name.", error); }
            if (photo.CopyName != name) throw new InvalidDataException("Virtual-copy names cannot have surrounding whitespace.");
            if (!names.TryGetValue(id, out var family)) names.Add(id, family = new(StringComparer.OrdinalIgnoreCase));
            if (!family.Add(name)) throw new InvalidDataException("Duplicate virtual-copy name in one original family.");
            if (photo.Width != master.Width || photo.Height != master.Height || photo.Original is null || master.Original is null)
                throw new InvalidDataException("Virtual-copy source dimensions do not match its original.");
            if (photo.Original.Length > 0 && !ReferenceEquals(photo.Original, master.Original) && !photo.Original.AsSpan().SequenceEqual(master.Original))
                throw new InvalidDataException("Virtual-copy source content differs from its original.");
            if (hydrateSources) photo.Original = master.Original;
        }
    }

    public static CatalogDocument PortableSnapshot(CatalogDocument catalog)
    {
        ValidateRelationships(catalog);
        if (!catalog.Photos.Any(p => p.IsVirtualCopy)) return catalog;
        return new CatalogDocument
        {
            SchemaVersion = catalog.SchemaVersion, ActivePhoto = catalog.ActivePhoto, Albums = catalog.Albums,
            Photos = catalog.Photos.Select(photo =>
            {
                if (!photo.IsVirtualCopy) return photo;
                var record = photo.CopyRecord(); record.Original = []; return record;
            }).ToList()
        };
    }
}
