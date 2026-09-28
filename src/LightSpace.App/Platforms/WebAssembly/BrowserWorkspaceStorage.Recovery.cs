using System.Runtime.InteropServices.JavaScript;
using System.Text;
using System.Text.Json;
using LightSpace.Storage;
namespace LightSpace.App;
internal sealed partial class BrowserWorkspaceStorage
{
    public async Task<string?> ReadManifestAsync()
    {
        var json = await BrowserRecovery.ReadManifest(); return string.IsNullOrEmpty(json) ? null : json;
    }
    public async Task<byte[]?> ReadBlobAsync(string key)
    {
        RecoveryKeys.Validate(key); var encoded = await BrowserRecovery.ReadBlob(key);
        if (encoded.Length == 0) return null;
        if (encoded.Length > RecoveryKeys.MaximumBlobBytes * 1.4) throw new InvalidDataException("Recovery source is too large.");
        return Convert.FromBase64String(encoded);
    }
    public async Task CommitAsync(RecoveryWrite write)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartArray();
            foreach (var blob in write.Blobs)
            {
                writer.WriteStartObject(); writer.WriteString("key", blob.Key);
                writer.WriteBase64String("data", blob.Bytes); writer.WriteEndObject();
            }
            writer.WriteEndArray();
        }
        // References are lowercase SHA-256 keys, not arbitrary strings.
        foreach (var key in write.References) RecoveryKeys.Validate(key);
        await BrowserRecovery.Commit(write.Manifest, string.Join(",", write.References), Encoding.UTF8.GetString(stream.ToArray()));
    }
}
internal static partial class BrowserRecovery
{
    [JSImport("globalThis.lightSpaceRecovery.readManifest")][return: JSMarshalAs<JSType.Promise<JSType.String>>] internal static partial Task<string> ReadManifest();
    [JSImport("globalThis.lightSpaceRecovery.readBlob")][return: JSMarshalAs<JSType.Promise<JSType.String>>] internal static partial Task<string> ReadBlob(string key);
    [JSImport("globalThis.lightSpaceRecovery.commit")][return: JSMarshalAs<JSType.Promise<JSType.String>>] internal static partial Task<string> Commit(string manifest, string keys, string sources);
}
