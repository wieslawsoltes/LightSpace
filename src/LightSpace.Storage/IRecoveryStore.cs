using System.Security.Cryptography;
namespace LightSpace.Storage;

public sealed record RecoveryBlob(string Key, byte[] Bytes);
public sealed record RecoveryWrite(string Manifest, string[] References, RecoveryBlob[] Blobs);

/// <summary>Content-addressed originals and an atomically published edit manifest. Commit must verify all references before publication.</summary>
public interface IRecoveryStore
{
    Task<string?> ReadManifestAsync();
    Task<byte[]?> ReadBlobAsync(string key);
    Task CommitAsync(RecoveryWrite write);
}

public static class RecoveryKeys
{
    public const int MaximumBlobBytes = 64 * 1024 * 1024;
    public const int MaximumManifestBytes = 64 * 1024 * 1024;
    public static bool IsValid(string? key) => key is { Length: 64 } && key.All(c => c is >= '0' and <= '9' or >= 'a' and <= 'f');
    public static void Validate(string key)
    {
        if (!IsValid(key)) throw new InvalidDataException("Invalid recovery source key.");
    }
    public static string Hash(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));
}
