namespace LightSpace.Editing;

/// <summary>A serialized committed revision, never an in-progress editing preview.</summary>
public sealed record CommittedCatalogSnapshot(long Revision, string Json);
