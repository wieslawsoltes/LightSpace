namespace LightSpace.Editing;

/// <summary>The acknowledgement of a particular committed catalog revision.</summary>
public sealed record RecoveryStatus(
    long CurrentRevision,
    long SavedRevision,
    bool HasActiveGesture,
    bool IsSaving,
    string? Error)
{
    public bool HasUnsavedChanges => HasActiveGesture || SavedRevision != CurrentRevision;
    public string State => Error is not null ? "Failed"
        : HasActiveGesture ? "Preview"
        : IsSaving ? "Saving"
        : HasUnsavedChanges ? "Pending"
        : "Saved";
}
