namespace LightSpace.Rendering.Skia;

/// <summary>
/// CPU work and cache counters, not GPU durations. CachedBytes counts unique
/// decoded sources; CachedImages counts per-photo processing identities.
/// The original six-argument constructor and deconstruction remain available.
/// </summary>
public sealed record RendererStatistics(long ImageDecodes, long ShaderBuilds, long DrawCalls, long CachedBytes, int CachedImages, long AutoSamples)
{
    public int CachedSources { get; init; }
    public long SharedSourceHits { get; init; }

    [System.Text.Json.Serialization.JsonConstructor]
    public RendererStatistics(long imageDecodes, long shaderBuilds, long drawCalls, long cachedBytes, int cachedImages,
        long autoSamples, int cachedSources, long sharedSourceHits)
        : this(imageDecodes, shaderBuilds, drawCalls, cachedBytes, cachedImages, autoSamples)
    {
        CachedSources = cachedSources; SharedSourceHits = sharedSourceHits;
    }
}
