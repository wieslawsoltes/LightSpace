namespace LightSpace.Workbench;

/// <summary>Read-only view state. Visible indicates that a reference is pinned; Grid can temporarily hide its panes.</summary>
public sealed record ReferenceDiagnostics(bool Visible, bool Stacked, bool Linked, Guid? SourceId, string? Name, float? Exposure,
    PhotoNavigationState ActiveNavigation, PhotoNavigationState ReferenceNavigation, long ReferenceDraws);
