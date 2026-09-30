namespace LightSpace.Workbench;

public sealed record WidgetBounds(string Id, double X, double Y, double Width, double Height);
public sealed record WorkbenchPerformance(RendererStatistics Viewport, RendererStatistics Thumbnails, long ThumbnailRenders, long CardBuilds, long LibraryBuilds, long InspectorBuilds);
public sealed record BrushDiagnostic(Guid MaskId, int Strokes, int Dabs, bool LastErase);
/// <summary>Read-only opt-in test diagnostics. Mask data omits brush coordinates; counters are not GPU timings.</summary>
public sealed record StudioDiagnostics(string ActivePhoto, string View, string Tool, float Exposure, int Rating, int Photos,
    int Masks, int CloneSpots, bool CanUndo, bool CanRedo, long Revision, string Status, WidgetBounds[] Widgets,
    RecoveryStatus Recovery, ColorGradingSettings Grading, LocalMask[] MaskSettings, float ComparisonPosition, WorkbenchPerformance Performance,
    ChannelCurves Curves, BrushDiagnostic[] Brushes, BrushCacheStatistics BrushCache, long CurveLookupBuilds, string Caption, string[] Keywords,
    RecoveryPersistenceStatistics? Persistence, long SourceSamplePixels, PhotographyDiagnostics Photography, ReferenceDiagnostics Reference)
{
    public CropToolDiagnostics? CropTool { get; init; }
    public SurveyDiagnostics? Survey { get; init; }
    public VirtualCopyDiagnostics? Copies { get; init; }
}

public sealed record CropToolDiagnostics(CropSettings Bounds, bool Locked, CropGuide Guide, bool Reversed, double OutputAspect, int OutputWidth, int OutputHeight);
