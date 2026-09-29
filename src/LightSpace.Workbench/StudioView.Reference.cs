namespace LightSpace.Workbench;

public sealed partial class StudioView
{
    private Grid? _detailHost, _referencePanes, _referenceHeader;
    private ReferencePhotoView? _referenceView;
    private TextBlock? _referenceTitle;
    private ReferencePhotoSnapshot? _referenceSnapshot;
    private CatalogDocument? _referenceCatalog;
    private bool _referenceOpen, _referenceStacked, _referenceLinked = true;
    private long _gridVisibilityCallback;

    private Grid CreateReferenceLayout()
    {
        var stage = new Grid();
        _detailHost = new Grid { RowDefinitions = { new() { Height = GridLength.Auto }, new() { Height = new(1, GridUnitType.Star) } } };
        _referenceHeader = new Grid
        {
            Background = Theme.Brush("#222222"), Padding = new(8, 3, 8, 3), Visibility = Visibility.Collapsed,
            ColumnDefinitions = { new() { Width = new(1, GridUnitType.Star) }, new() { Width = GridLength.Auto } }
        };
        _referenceTitle = Theme.Text("REFERENCE  /  ACTIVE", 10, true); _referenceHeader.Children.Add(_referenceTitle);
        var tools = Row(); tools.Spacing = 1;
        tools.Children.Add(Button("Pin active reference", Glyph.Photo, null, PinCurrentReference));
        tools.Children.Add(Button("Reference fit both", text: "Fit", action: () => { Viewport.Fit(); _referenceView?.Fit(); }));
        var linked = Button("Link reference navigation", Glyph.Link); linked.Selected = true;
        linked.Click += (_, _) => { _referenceLinked = !_referenceLinked; linked.Selected = _referenceLinked; if (_referenceLinked) LinkFromActive(Viewport.Navigation); PublishDiagnostics(); };
        tools.Children.Add(linked);
        tools.Children.Add(Button("Reference layout", Glyph.Grid, null, () => { _referenceStacked = !_referenceStacked; ArrangeReference(); }));
        tools.Children.Add(Button("Apply reference settings", Glyph.Edit, null, () => ShowSettingsDialog(true, _referenceSnapshot?.Photo.State, activeOnly: true)));
        tools.Children.Add(Button("Close reference", Glyph.Close, null, CloseReference));
        Grid.SetColumn(tools, 1); _referenceHeader.Children.Add(tools); _detailHost.Children.Add(_referenceHeader);
        _referencePanes = new Grid(); Grid.SetRow(_referencePanes, 1); _detailHost.Children.Add(_referencePanes);
        _referenceView = Register("reference-canvas", new ReferencePhotoView(_renderer) { Visibility = Visibility.Collapsed });
        _referencePanes.Children.Add(_referenceView); _referencePanes.Children.Add(Viewport); stage.Children.Add(_detailHost);
        _referenceView.NavigationChanged += LinkFromReference; _referenceView.RenderFailed += SetStatus;
        Viewport.NavigationChanged += LinkFromActive;
        Session.ViewChanged += RefreshReference; Session.Changed += RefreshReference;
        _gridVisibilityCallback = _gridScroll.RegisterPropertyChangedCallback(UIElement.VisibilityProperty, (_, _) =>
        {
            if (_detailHost is not null) _detailHost.Visibility = _gridScroll.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
        });
        ArrangeReference(); return stage;
    }
    private void ToggleReference()
    {
        if (_referenceOpen) { CloseReference(); return; }
        PinCurrentReference();
    }
    private void PinCurrentReference()
    {
        Session.CommitGesture("Adjustment");
        if (Session.Active is not { } photo || _referenceView is null) return;
        var previous = _referenceSnapshot;
        _referenceSnapshot = ReferencePhotoSnapshot.Capture(photo); _referenceCatalog = Session.Catalog;
        _referenceView.Photo = _referenceSnapshot.Photo;
        if (previous is not null) _renderer.ReleasePhoto(previous.Photo.Id);
        _referenceOpen = true; SetGrid(false); ArrangeReference(); Viewport.Fit(); _referenceView.Fit(); RefreshReference();
        SetStatus("Reference pinned. Choose another photo in the filmstrip, or edit the active photo against this frozen look.");
    }
    private void CloseReference()
    {
        _referenceOpen = false;
        if (_referenceView is not null) _referenceView.Photo = null;
        if (_referenceSnapshot is not null) _renderer.ReleasePhoto(_referenceSnapshot.Photo.Id);
        _referenceSnapshot = null; _referenceCatalog = null; ArrangeReference();
    }
    private void ArrangeReference()
    {
        if (_referenceHeader is null || _referencePanes is null || _referenceView is null) return;
        _referenceHeader.Visibility = _referenceView.Visibility = _referenceOpen ? Visibility.Visible : Visibility.Collapsed;
        _referencePanes.ColumnDefinitions.Clear(); _referencePanes.RowDefinitions.Clear();
        _referencePanes.ColumnSpacing = _referenceOpen && !_referenceStacked ? 1 : 0;
        _referencePanes.RowSpacing = _referenceOpen && _referenceStacked ? 1 : 0;
        if (_referenceOpen && _referenceStacked)
        {
            _referencePanes.RowDefinitions.Add(new() { Height = new(1, GridUnitType.Star) });
            _referencePanes.RowDefinitions.Add(new() { Height = new(1, GridUnitType.Star) });
            Grid.SetColumn(_referenceView, 0); Grid.SetRow(_referenceView, 0); Grid.SetColumn(Viewport, 0); Grid.SetRow(Viewport, 1);
        }
        else
        {
            _referencePanes.ColumnDefinitions.Add(new() { Width = _referenceOpen ? new(1, GridUnitType.Star) : new(0) });
            _referencePanes.ColumnDefinitions.Add(new() { Width = new(1, GridUnitType.Star) });
            Grid.SetColumn(_referenceView, 0); Grid.SetRow(_referenceView, 0); Grid.SetColumn(Viewport, 1); Grid.SetRow(Viewport, 0);
        }
        PublishDiagnostics();
    }
    private void RefreshReference()
    {
        if (!_referenceOpen || _disposed) return;
        if (!ReferenceEquals(_referenceCatalog, Session.Catalog)) { CloseReference(); return; }
        if (_referenceTitle is not null)
        {
            var title = $"REFERENCE: {_referenceSnapshot?.Photo.Name}   /   ACTIVE: {Session.Active?.Name}";
            if (_referenceTitle.Text != title) _referenceTitle.Text = title;
        }
    }
    private void LinkFromActive(PhotoNavigationState navigation)
    {
        if (_referenceOpen && _referenceLinked) _referenceView?.SetNavigation(navigation);
    }
    private void LinkFromReference(PhotoNavigationState navigation)
    {
        if (_referenceOpen && _referenceLinked) Viewport.SetNavigation(navigation);
        PublishDiagnostics();
    }
    private ReferenceDiagnostics ReferenceInfo() => new(_referenceOpen, _referenceStacked, _referenceLinked,
        _referenceSnapshot?.SourcePhotoId, _referenceSnapshot?.Photo.Name, _referenceSnapshot?.Photo.State.Develop.Exposure,
        Viewport.Navigation, _referenceView?.Navigation ?? PhotoNavigationState.Fit, _referenceView?.RenderCount ?? 0);
    private void DisposeReference()
    {
        Session.ViewChanged -= RefreshReference; Session.Changed -= RefreshReference;
        Viewport.NavigationChanged -= LinkFromActive;
        if (_referenceView is not null) { _referenceView.NavigationChanged -= LinkFromReference; _referenceView.RenderFailed -= SetStatus; }
        _gridScroll.UnregisterPropertyChangedCallback(UIElement.VisibilityProperty, _gridVisibilityCallback);
        CloseReference();
    }
}

public sealed record ReferenceDiagnostics(bool Visible, bool Stacked, bool Linked, Guid? SourceId, string? Name, float? Exposure,
    PhotoNavigationState ActiveNavigation, PhotoNavigationState ReferenceNavigation, long ReferenceDraws);
