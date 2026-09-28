namespace LightSpace.Workbench;

public sealed partial class StudioView : UserControl, IDisposable
{
    private const int PageSize = 60;
    private readonly IWorkspaceStorage _storage;
    private readonly PhotoRenderer _renderer = new();
    private readonly ThumbnailCache _thumbnails = new();
    private readonly Dictionary<string, FrameworkElement> _widgets = [];
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
    private LightButton? _saveButton;
    public event Action<bool>? UnsavedChangesChanged;
    public EditorSession Session { get; }
    public PhotoViewport Viewport { get; }
    public event Action<StudioDiagnostics>? DiagnosticsChanged;

    public StudioView(EditorSession session, IWorkspaceStorage storage, bool recoveryLoaded = true)
    {
        Session = session; _storage = storage;
        _recovery = new RecoveryCoordinator(session, storage.WriteRecoveryAsync, recoveryLoaded); _recovery.StatusChanged += RecoveryChanged;
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
        Content = root; KeyDown += Keyboard; SizeChanged += (_, _) => Resize();
        Session.Changed += Committed; Session.ViewChanged += RefreshLive;
        Viewport.ViewChanged += ViewportChanged; Viewport.Status += SetStatus;
        _saveTimer.Tick += async (_, _) => { _saveTimer.Stop(); await SaveRecoveryAsync(); };
        _histogramTimer.Tick += (_, _) =>
        {
            _histogramTimer.Stop();
            if (Session.Active is not { } photo) return;
            try { _histogram.Histogram = _renderer.CalculateHistogram(photo); _histogramPhoto = photo.Id; _histogramState = photo.State; }
            catch (Exception error) { SetStatus(error.Message); }
        };
        _diagnosticsTimer.Tick += (_, _) => PublishDiagnostics();
        Loaded += (_, _) => { if (DiagnosticsChanged is not null) _diagnosticsTimer.Start(); Resize(); RefreshAll(); };
        RefreshAll(); RecoveryChanged(_recovery.Status); if (!recoveryLoaded) _saveTimer.Start();
    }
    private static StackPanel Row() => new() { Orientation = Orientation.Horizontal, Spacing = 8, VerticalAlignment = VerticalAlignment.Center };
    private T Register<T>(string id, T widget) where T : FrameworkElement { _widgets[id] = widget; return widget; }
    private LightButton Button(string name, Glyph glyph = Glyph.None, string? text = null, Action? action = null) => Register(name, new LightButton(name, glyph, text, action));
    private PanelSection Section(string title, UIElement body, bool expanded = true)
    {
        var section = new PanelSection(title, body, _sectionStates.GetValueOrDefault(title, expanded));
        section.ExpandedChanged += value => _sectionStates[title] = value; Register("section-" + title, section.Header); return section;
    }
    private void BuildRails()
    {
        var rail = new Grid { Background = Theme.Brush("#1b1b1b"), RowDefinitions = { new() { Height = new(1, GridUnitType.Star) }, new() { Height = GridLength.Auto } } };
        var top = new StackPanel { Spacing = 8, Margin = new(3, 12, 3, 0) };
        top.Children.Add(Button("Add photos", Glyph.Add, null, () => Run(ImportAsync))); top.Children.Add(Button("Grid view", Glyph.Grid, null, () => SetGrid(true))); top.Children.Add(Button("Detail view", Glyph.Photo, null, () => SetGrid(false))); rail.Children.Add(top);
        var bottom = new StackPanel { Spacing = 8, Margin = new(3, 0, 3, 12) };
        bottom.Children.Add(Button("Help", Glyph.Help, null, () => Run(ShowHelpAsync))); bottom.Children.Add(Button("Local storage", Glyph.Info, null, () => SetStatus("Local-only catalog. Export a catalog backup to retain originals and edits."))); Grid.SetRow(bottom, 1); rail.Children.Add(bottom); _body.Children.Add(rail);
        var tools = new StackPanel { Background = Theme.Brush("#202020"), Spacing = 9, Padding = new(3, 12, 3, 0) };
        foreach (var (tool, glyph, name) in new[] { (PhotoTool.Edit, Glyph.Edit, "Edit photo"), (PhotoTool.Crop, Glyph.Crop, "Crop photo"), (PhotoTool.Clone, Glyph.Clone, "Clone tool"), (PhotoTool.RadialMask, Glyph.Mask, "Masking") })
        { var button = Button(name, glyph, null, () => ChooseTool(tool)); button.Padding = new(8); _tools[tool] = button; tools.Children.Add(button); }
        tools.Children.Add(Theme.Divider()); tools.Children.Add(Button("Presets", Glyph.Presets, null, () => ShowInspector("Presets")));
        tools.Children.Add(Button("Versions and history", Glyph.History, null, () => ShowInspector("History"))); tools.Children.Add(Button("Photo information", Glyph.Info, null, () => ShowInspector("Info")));
        Grid.SetColumn(tools, 4); _body.Children.Add(tools);
    }
    private UIElement BuildCenter()
    {
        var center = new Grid { RowDefinitions = { new() { Height = new(42) }, new() { Height = new(1, GridUnitType.Star) }, new() { Height = new(39) }, new() { Height = new(116) }, new() { Height = new(25) } } };
        var breadcrumb = new Grid { Padding = new(18, 0, 14, 0), Background = Theme.Brush("#1c1c1c"), ColumnDefinitions = { new() { Width = new(1, GridUnitType.Star) }, new() { Width = GridLength.Auto } } }; breadcrumb.Children.Add(_title);
        var pages = Row(); pages.Children.Add(Button("Previous page", text: "‹", action: () => { if (_page > 0) { _page--; RefreshCatalog(); } })); pages.Children.Add(Button("Next page", text: "›", action: () => { if ((_page + 1) * PageSize < _visible.Count) { _page++; RefreshCatalog(); } })); Grid.SetColumn(pages, 1); breadcrumb.Children.Add(pages); center.Children.Add(breadcrumb);
        var photo = new Grid(); photo.Children.Add(Viewport); photo.Children.Add(_gridScroll); Grid.SetRow(photo, 1); center.Children.Add(photo);
        var toolbar = new Grid { Background = Theme.Brush("#202020"), Padding = new(9, 0, 9, 0), ColumnDefinitions = { new() { Width = new(1, GridUnitType.Star) }, new() { Width = GridLength.Auto }, new() { Width = new(1, GridUnitType.Star) } } };
        var left = Row(); left.Spacing = 2;
        left.Children.Add(Button("Fit image", text: "Fit", action: Viewport.Fit)); left.Children.Add(Button("Zoom image", text: "100%", action: Viewport.ToggleZoom));
        left.Children.Add(Button("Before and after", Glyph.Compare, null, () => { Viewport.Compare = !Viewport.Compare; Viewport.Invalidate(); PublishDiagnostics(); }));
        left.Children.Add(Button("Show original", text: "Original", action: () => { Viewport.Before = !Viewport.Before; Viewport.Invalidate(); })); toolbar.Children.Add(left);
        var rating = Row(); rating.Spacing = 0;
        for (var i = 1; i <= 5; i++) { var value = i; var b = Button($"Rate {i}", Glyph.Star, null, () => Session.Edit($"Rate {value} stars", s => s with { Rating = value }, true)); b.MinWidth = 25; b.Padding = new(3, 6, 3, 6); _ratingButtons.Add(b); rating.Children.Add(b); }
        Grid.SetColumn(rating, 1); toolbar.Children.Add(rating);
        var flags = Row(); flags.HorizontalAlignment = HorizontalAlignment.Right; flags.Spacing = 2;
        flags.Children.Add(Button("Pick photo", Glyph.Flag, null, () => Session.Edit("Flag as pick", s => s with { Flag = s.Flag == PhotoFlag.Pick ? PhotoFlag.None : PhotoFlag.Pick }, true)));
        flags.Children.Add(Button("Reject photo", Glyph.Reject, null, () => Session.Edit("Flag as rejected", s => s with { Flag = PhotoFlag.Reject }, true))); Grid.SetColumn(flags, 2); toolbar.Children.Add(flags); Grid.SetRow(toolbar, 2); center.Children.Add(toolbar);
        var film = new ScrollViewer { Content = _filmstrip, Padding = new(10, 8, 10, 6), VerticalScrollBarVisibility = ScrollBarVisibility.Disabled, HorizontalScrollBarVisibility = ScrollBarVisibility.Auto, Background = Theme.Brush("#191919") }; Grid.SetRow(film, 3); center.Children.Add(film);
        var footer = new Grid { Background = Theme.Brush("#1f1f1f"), Padding = new(12, 0, 12, 0), ColumnDefinitions = { new() { Width = GridLength.Auto }, new() { Width = new(1, GridUnitType.Star) }, new() { Width = GridLength.Auto } } };
        footer.Children.Add(_count); _status.Margin = new(18, 0, 18, 0); Grid.SetColumn(_status, 1); footer.Children.Add(_status);
        _saveButton = Button("Save recovery now", Glyph.Check, "Saved", () => Run(SaveRecoveryAsync)); _saveButton.MinHeight = 23; _saveButton.Padding = new(3, 0, 3, 0); Grid.SetColumn(_saveButton, 2); footer.Children.Add(_saveButton); Grid.SetRow(footer, 4); center.Children.Add(footer);
        return center;
    }
    private void Resize()
    {
        if (ActualWidth <= 0) return;
        _body.ColumnDefinitions[1].Width = new(_sidebarVisible && ActualWidth >= 1080 ? 212 : 0);
        _body.ColumnDefinitions[3].Width = new(ActualWidth < 850 ? 250 : 302); PublishDiagnostics();
    }
    public void SetGrid(bool enabled) { _gridMode = enabled; _gridScroll.Visibility = enabled ? Visibility.Visible : Visibility.Collapsed; Viewport.Visibility = enabled ? Visibility.Collapsed : Visibility.Visible; PublishDiagnostics(); }
    public void ChooseTool(PhotoTool tool)
    {
        SetGrid(false); Viewport.SetTool(tool); ShowInspector(tool switch { PhotoTool.Crop => "Crop", PhotoTool.Clone => "Clone", PhotoTool.RadialMask or PhotoTool.LinearMask => "Masks", _ => "Edit" }); RefreshToolButtons();
    }
    private void RefreshToolButtons() { foreach (var (tool, button) in _tools) button.Selected = tool == Viewport.Tool || tool == PhotoTool.RadialMask && Viewport.Tool == PhotoTool.LinearMask; }
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
        RefreshCatalog(); RefreshLive();
        if (_inspectorMode == "History" || _inspectorMode == "Masks" && _maskStructure != MaskStructure()) BuildInspector();
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
            for (var i = 0; i < _ratingButtons.Count; i++) _ratingButtons[i].Selected = i < photo.State.Rating;
            if ((_histogramPhoto != photo.Id || _histogramState is null || !PhotoStateEquality.Pixels(_histogramState, photo.State)) && !_histogramTimer.IsEnabled) _histogramTimer.Start();
            RefreshToolButtons();
        }
        finally { _refreshing = false; }
        PublishDiagnostics();
    }
    private void RefreshCatalog()
    {
        _visible = _query.Execute(Session.Catalog); _page = Math.Clamp(_page, 0, Math.Max(0, (_visible.Count - 1) / PageSize));
        var page = _visible.Skip(_page * PageSize).Take(PageSize).ToArray(); var ids = page.Select(p => p.Id).ToArray();
        if (!_tileIds.AsSpan().SequenceEqual(ids))
        {
            foreach (var key in _widgets.Keys.Where(k => k.StartsWith("photo-") || k.StartsWith("grid-photo-")).ToArray()) _widgets.Remove(key);
            _filmstrip.Children.Clear(); _photoGrid.Children.Clear(); _filmCards.Clear(); _gridCards.Clear(); _tileIds = ids;
            for (var i = 0; i < page.Length; i++)
            {
                var film = CreateCard(page[i], i, false); var grid = CreateCard(page[i], i, true);
                _filmCards.Add(film); _gridCards.Add(grid); _filmstrip.Children.Add(film); _photoGrid.Children.Add(grid);
            }
        }
        for (var i = 0; i < page.Length; i++) { var selected = Session.Selection.Contains(page[i].Id); _filmCards[i].Update(page[i], selected); _gridCards[i].Update(page[i], selected); }
        _count.Text = $"{_visible.Count} photos · {Session.Selection.Count} selected";
        _title.Text = _query.Album is Guid id ? Session.Catalog.Albums.FirstOrDefault(a => a.Id == id)?.Name ?? "Album" : _query.Flag is PhotoFlag.Pick ? "Picks" : _query.Flag is PhotoFlag.Reject ? "Rejected" : _query.MinimumRating > 0 ? "Favorites" : "All photos";
        if (_visible.Count > PageSize) _title.Text += $"  /  {_page + 1} of {(_visible.Count + PageSize - 1) / PageSize}";
        BuildLibrary(); PublishDiagnostics();
    }
    private PhotoCard CreateCard(PhotoDocument photo, int index, bool grid)
    {
        var card = new PhotoCard(photo, _thumbnails, grid); _cardBuilds++;
        card.Selected += selected =>
        {
            var control = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);
            Session.Select(selected.Id, control); RefreshCatalog(); if (grid && !control) SetGrid(false);
        };
        return Register((grid ? "grid-photo-" : "photo-") + index, card);
    }
    private void BuildLibrary()
    {
        var signature = _query.Album + ":" + _query.Flag + ":" + _query.MinimumRating + "|" + string.Join("|", Session.Catalog.Albums.Select(a => a.Id + ":" + a.Name));
        if (_librarySignature == signature) return; _librarySignature = signature; _libraryBuilds++;
        _library.Children.Clear(); _library.Padding = new(10, 12, 10, 12);
        var import = Button("Import photos", Glyph.Add, "Add photos", () => Run(ImportAsync)); import.HorizontalAlignment = HorizontalAlignment.Stretch; import.HorizontalContentAlignment = HorizontalAlignment.Left; import.Margin = new(0, 0, 0, 20); _library.Children.Add(import);
        void Filter(string name, Glyph glyph, PhotoQuery query)
        {
            var b = Button(name, glyph, name, () => { _query = query with { Text = _search.Text }; _page = 0; RefreshCatalog(); SetGrid(true); });
            b.HorizontalAlignment = HorizontalAlignment.Stretch; b.HorizontalContentAlignment = HorizontalAlignment.Left; b.Margin = new(0, 2, 0, 2); _library.Children.Add(b);
        }
        Filter("All photos", Glyph.Photo, new()); Filter("Favorites", Glyph.Star, new(MinimumRating: 4)); Filter("Picks", Glyph.Flag, new(Flag: PhotoFlag.Pick)); Filter("Rejected", Glyph.Reject, new(Flag: PhotoFlag.Reject));
        var divider = Theme.Divider(); divider.Margin = new(0, 20, 0, 14); _library.Children.Add(divider);
        var heading = new Grid { ColumnDefinitions = { new() { Width = new(1, GridUnitType.Star) }, new() { Width = GridLength.Auto } } };
        var title = Theme.Text("ALBUMS", 10, true); title.Margin = new(10, 0, 0, 0); heading.Children.Add(title);
        var add = Button("Create album", Glyph.Add, null, () => Run(CreateAlbumAsync)); Grid.SetColumn(add, 1); heading.Children.Add(add); _library.Children.Add(heading);
        foreach (var album in Session.Catalog.Albums)
        {
            var b = Button("Album " + album.Name, Glyph.Folder, album.Name, () => { _query = _query with { Album = album.Id, Flag = null, MinimumRating = 0 }; _page = 0; RefreshCatalog(); SetGrid(true); });
            b.HorizontalAlignment = HorizontalAlignment.Stretch; b.HorizontalContentAlignment = HorizontalAlignment.Left; b.Selected = _query.Album == album.Id; _library.Children.Add(b);
        }
        var hint = Theme.Text("Your photos, on this device.", 10, true); hint.Margin = new(10, 35, 10, 0); _library.Children.Add(hint);
    }
    public void PublishDiagnostics()
    {
        if (DiagnosticsChanged is null || !IsLoaded || _disposed) return;
        var list = new List<WidgetBounds>();
        foreach (var (id, widget) in _widgets)
        {
            if (!widget.IsLoaded || widget.ActualWidth < 1 || widget.ActualHeight < 1) continue;
            var visible = true; DependencyObject? parent = widget;
            while (parent is not null) { if (parent is FrameworkElement element && element.Visibility == Visibility.Collapsed) { visible = false; break; } parent = VisualTreeHelper.GetParent(parent); }
            if (!visible) continue;
            try { var rect = widget.TransformToVisual(this).TransformBounds(new Rect(0, 0, widget.ActualWidth, widget.ActualHeight)); if (rect.Right > 0 && rect.Bottom > 0 && rect.X < ActualWidth && rect.Y < ActualHeight) list.Add(new(id, rect.X, rect.Y, rect.Width, rect.Height)); } catch (InvalidOperationException) { }
        }
        if (!_gridMode)
        {
            var origin = Viewport.TransformToVisual(this).TransformPoint(new(0, 0));
            var image = Viewport.ImageBounds; list.Add(new("image", origin.X + image.X, origin.Y + image.Y, image.Width, image.Height));
            foreach (var handle in Viewport.InteractionHandles()) list.Add(new(handle.Id, origin.X + handle.X - 7, origin.Y + handle.Y - 7, 14, 14));
        }
        var p = Session.Active;
        DiagnosticsChanged.Invoke(new(p?.Name ?? "", _gridMode ? "Grid" : "Detail", Viewport.Tool.ToString(), p?.State.Develop.Exposure ?? 0, p?.State.Rating ?? 0,
            Session.Catalog.Photos.Count, p?.State.Masks.Length ?? 0, p?.State.CloneSpots.Length ?? 0, Session.CanUndo, Session.CanRedo, Session.Revision, _status.Text,
            list.ToArray(), _recovery.Status, p?.State.Develop.Grading ?? new(), p?.State.Masks ?? [], Viewport.ComparisonPosition,
            new(_renderer.Statistics, _thumbnails.Statistics, _thumbnails.Renders, _cardBuilds, _libraryBuilds, _inspectorBuilds)));
    }
    private async void Run(Func<Task> operation) { try { await operation(); } catch (Exception error) { SetStatus(error.Message); } }
    public new void Dispose()
    {
        if (_disposed) return; _disposed = true; _recovery.Dispose(); _diagnosticsTimer.Stop(); _saveTimer.Stop(); _histogramTimer.Stop();
        Session.Changed -= Committed; Session.ViewChanged -= RefreshLive; Viewport.ViewChanged -= ViewportChanged;
        Viewport.Dispose(); _thumbnails.Dispose(); _renderer.Dispose();
    }
}
