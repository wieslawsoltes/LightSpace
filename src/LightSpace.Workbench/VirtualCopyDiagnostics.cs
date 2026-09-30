namespace LightSpace.Workbench;

public sealed record CopyFamilyItem(Guid Id, string CopyName, string DisplayName, bool IsVirtualCopy, float Exposure);
public sealed record VirtualCopyDiagnostics(Guid? ActiveId, Guid? MasterId, string Name, string Filter, Guid[] VisibleIds, Guid[] SelectedIds, CopyFamilyItem[] Family);
