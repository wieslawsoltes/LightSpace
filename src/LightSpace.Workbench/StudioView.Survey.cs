namespace LightSpace.Workbench;

public sealed partial class StudioView
{
    private readonly SurveySelection _survey = new();
    private Dictionary<Guid, PhotoDocument> _surveyPhotos = [];
    private CatalogDocument? _surveyCatalog;
    private Grid? _surveyRoot;
    private PhotoSurveyView? _surveyView;
    private TextBlock? _surveyTitle;
    private LightButton? _surveyPrevious, _surveyNext, _surveyRestore;
    private bool _surveyMode, _refreshingSurvey;

    private void InitializeSurvey(Grid stage)
    {
        _surveyRoot = new Grid
        {
            Visibility = Visibility.Collapsed, Background = Theme.Background,
            RowDefinitions = { new() { Height = GridLength.Auto }, new() { Height = new(1, GridUnitType.Star) } }
        };
        var bar = new Grid
        {
            Padding = new(8, 4, 8, 4), Background = Theme.Brush("#222222"),
            ColumnDefinitions = { new() { Width = new(1, GridUnitType.Star) }, new() { Width = GridLength.Auto } }
        };
        _surveyTitle = Theme.Text("Survey", 11); bar.Children.Add(_surveyTitle);
        var actions = Row(); actions.Spacing = 2;
        actions.Children.Add(Button("Survey current selection", Glyph.Photo, null, OpenSurvey));
        actions.Children.Add(Button("Survey hide rejected", Glyph.Reject, null, () => _survey.ExcludeWhere(id => _surveyPhotos[id].State.Flag == PhotoFlag.Reject)));
        _surveyRestore = Button("Survey restore excluded", Glyph.Undo, null, _survey.RestoreAll); actions.Children.Add(_surveyRestore);
        actions.Children.Add(Button("Survey retry previews", Glyph.History, null, () => _surveyView?.RetryFailed()));
        actions.Children.Add(Button("Survey original", Glyph.Compare, null, () =>
        {
            if (_surveyView is null) return; _surveyView.Before = !_surveyView.Before;
            SetStatus(_surveyView.Before ? "Survey: source colors with crop and optical alignment retained." : "Survey: edited photographs.");
        }));
        _surveyPrevious = Button("Survey previous page", text: "‹", action: () => _survey.MovePage(-1)); actions.Children.Add(_surveyPrevious);
        _surveyNext = Button("Survey next page", text: "›", action: () => _survey.MovePage(1)); actions.Children.Add(_surveyNext);
        actions.Children.Add(Button("Survey done", text: "Done", action: () => SetGrid(false)));
        Grid.SetColumn(actions, 1); bar.Children.Add(actions); _surveyRoot.Children.Add(bar);
        _surveyView = Register("survey-canvas", new PhotoSurveyView()); Grid.SetRow(_surveyView, 1); _surveyRoot.Children.Add(_surveyView);
        _surveyView.Activated += id => _survey.Activate(id);
        _surveyView.OpenRequested += OpenSurveyPhoto;
        _surveyView.ExcludeRequested += _survey.Exclude;
        _surveyView.RatingRequested += id => EditSurveyPhoto(id, "Survey rating", state => state with { Rating = (state.Rating + 1) % 6 });
        _surveyView.FlagRequested += (id, flag) => EditSurveyPhoto(id, "Survey flag", state => state with { Flag = state.Flag == flag ? PhotoFlag.None : flag });
        _surveyView.Status += SetStatus; _survey.Changed += RefreshSurvey;
        stage.Children.Add(_surveyRoot);
    }
    private void OpenSurvey()
    {
        Session.CommitGesture("Adjustment");
        if (Session.Catalog.Photos.Count == 0) { SetStatus("Import photographs before opening Survey."); return; }
        var selected = Session.Catalog.Photos.Where(p => Session.Selection.Contains(p.Id)).ToArray();
        var candidates = selected.Length >= 2 ? selected : _visible.ToArray();
        if (candidates.Length == 0) { SetStatus("No photographs match the current filter."); return; }
        SetGrid(false); CloseReference();
        _surveyCatalog = Session.Catalog; _surveyPhotos = candidates.ToDictionary(p => p.Id);
        _surveyMode = true; _histogramTimer.Stop();
        if (_detailHost is not null) _detailHost.Visibility = Visibility.Collapsed;
        _gridScroll.Visibility = Viewport.Visibility = Visibility.Collapsed;
        _surveyRoot!.Visibility = Visibility.Visible; _surveyView!.Before = false; _surveyView.SetActive(true);
        _survey.Open(candidates.Select(p => p.Id), Session.Catalog.ActivePhoto);
        SetSurveyChrome(true); Resize();
        SetStatus(selected.Length >= 2
            ? "Survey: click a photo, use 0–5 or P/X/U, and exclude with Delete. No source files are deleted."
            : "Surveying filtered photos because fewer than two were selected. Exclusions affect only this survey.");
    }
    private void ExitSurvey()
    {
        if (!_surveyMode) return;
        _surveyMode = false; _surveyRoot!.Visibility = Visibility.Collapsed; _surveyView!.SetActive(false); _surveyView.SetPhotos([], Guid.Empty);
        _survey.Open([]); _surveyPhotos.Clear(); _surveyCatalog = null;
        if (_detailHost is not null) _detailHost.Visibility = Visibility.Visible;
        _gridMode = false; _gridScroll.Visibility = Visibility.Collapsed; Viewport.Visibility = Visibility.Visible;
        _displayedPhoto = Guid.Empty; SetSurveyChrome(false);
        // Fallback surveys can activate a candidate outside the original selection.
        // Never return to Detail with a hidden, unrelated metadata-edit target.
        if (Session.Active is { } active && !Session.Selection.Contains(active.Id)) Session.Select(active.Id);
        Resize(); RefreshLive();
    }
    private void SetSurveyChrome(bool surveying)
    {
        var visibility = surveying ? Visibility.Collapsed : Visibility.Visible;
        if (_widgets.TryGetValue("inspector-scroll", out var reference) && reference.TryGetTarget(out var inspector)) inspector.Visibility = visibility;
        if (_centerLayout is not null)
            foreach (var child in _centerLayout.Children.OfType<FrameworkElement>())
                if (Grid.GetRow(child) == 2) child.Visibility = visibility;
    }
    private void RefreshSurvey()
    {
        if (!_surveyMode || _refreshingSurvey || _surveyView is null || _disposed) return;
        _refreshingSurvey = true;
        try
        {
            if (!ReferenceEquals(_surveyCatalog, Session.Catalog)) { ExitSurvey(); return; }
            if (_survey.ActiveId != Guid.Empty && _survey.ActiveId != Session.Catalog.ActivePhoto) Session.ActivatePhoto(_survey.ActiveId);
            var photos = _survey.VisibleIds.Select(id => _surveyPhotos[id]).ToArray();
            _surveyView.SetPhotos(photos, _survey.ActiveId);
            foreach (var key in _widgets.Keys.Where(k => k.StartsWith("survey-photo-") || k.StartsWith("survey-rating-") || k.StartsWith("survey-pick-") || k.StartsWith("survey-reject-") || k.StartsWith("survey-exclude-")).ToArray()) _widgets.Remove(key);
            foreach (var (id, widget) in _surveyView.Widgets) Register(id, widget);
            _surveyTitle!.Text = $"Survey · {_survey.Remaining}/{_survey.Total} · {_survey.Page + 1}/{_survey.PageCount}";
            _surveyPrevious!.IsEnabled = _survey.Page > 0; _surveyNext!.IsEnabled = _survey.Page + 1 < _survey.PageCount;
            _surveyRestore!.IsEnabled = _survey.Excluded > 0;
        }
        finally { _refreshingSurvey = false; }
        PublishDiagnostics();
    }
    private void EditSurveyPhoto(Guid id, string name, Func<PhotoState, PhotoState> edit)
    {
        if (!_surveyMode || !_survey.Activate(id)) return;
        Session.EditPhoto(id, name, edit);
    }
    private void OpenSurveyPhoto(Guid id)
    {
        if (!_survey.Activate(id)) return;
        SetGrid(false); Session.ActivatePhoto(id);
    }
    private bool SurveyKeyboard(VirtualKey key)
    {
        if (!_surveyMode) return false;
        if (key >= VirtualKey.Number0 && key <= VirtualKey.Number5)
        {
            var rating = (int)key - (int)VirtualKey.Number0;
            EditSurveyPhoto(_survey.ActiveId, "Survey rating", state => state with { Rating = rating }); return true;
        }
        switch (key)
        {
            case VirtualKey.P: EditSurveyPhoto(_survey.ActiveId, "Survey pick", s => s with { Flag = PhotoFlag.Pick }); break;
            case VirtualKey.X: EditSurveyPhoto(_survey.ActiveId, "Survey reject", s => s with { Flag = PhotoFlag.Reject }); break;
            case VirtualKey.U: EditSurveyPhoto(_survey.ActiveId, "Survey clear flag", s => s with { Flag = PhotoFlag.None }); break;
            case VirtualKey.Left: case VirtualKey.Up: _survey.MoveActive(-1); break;
            case VirtualKey.Right: case VirtualKey.Down: _survey.MoveActive(1); break;
            case VirtualKey.PageUp: _survey.MovePage(-1); break;
            case VirtualKey.PageDown: _survey.MovePage(1); break;
            case VirtualKey.Delete: case VirtualKey.Back: _survey.Exclude(_survey.ActiveId); break;
            case VirtualKey.Z: if (_survey.ActiveId != Guid.Empty) { OpenSurveyPhoto(_survey.ActiveId); Viewport.ToggleZoom(); } break;
            case VirtualKey.Y: if (_survey.ActiveId != Guid.Empty) { OpenSurveyPhoto(_survey.ActiveId); Viewport.Compare = true; Viewport.Invalidate(); } break;
            case VirtualKey.Enter: if (_survey.ActiveId != Guid.Empty) OpenSurveyPhoto(_survey.ActiveId); break;
            case VirtualKey.Escape: SetGrid(false); break;
            default:
                if ((int)key == 220 && _surveyView is not null) { _surveyView.Before = !_surveyView.Before; return true; }
                return false;
        }
        return true;
    }
    private SurveyDiagnostics SurveyInfo() => new(_surveyMode, _survey.Total, _survey.Remaining, _survey.Excluded, _survey.Page,
        _survey.PageCount, _survey.ActiveId, _survey.VisibleIds.ToArray(), _surveyView?.ReadyCount ?? 0, _surveyView?.FailedCount ?? 0,
        _surveyView?.PreparationSteps ?? 0, _surveyView?.CardBuilds ?? 0, _surveyView?.LayoutBuilds ?? 0,
        _surveyView?.Statistics ?? new(0, 0, 0, 0, 0, 0));
    private void DisposeSurvey() { _survey.Changed -= RefreshSurvey; _surveyView?.Dispose(); }
}
