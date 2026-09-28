namespace LightSpace.Storage;

/// <summary>Optional capability: a user-authorized XMP sidecar picker. No implicit folder scanning.</summary>
public interface ISidecarStorage
{
    Task<WorkspaceFile?> OpenSidecarAsync();
}
