namespace LightSpace.Workbench;

/// <summary>Read-only diagnostics for accessibility and real pointer-driven acceptance tests. No image bytes are exposed.</summary>
public sealed record WidgetBounds(string Id,double X,double Y,double Width,double Height);
public sealed record StudioDiagnostics(string ActivePhoto,string View,string Tool,float Exposure,int Rating,int Photos,int Masks,int CloneSpots,bool CanUndo,bool CanRedo,long Revision,string Status,WidgetBounds[] Widgets,RecoveryStatus Recovery);
