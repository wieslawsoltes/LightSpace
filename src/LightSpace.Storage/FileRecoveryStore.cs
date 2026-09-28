using System.Text;
namespace LightSpace.Storage;

/// <summary>Immutable source files are staged before manifest replacement. Orphans are retained, not deleted while another manifest may reference them.</summary>
public sealed class FileRecoveryStore : IRecoveryStore
{
    private readonly string _root;
    private readonly SemaphoreSlim _writer = new(1, 1);
    public FileRecoveryStore(string directory) => _root = Path.GetFullPath(directory);
    private string ManifestPath => Path.Combine(_root, "recovery-manifest-v1.json");
    private string BlobPath(string key) { RecoveryKeys.Validate(key); return Path.Combine(_root, "originals-v1", key + ".bin"); }
    public async Task<string?> ReadManifestAsync()
    {
        var bytes = await ReadBoundedAsync(ManifestPath, RecoveryKeys.MaximumManifestBytes);
        return bytes is null ? null : new UTF8Encoding(false, true).GetString(bytes);
    }
    public Task<byte[]?> ReadBlobAsync(string key) => ReadBoundedAsync(BlobPath(key), RecoveryKeys.MaximumBlobBytes);
    public async Task CommitAsync(RecoveryWrite write)
    {
        ArgumentNullException.ThrowIfNull(write);
        if (Encoding.UTF8.GetByteCount(write.Manifest) > RecoveryKeys.MaximumManifestBytes || write.References.Length > 5000)
            throw new InvalidDataException("Recovery exceeds the storage limits.");
        var required = write.References.ToHashSet(StringComparer.Ordinal);
        foreach (var key in required) RecoveryKeys.Validate(key);
        long bytes = 0;
        foreach (var blob in write.Blobs)
        {
            RecoveryKeys.Validate(blob.Key); bytes += blob.Bytes.Length;
            if (!required.Contains(blob.Key) || blob.Bytes.Length is < 1 or > RecoveryKeys.MaximumBlobBytes
                || bytes > 256L * 1024 * 1024 || RecoveryKeys.Hash(blob.Bytes) != blob.Key)
                throw new InvalidDataException("Invalid recovery source payload.");
        }
        await _writer.WaitAsync();
        try
        {
            Directory.CreateDirectory(Path.Combine(_root, "originals-v1"));
            foreach (var blob in write.Blobs) await ReplaceAsync(BlobPath(blob.Key), blob.Bytes);
            foreach (var key in required)
                if (!File.Exists(BlobPath(key))) throw new InvalidDataException("Recovery source is missing: " + key);
            await ReplaceAsync(ManifestPath, Encoding.UTF8.GetBytes(write.Manifest));
        }
        finally { _writer.Release(); }
    }
    private static async Task<byte[]?> ReadBoundedAsync(string path, int limit)
    {
        FileStream input;
        try { input = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete, 65536, true); }
        catch (FileNotFoundException) { return null; }
        catch (DirectoryNotFoundException) { return null; }
        await using (input)
        {
            if (input.Length > limit) throw new InvalidDataException("Recovery file exceeds its safety limit.");
            using var output = new MemoryStream(); var buffer = new byte[65536]; int count;
            while ((count = await input.ReadAsync(buffer)) > 0)
            {
                if (output.Length + count > limit) throw new InvalidDataException("Recovery file grew beyond its safety limit.");
                output.Write(buffer, 0, count);
            }
            return output.ToArray();
        }
    }
    private static async Task ReplaceAsync(string path, byte[] bytes)
    {
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await using (var file = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None, 65536, true))
            {
                await file.WriteAsync(bytes); await file.FlushAsync(); file.Flush(flushToDisk: true);
            }
            File.Move(temporary, path, true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
}
