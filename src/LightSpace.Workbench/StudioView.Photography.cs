namespace LightSpace.Workbench;

public sealed partial class StudioView
{
    private GeometryEditor? _geometryEditor;
    private OpticsEditor? _opticsEditor;
    private PanelResizeGrip? _libraryGrip, _inspectorGrip;
    private double _libraryWidth = 212, _inspectorWidth = 302;
    private double _resizeStart;
    private bool _focusMode, _filmstripVisible = true;
    private Grid? _centerLayout;
    private float _histogramStart;

    private void InitializePhotography()
    {
        foreach (var (id, widget) in _histogram.Widgets) Register(id, widget);
        _histogram.ClippingChanged += SetClipping;
        _histogram.AdjustmentStarted += name =>
        {
            if (Session.Active is not { } photo) return;
            _histogramStart = photo.State.Develop.Get(name); Session.BeginGesture();
        };
        _histogram.AdjustmentDelta += (name, delta) => Session.Preview(s => s with { Develop = s.Develop.Set(name, _histogramStart + delta) });
        _histogram.Committed += () => Session.CommitGesture("Histogram tone"); _histogram.Canceled += Session.CancelGesture;
        _libraryGrip = Register("library-resize", new PanelResizeGrip("Resize library") { HorizontalAlignment = HorizontalAlignment.Right });
        Grid.SetColumn(_libraryGrip, 1); _body.Children.Add(_libraryGrip);
        _libraryGrip.Started += () => _resizeStart = _libraryWidth;
        _libraryGrip.Delta += delta => { _libraryWidth = Math.Clamp(_resizeStart + delta, 150, 360); Resize(); };
        _libraryGrip.Canceled += () => { _libraryWidth = _resizeStart; Resize(); };
        _libraryGrip.Reset += () => { _libraryWidth = 212; Resize(); };
        _inspectorGrip = Register("inspector-resize", new PanelResizeGrip("Resize edit panel") { HorizontalAlignment = HorizontalAlignment.Left });
        Grid.SetColumn(_inspectorGrip, 3); _body.Children.Add(_inspectorGrip);
        _inspectorGrip.Started += () => _resizeStart = _inspectorWidth;
        _inspectorGrip.Delta += delta => { _inspectorWidth = Math.Clamp(_resizeStart - delta, 270, Math.Max(270, Math.Min(520, ActualWidth * .5))); Resize(); };
        _inspectorGrip.Canceled += () => { _inspectorWidth = _resizeStart; Resize(); };
        _inspectorGrip.Reset += () => { _inspectorWidth = 302; Resize(); };
    }
    private void SetClipping(ClippingIndicators value)
    {
        Viewport.Clipping = value; _histogram.Clipping = value; Viewport.Invalidate(); PublishDiagnostics();
    }
    private void StartWhiteBalance()
    {
        SetGrid(false); Viewport.SetTool(PhotoTool.WhiteBalance);
        SetStatus("Click a neutral area in the photograph to set relative white balance. Escape leaves the picker.");
    }
    private void StartStraighten()
    {
        SetGrid(false); Viewport.SetTool(PhotoTool.Straighten); ShowInspector("Crop");
        SetStatus("Drag along the horizon. Release to straighten; Escape cancels the gesture.");
    }
    private GeometryEditor CreateGeometryEditor(PhotoDocument photo)
    {
        var editor = new GeometryEditor { Value = photo.State.Geometry }; _geometryEditor = editor;
        editor.Previewed += value => Session.Preview(s => s with { Geometry = value });
        editor.Committed += () => Session.CommitGesture("Geometry"); editor.Canceled += Session.CancelGesture;
        editor.StraightenRequested += StartStraighten;
        foreach (var (id, widget) in editor.Widgets) Register(id, widget); return editor;
    }
    private OpticsEditor CreateOpticsEditor(PhotoDocument photo)
    {
        var editor = new OpticsEditor { Value = photo.State.Optics }; _opticsEditor = editor;
        editor.Previewed += value => Session.Preview(s => s with { Optics = value });
        editor.Committed += () => Session.CommitGesture("Optics"); editor.Canceled += Session.CancelGesture;
        foreach (var (id, widget) in editor.Widgets) Register(id, widget); return editor;
    }
    private void BuildGeometry(PhotoDocument photo)
    {
        var panel = new StackPanel { Margin = new(18, 0, 18, 18), Spacing = 5 };
        panel.Children.Add(CreateGeometryEditor(photo));
        panel.Children.Add(Note("Manual projective correction. Constrain crop keeps the frame inside a conservative valid image region."));
        _inspector.Children.Add(panel);
    }
    private void BuildOptics(PhotoDocument photo)
    {
        var panel = new StackPanel { Margin = new(18, 0, 18, 18) };
        panel.Children.Add(CreateOpticsEditor(photo));
        panel.Children.Add(Button("Open geometry", Glyph.Crop, "Geometry and constrain crop", () => ShowInspector("Geometry")));
        _inspector.Children.Add(panel);
    }
    private void ToggleFocusMode() { _focusMode = !_focusMode; Resize(); }
    private void ToggleFilmstrip() { _filmstripVisible = !_filmstripVisible; Resize(); }
    private void RefreshPhotography(PhotoDocument photo)
    {
        if (_geometryEditor is not null) _geometryEditor.Value = photo.State.Geometry;
        if (_opticsEditor is not null) _opticsEditor.Value = photo.State.Optics;
    }
    private PhotographyDiagnostics Photography(PhotoDocument? photo) => new(photo?.State.Geometry ?? new(), photo?.State.Optics ?? new(),
        Viewport.Clipping, _inspectorWidth, _libraryWidth, _focusMode, _filmstripVisible, _renderer.Presentation);
}

public sealed record PhotographyDiagnostics(GeometrySettings Geometry, LensCorrectionSettings Optics, ClippingIndicators Clipping,
    double InspectorWidth, double LibraryWidth, bool FocusMode, bool FilmstripVisible, PresentationStatistics Presentation);
