namespace LightSpace.Workbench;

public sealed partial class StudioView : UserControl, IDisposable
{
    private const int PageSize = 60;
    private readonly IWorkspaceStorage _storage;
    private readonly PhotoRenderer _renderer = new();
    private readonly ThumbnailCache _thumbnails = new();
    private readonly Dictionary<string, WeakReference<FrameworkElement>> _widgets = [];
    private readonly Dictionary<string, AdjustmentSlider> _sliders = [];
    private readonly Dictionary<string, bool> _sectionStates = [];
    private readonly Grid _body = new();
    private readonly StackPanel _library = new(), _inspector = new(), _filmstrip = new() { Orientation = Orientation.Horizontal, Spacing = 4 };
    private readonly PhotoWrapPanel _photoGrid = new();
    private readonly List<PhotoCard> _filmCards = [], _gridCards = [];
    private Guid[] _tileIds = [];
    private readonly ScrollViewer _gridScroll;
    private readonly TextBlock _title = Theme.Text("All photos", 13), _status = Theme.Text("Ready", 10, true), _count = Theme.Text("", 10, true);
    private readonly HistogramView _histogram = new();
    private readonly List<LightButton> _ratingButtons = [];
    private readonly DispatcherTimer _saveTimer = new() { Interval = TimeSpan.FromMilliseconds(900) };
    private readonly DispatcherTimer _diagnosticsTimer = new() { Interval = TimeSpan.FromMilliseconds(400) };
    private readonly DispatcherTimer _histogramTimer = new() { Interval = TimeSpan.FromMilliseconds(160) };
    private readonly Dictionary<PhotoTool, LightButton> _tools = [];
    private readonly TextBox _search;
    private PhotoQuery _query = new();
    private IReadOnlyList<PhotoDocument> _visible = [];
    private int _page;
    private bool _gridMode, _sidebarVisible = true, _refreshing, _disposed, _buildingInspector;
    private Guid _displayedPhoto, _histogramPhoto;
    private PhotoState? _histogramState;
    private string _inspectorMode = "Edit", _librarySignature = "", _maskStructure = "";
    private long _cardBuilds, _libraryBuilds, _inspectorBuilds;
    private PhotoState? _clipboard;
    private ToneCurveView? _curve;
    private ColorGradingEditor? _gradingEditor;
    private MaskSettingsEditor? _maskEditor;
    private readonly RecoveryCoordinator _recovery;
    private readonly RecoveryPersistence? _persistence;
    private LightButton? _saveButton;
    public event Action<bool>? UnsavedChangesChanged;
    public EditorSession Session { get; }
    public PhotoViewport Viewport { get; }
    public event Action<StudioDiagnostics>? DiagnosticsChanged;
    public StudioView(EditorSession session, IWorkspaceStorage storage, bool recoveryLoaded = true, RecoveryPersistence? persistence = null)
    {
        Session = session; _storage = storage;
        _persistence = persistence ?? (storage is IRecoveryStore store ? new RecoveryPersistence(store) : null);
        _recovery = _persistence is null ? new RecoveryCoordinator(session, storage.WriteRecoveryAsync, recoveryLoaded) : RecoveryCoordinator.Incremental(session, _persistence, recoveryLoaded); _recovery.StatusChanged += RecoveryChanged;
        FontFamily = Theme.Font; RequestedTheme = ElementTheme.Dark; Background = Theme.Background;
        HorizontalContentAlignment = HorizontalAlignment.Stretch; VerticalContentAlignment = VerticalAlignment.Stretch;
        Viewport = new(session, _renderer); Register("canvas", Viewport);
        _gridScroll = new ScrollViewer { Content = _photoGrid, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, Visibility = Visibility.Collapsed, Background = Theme.Background };
        var root = new Grid { RowDefinitions = { new() { Height = new(54) }, new() { Height = new(1, GridUnitType.Star) } } };
        var header = new Grid { Background = Theme.Brush("#222222"), Padding = new(10, 0, 12, 0), ColumnDefinitions = { new() { Width = new(250) }, new() { Width = new(1, GridUnitType.Star) }, new() { Width = GridLength.Auto } } };
        var brand = Row(); brand.Spacing = 12;
        brand.Children.Add(Button("Toggle library", Glyph.Menu, null, () => { _sidebarVisible = !_sidebarVisible; Resize(); }));
        brand.Children.Add(new Border { Width = 30, Height = 30, CornerRadius = new(5), Background = Theme.Brush("#142d3c"), Child = new TextBlock { Text = "Ls", FontSize = 18, FontFamily = Theme.Font, Foreground = Theme.Brush("#a9d6ef"), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center } });
        brand.Children.Add(Theme.Text("LightSpace", 16)); header.Children.Add(brand);
        _search = Theme.Input("Search all photos", "Search photos"); _search.MaxWidth = 450; _search.Margin = new(16, 10, 16, 10); _search.VerticalAlignment = VerticalAlignment.Center;
        _search.TextChanged += (_, _) => { _query = _query with { Text = _search.Text }; _page = 0; RefreshCatalog(); if (_search.Text.Length > 0) SetGrid(true); };
        Grid.SetColumn(_search, 1); header.Children.Add(_search); Register("search", _search);
        var actions = Row(); actions.Spacing = 4;
        actions.Children.Add(Button("Undo", Glyph.Undo, null, Session.Undo)); actions.Children.Add(Button("Redo", Glyph.Redo, null, Session.Redo));
        actions.Children.Add(Button("Open catalog", Glyph.Folder, null, () => Run(OpenCatalogAsync))); actions.Children.Add(Button("Save catalog", Glyph.Check, null, () => Run(SaveCatalogAsync)));
        actions.Children.Add(Button("Export", Glyph.Export, "Export", () => Run(ShowExportAsync))); Grid.SetColumn(actions, 2); header.Children.Add(actions); root.Children.Add(header);
        foreach (var width in new[] { new GridLength(48), new(212), new(1, GridUnitType.Star), new(302), new(44) }) _body.ColumnDefinitions.Add(new() { Width = width });
        Grid.SetRow(_body, 1); root.Children.Add(_body); BuildRails();
        var libraryScroll = new ScrollViewer { Content = _library, Background = Theme.Sidebar, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
        Grid.SetColumn(libraryScroll, 1); _body.Children.Add(libraryScroll);
        var center = BuildCenter(); Grid.SetColumn(center, 2); _body.Children.Add(center);
        var inspectorScroll = new ScrollViewer { Content = _inspector, Background = Theme.Panel, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        Grid.SetColumn(inspectorScroll, 3); _body.Children.Add(inspectorScroll); Register("inspector-scroll", inspectorScroll);
        Content = root; KeyDown += Keyboard; KeyDown += AdvancedKeyboard; SizeChanged += (_, _) => Resize();
        Session.Changed += Committed; Session.ViewChanged += RefreshLive;
        Viewport.ViewChanged += ViewportChanged; Viewport.Status += SetStatus;
        _saveTimer.Tick += async (_, _) => { _saveTimer.Stop(); await SaveRecoveryAsync(); };
        _histogramTimer.Tick += (_, _) =>
        {
            _histogramTimer.Stop(); if (Session.Active is not { } photo) return;
            try { _histogram.Histogram = _renderer.CalculateHistogram(photo); _histogramPhoto = photo.Id; _histogramState = photo.State; }
            catch (Exception error) { SetStatus(error.Message); }
        };
        _diagnosticsTimer.Tick += (_, _) => PublishDiagnostics();
        Loaded += (_, _) => { if (DiagnosticsChanged is not null) _diagnosticsTimer.Start(); Resize(); RefreshAll(); };
        InitializePhotography(); RefreshAll(); RecoveryChanged(_recovery.Status); if (!recoveryLoaded) _saveTimer.Start();
    }
    private static StackPanel Row() => new() { Orientation = Orientation.Horizontal, Spacing = 8, VerticalAlignment = VerticalAlignment.Center };
    private T Register<T>(string id, T widget) where T : FrameworkElement
    {
        _widgets[id] = new(widget);
        if (_widgets.Count > 512)
            foreach (var key in _widgets.Where(p => !p.Value.TryGetTarget(out var target) || !target.IsLoaded).Select(p => p.Key).Where(k => k != id).ToArray()) _widgets.Remove(key);
        return widget;
    }
    private LightButton Button(string name, Glyph glyph = Glyph.None, string? text = null, Action? action = null) => Register(name, new LightButton(name, glyph, text, action));
    private PanelSection Section(string title, UIElement body, bool expanded = true)
    {
        var section = new PanelSection(title, body, _sectionStates.GetValueOrDefault(title, expanded));
        section.ExpandedChanged += value => _sectionStates[title] = value; Register("section-" + title, section.Header); return section;
    }
    private void Resize()
    {
        if (ActualWidth <= 0) return;
        _body.ColumnDefinitions[1].Width = new(!_focusMode && _sidebarVisible && ActualWidth >= 1080 ? _libraryWidth : 0);
        _body.ColumnDefinitions[3].Width = new(_focusMode ? 0 : ActualWidth < 850 ? Math.Min(270, _inspectorWidth) : _inspectorWidth);
        if (_libraryGrip is not null) _libraryGrip.Visibility = _body.ColumnDefinitions[1].Width.Value > 0 ? Visibility.Visible : Visibility.Collapsed;
        if (_inspectorGrip is not null) _inspectorGrip.Visibility = _focusMode ? Visibility.Collapsed : Visibility.Visible;
        if (_centerLayout is not null) _centerLayout.RowDefinitions[3].Height = new(!_focusMode && _filmstripVisible ? 116 : 0);
        PublishDiagnostics();
    }
    public void SetGrid(bool enabled) { _gridMode = enabled; _gridScroll.Visibility = enabled ? Visibility.Visible : Visibility.Collapsed; Viewport.Visibility = enabled ? Visibility.Collapsed : Visibility.Visible; PublishDiagnostics(); }
    public void ChooseTool(PhotoTool tool)
    {
        SetGrid(false); Viewport.SetTool(tool);
        ShowInspector(tool switch { PhotoTool.Crop or PhotoTool.Straighten => "Crop", PhotoTool.Clone => "Clone", PhotoTool.RadialMask or PhotoTool.LinearMask or PhotoTool.Brush or PhotoTool.ColorRange => "Masks", _ => "Edit" }); RefreshToolButtons();
    }
    private void RefreshToolButtons()
    {
        foreach (var (tool, button) in _tools)
            button.Selected = tool == Viewport.Tool || tool == PhotoTool.RadialMask && Viewport.Tool is PhotoTool.LinearMask or PhotoTool.Brush or PhotoTool.ColorRange;
    }
    private void ViewportChanged()
    {
        RefreshToolButtons();
        if (!_buildingInspector && _inspectorMode == "Masks" && Session.Active is { } p && Viewport.ActiveMask >= 0 && Viewport.ActiveMask < p.State.Masks.Length && _maskEditor?.Value.Id != p.State.Masks[Viewport.ActiveMask].Id) BuildInspector();
        PublishDiagnostics();
    }
    public void SetStatus(string text) { _status.Text = text; ToolTipService.SetToolTip(_status, text); PublishDiagnostics(); }
    private string MaskStructure() => Session.Active is { } p ? string.Join("|", p.State.Masks.Select(m => m.Id + ":" + m.Name)) : "";
    private void Committed()
    {
        RefreshCatalog(); RefreshLive(); if (_inspectorMode == "History" || _inspectorMode == "Masks" && _maskStructure != MaskStructure()) BuildInspector();
        _saveTimer.Stop(); _saveTimer.Start();
    }
    private void RefreshAll() { RefreshCatalog(); BuildInspector(); RefreshLive(); }
    private void RefreshLive()
    {
        if (_refreshing || _disposed) return; _refreshing = true;
        try
        {
            if (Session.Active is not { } photo) return;
            if (_displayedPhoto != photo.Id) { _displayedPhoto = photo.Id; Viewport.Fit(); BuildInspector(); RefreshCatalog(); }
            foreach (var (name, slider) in _sliders) slider.Value = photo.State.Develop.Get(name);
            if (_curve is not null) _curve.Curve = photo.State.Develop.Curve;
            if (_mixerEditor is not null) _mixerEditor.Value = photo.State.Develop.Mixer;
            if (_gradingEditor is not null) _gradingEditor.Value = photo.State.Develop.Grading;
            if (_maskEditor is not null && Viewport.ActiveMask >= 0 && Viewport.ActiveMask < photo.State.Masks.Length) _maskEditor.Value = photo.State.Masks[Viewport.ActiveMask];
            RefreshPhotography(photo);
            for (var i = 0; i < _ratingButtons.Count; i++) _ratingButtons[i].Selected = i < photo.State.Rating;
            if ((_histogramPhoto != photo.Id || _histogramState is null || !PhotoStateEquality.Pixels(_histogramState, photo.State)) && !_histogramTimer.IsEnabled) _histogramTimer.Start();
            RefreshToolButtons();
        }
        finally { _refreshing = false; }
        PublishDiagnostics();
    }
    private async void Run(Func<Task> operation) { try { await operation(); } catch (Exception error) { SetStatus(error.Message); } }
    public new void Dispose()
    {
        if (_disposed) return; _disposed = true; _recovery.Dispose(); _diagnosticsTimer.Stop(); _saveTimer.Stop(); _histogramTimer.Stop();
        Session.Changed -= Committed; Session.ViewChanged -= RefreshLive; Viewport.ViewChanged -= ViewportChanged;
        Viewport.Dispose(); _thumbnails.Dispose(); _renderer.Dispose();
    }
}
