namespace LightSpace.Workbench;

/// <summary>Bounded survey state and work counters. No encoded sources, pixels or GPU timing estimates.</summary>
public sealed record SurveyDiagnostics(bool Visible, int Total, int Remaining, int Excluded, int Page, int PageCount,
    Guid ActiveId, Guid[] VisibleIds, int Ready, int Failed, int PreparationSteps, int CardBuilds, int LayoutBuilds, RendererStatistics Renderer);
