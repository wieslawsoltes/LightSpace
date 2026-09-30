using System.Runtime.CompilerServices;
using System.Text;
using LightSpace.Catalog;
using LightSpace.Core;
using LightSpace.Storage;
namespace LightSpace.Editing;

public sealed record RecoveryPersistenceStatistics(long Hashes, long BytesHashed, long Commits, long BlobWrites, long BlobBytes, long ManifestBytes);

/// <summary>Owner-thread manifest builder. Source arrays are immutable by contract; weak hash memoization never retains a discarded original.</summary>
public sealed class RecoveryPersistence(IRecoveryStore store)
{
    private sealed record Identity(string Key);
    private readonly ConditionalWeakTable<byte[], Identity> _hashes = new();
    private readonly HashSet<string> _persisted = new(StringComparer.Ordinal);
    private long _hashCount, _hashedBytes, _commits, _blobWrites, _blobBytes, _manifestBytes;
    public RecoveryPersistenceStatistics Statistics => new(_hashCount, _hashedBytes, _commits, _blobWrites, _blobBytes, _manifestBytes);
    private string Identify(byte[] original)
    {
        if (_hashes.TryGetValue(original, out var identity)) return identity.Key;
        if (original.Length is < 1 or > CatalogSerializer.MaxFileBytes) throw new InvalidDataException("Invalid recovery original size.");
        var key = RecoveryKeys.Hash(original); _hashes.Add(original, new(key));
        _hashCount++; _hashedBytes += original.Length; return key;
    }
    public CommittedCatalogSnapshot Capture(EditorSession session)
    {
        var catalog = session.CopyCommittedCatalog();
        VirtualCopyCatalog.ValidateRelationships(catalog);
        var manifest = new RecoveryManifest { Revision = session.Revision, Catalog = catalog };
        var blobs = new Dictionary<string, RecoveryBlob>(StringComparer.Ordinal); long bytes = 0;
        foreach (var photo in catalog.Photos)
        {
            var key = Identify(photo.Original); manifest.Sources.Add(photo.Id, new(key, photo.Original.Length));
            if (blobs.TryAdd(key, new(key, photo.Original))) bytes += photo.Original.Length;
            if (bytes > CatalogSerializer.MaxCatalogBytes) throw new InvalidDataException("Recovery originals exceed 256 MiB.");
            photo.Original = [];
        }
        var json = manifest.Serialize();
        return new(session.Revision, json, new(json, blobs.Keys.ToArray(), blobs.Values.ToArray()));
    }
    public async Task CommitAsync(CommittedCatalogSnapshot snapshot)
    {
        var batch = snapshot.IncrementalWrite ?? throw new InvalidOperationException("Expected an incremental snapshot.");
        var missing = batch.Blobs.Where(blob => !_persisted.Contains(blob.Key)).ToArray();
        try
        {
            await store.CommitAsync(batch with { Blobs = missing });
            _persisted.IntersectWith(batch.References);
            foreach (var key in batch.References) _persisted.Add(key);
            _commits++; _blobWrites += missing.Length; _blobBytes += missing.Sum(blob => (long)blob.Bytes.Length);
            _manifestBytes += Encoding.UTF8.GetByteCount(batch.Manifest);
        }
        catch { _persisted.Clear(); throw; }
    }
    public async Task<CatalogDocument?> RestoreAsync()
    {
        var json = await store.ReadManifestAsync(); if (json is null) return null;
        var manifest = RecoveryManifest.Parse(json); var originals = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        foreach (var photo in manifest.Catalog.Photos)
        {
            var source = manifest.Sources[photo.Id];
            if (!originals.TryGetValue(source.Key, out var original))
            {
                original = await store.ReadBlobAsync(source.Key) ?? throw new InvalidDataException("Recovery original is missing: " + source.Key);
                if (original.Length != source.Length || Identify(original) != source.Key) throw new InvalidDataException("Recovery original failed its integrity check.");
                originals.Add(source.Key, original);
            }
            photo.Original = original;
        }
        foreach (var key in originals.Keys) _persisted.Add(key);
        return manifest.Catalog;
    }
}
