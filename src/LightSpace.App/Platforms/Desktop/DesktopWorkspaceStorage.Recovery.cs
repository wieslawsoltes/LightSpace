using LightSpace.Storage;
namespace LightSpace.App;
internal sealed partial class DesktopWorkspaceStorage
{
    private readonly FileRecoveryStore _recoveryStore = new(DirectoryPath);
    public Task<string?> ReadManifestAsync() => _recoveryStore.ReadManifestAsync();
    public Task<byte[]?> ReadBlobAsync(string key) => _recoveryStore.ReadBlobAsync(key);
    public Task CommitAsync(RecoveryWrite write) => _recoveryStore.CommitAsync(write);
}
