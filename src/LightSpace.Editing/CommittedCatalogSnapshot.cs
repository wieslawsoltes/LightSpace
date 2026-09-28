using LightSpace.Storage;
namespace LightSpace.Editing;

/// <summary>A committed catalog or source-separated recovery revision, never an active preview.</summary>
public sealed record CommittedCatalogSnapshot(long Revision, string Json, RecoveryWrite? IncrementalWrite = null);
