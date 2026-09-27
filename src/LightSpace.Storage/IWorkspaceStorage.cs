namespace LightSpace.Storage;

public sealed record WorkspaceFile(string Name, byte[] Bytes);
public interface IWorkspaceStorage
{
    Task<IReadOnlyList<WorkspaceFile>> OpenImagesAsync();
    Task<WorkspaceFile?> OpenCatalogAsync();
    Task SaveAsync(string name, byte[] bytes, string mimeType);
    Task<string?> ReadRecoveryAsync();
    Task WriteRecoveryAsync(string catalog);
}
