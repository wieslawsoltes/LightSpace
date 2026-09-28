namespace LightSpace.Workbench;

public sealed partial class StudioView
{
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
}
