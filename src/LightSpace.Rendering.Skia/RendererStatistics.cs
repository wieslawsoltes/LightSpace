namespace LightSpace.Rendering.Skia;

/// <summary>CPU work and cache counters, not GPU durations. CachedBytes counts unique decoded sources; CachedImages counts per-photo processing identities.</summary>
public sealed record RendererStatistics(long ImageDecodes, long ShaderBuilds, long DrawCalls, long CachedBytes, int CachedImages, long AutoSamples,
    int CachedSources = 0, long SharedSourceHits = 0);
