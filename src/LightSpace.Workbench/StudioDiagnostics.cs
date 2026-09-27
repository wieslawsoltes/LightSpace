namespace LightSpace.Workbench;

public sealed record WidgetBounds(string Id, double X, double Y, double Width, double Height);
public sealed record WorkbenchPerformance(RendererStatistics Viewport, RendererStatistics Thumbnails, long ThumbnailRenders, long CardBuilds, long LibraryBuilds, long InspectorBuilds);
/// <summary>Read-only test diagnostics; no source bytes. CPU/cache counters are not GPU timing measurements.</summary>
public sealed record StudioDiagnostics(string ActivePhoto, string View, string Tool, float Exposure, int Rating, int Photos,
    int Masks, int CloneSpots, bool CanUndo, bool CanRedo, long Revision, string Status, WidgetBounds[] Widgets,
    RecoveryStatus Recovery, ColorGradingSettings Grading, LocalMask[] MaskSettings, float ComparisonPosition, WorkbenchPerformance Performance);
