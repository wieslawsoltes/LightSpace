namespace LightSpace.Rendering.Skia;

/// <summary>CPU work and cache counters. These are not GPU timings or presentation timestamps.</summary>
public sealed record RendererStatistics(long ImageDecodes, long ShaderBuilds, long DrawCalls, long CachedBytes, int CachedImages, long AutoSamples);
