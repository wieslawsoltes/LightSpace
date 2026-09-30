namespace LightSpace.Workbench;

public sealed partial class StudioView
{
    private void BuildRails()
    {
        var rail = new Grid { Background = Theme.Brush("#1b1b1b"), RowDefinitions = { new() { Height = new(1, GridUnitType.Star) }, new() { Height = GridLength.Auto } } };
        var top = new StackPanel { Spacing = 8, Margin = new(3, 12, 3, 0) };
        top.Children.Add(Button("Add photos", Glyph.Add, null, () => Run(ImportAsync)));
        top.Children.Add(Button("Grid view", Glyph.Grid, null, () => SetGrid(true)));
        top.Children.Add(Button("Detail view", Glyph.Photo, null, () => SetGrid(false)));
        top.Children.Add(Button("Manage virtual copies", Glyph.Photo, null, OpenVirtualCopies));
        top.Children.Add(Button("Survey view", Glyph.Grid, null, OpenSurvey));
        top.Children.Add(Button("Reference view", Glyph.Compare, null, ToggleReference));
        top.Children.Add(Button("Copy settings", Glyph.Edit, null, () => ShowSettingsDialog(false)));
        top.Children.Add(Button("Paste selected settings", Glyph.Check, null, PasteSelectedSettings));
        top.Children.Add(Button("Synchronize settings", Glyph.Link, null, () => ShowSettingsDialog(true)));
        rail.Children.Add(top);
        var bottom = new StackPanel { Spacing = 8, Margin = new(3, 0, 3, 12) };
        bottom.Children.Add(Button("Help", Glyph.Help, null, () => Run(ShowHelpAsync))); bottom.Children.Add(Button("Local storage", Glyph.Info, null, () => SetStatus("Local-only catalog. Export a catalog backup to retain originals and edits."))); Grid.SetRow(bottom, 1); rail.Children.Add(bottom); _body.Children.Add(rail);
        var tools = new StackPanel { Background = Theme.Brush("#202020"), Spacing = 9, Padding = new(3, 12, 3, 0) };
        foreach (var (tool, glyph, name) in new[] { (PhotoTool.Edit, Glyph.Edit, "Edit photo"), (PhotoTool.Crop, Glyph.Crop, "Crop photo"), (PhotoTool.Clone, Glyph.Clone, "Clone tool"), (PhotoTool.RadialMask, Glyph.Mask, "Masking") })
        { var button = Button(name, glyph, null, () => ChooseTool(tool)); button.Padding = new(8); _tools[tool] = button; tools.Children.Add(button); }
        tools.Children.Add(Theme.Divider());
        void Open(string panel) { _focusMode = false; Resize(); SetGrid(false); Viewport.SetTool(PhotoTool.Edit); ShowInspector(panel); }
        tools.Children.Add(Button("Optics panel", Glyph.Photo, null, () => Open("Optics")));
        tools.Children.Add(Button("Geometry panel", Glyph.Rotate, null, () => Open("Geometry")));
        tools.Children.Add(Button("Presets", Glyph.Presets, null, () => ShowInspector("Presets")));
        tools.Children.Add(Button("Versions and history", Glyph.History, null, () => ShowInspector("History"))); tools.Children.Add(Button("Photo information", Glyph.Info, null, () => ShowInspector("Info")));
        Grid.SetColumn(tools, 4); _body.Children.Add(tools);
    }
    private UIElement BuildCenter()
    {
        var center = new Grid { RowDefinitions = { new() { Height = new(42) }, new() { Height = new(1, GridUnitType.Star) }, new() { Height = new(39) }, new() { Height = new(116) }, new() { Height = new(25) } } };
        _centerLayout = center;
        var breadcrumb = new Grid { Padding = new(18, 0, 14, 0), Background = Theme.Brush("#1c1c1c"), ColumnDefinitions = { new() { Width = new(1, GridUnitType.Star) }, new() { Width = GridLength.Auto } } }; breadcrumb.Children.Add(_title);
        var pages = Row(); pages.Spacing = 2;
        pages.Children.Add(Button("Toggle filmstrip", Glyph.Photo, null, ToggleFilmstrip)); pages.Children.Add(Button("Focus mode", Glyph.Compare, null, () => ToggleFocusMode()));
        pages.Children.Add(Button("Previous page", text: "‹", action: () => { if (_page > 0) { _page--; RefreshCatalog(); } }));
        pages.Children.Add(Button("Next page", text: "›", action: () => { if ((_page + 1) * PageSize < _visible.Count) { _page++; RefreshCatalog(); } })); Grid.SetColumn(pages, 1); breadcrumb.Children.Add(pages); center.Children.Add(breadcrumb);
        var photo = CreateReferenceLayout(); photo.Children.Add(_gridScroll); InitializeSurvey(photo); Grid.SetRow(photo, 1); center.Children.Add(photo);
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
}
