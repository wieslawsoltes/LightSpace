namespace LightSpace.Workbench;

public sealed partial class StudioView
{
    private HashSet<Guid> _knownCatalogIds = [];
    private CatalogDocument? _knownCatalog;
    private int _knownCatalogCount = -1;
    private const int CopyFamilyPageSize = 6;
    private Guid _copyFamilyActive;
    private int _copyFamilyPage;

    private void OpenVirtualCopies()
    {
        _focusMode = false; SetGrid(false); Viewport.SetTool(PhotoTool.Edit); Resize(); ShowInspector("Virtual copies");
    }
    private void CreateCopies(bool selected = false)
    {
        if (Session.Active is not { } active) return;
        var ids = selected ? Session.Selection.ToArray() : new[] { active.Id };
        SetGrid(false); _query = new(); _search.Text = ""; _page = 0;
        try
        {
            var created = Session.CreateVirtualCopies(ids);
            _page = Math.Max(0, Session.Catalog.Photos.FindIndex(p => p.Id == Session.Catalog.ActivePhoto)) / PageSize;
            RefreshCatalog(); OpenVirtualCopies();
            SetStatus($"Created {created.Count} virtual cop{(created.Count == 1 ? "y" : "ies")}. Original bytes are shared; edits are independent.");
        }
        catch (Exception error) when (error is InvalidOperationException or ArgumentException) { SetStatus(error.Message); }
    }
    private void BuildVirtualCopies(PhotoDocument active)
    {
        var root = active.MasterPhotoId ?? active.Id;
        var panel = new StackPanel { Spacing = 8, Margin = new(18, 0, 18, 18) };
        panel.Children.Add(Note("Explore another look without duplicating the original file. Each virtual copy has independent processing, metadata and named versions."));
        panel.Children.Add(Button("Create virtual copy", Glyph.Add, "Create virtual copy", () => CreateCopies()));
        panel.Children.Add(Button("Create selected virtual copies", Glyph.Photo, "Create for selection", () => CreateCopies(true)));
        panel.Children.Add(Theme.Divider()); panel.Children.Add(Theme.Text("THIS ORIGINAL", 10, true));
        var family = Session.Catalog.Photos.Where(p => p.Id == root || p.MasterPhotoId == root).ToArray();
        if (_copyFamilyActive != active.Id)
        {
            _copyFamilyActive = active.Id;
            _copyFamilyPage = Math.Max(0, Array.FindIndex(family, p => p.Id == active.Id)) / CopyFamilyPageSize;
        }
        _copyFamilyPage = Math.Clamp(_copyFamilyPage, 0, Math.Max(0, (family.Length - 1) / CopyFamilyPageSize));
        if (family.Length > CopyFamilyPageSize)
        {
            var pages = Row();
            pages.Children.Add(Button("Previous copy page", text: "‹", action: () => { _copyFamilyPage = Math.Max(0, _copyFamilyPage - 1); BuildInspector(); }));
            pages.Children.Add(Theme.Text($"{_copyFamilyPage + 1} / {(family.Length + CopyFamilyPageSize - 1) / CopyFamilyPageSize}", 11, true));
            pages.Children.Add(Button("Next copy page", text: "›", action: () => { _copyFamilyPage++; BuildInspector(); }));
            panel.Children.Add(pages);
        }
        foreach (var photo in family.Skip(_copyFamilyPage * CopyFamilyPageSize).Take(CopyFamilyPageSize))
        {
            var id = photo.Id;
            var button = Button("copy-family-" + id, Glyph.Photo, photo.IsVirtualCopy ? photo.CopyName : "Original", () =>
            {
                _query = new(); _search.Text = ""; _page = Math.Max(0, Session.Catalog.Photos.FindIndex(p => p.Id == id)) / PageSize;
                Session.Select(id); RefreshCatalog();
            });
            button.Selected = id == active.Id; button.HorizontalAlignment = HorizontalAlignment.Stretch; button.HorizontalContentAlignment = HorizontalAlignment.Left;
            ToolTipService.SetToolTip(button, photo.DisplayName); panel.Children.Add(button);
        }
        if (active.IsVirtualCopy)
        {
            var input = Register("Virtual copy name", Theme.Input("Copy name", "Virtual copy name", active.CopyName));
            input.MaxLength = VirtualCopyNames.MaximumLength; panel.Children.Add(input);
            var catalog = Session.Catalog; var id = active.Id;
            panel.Children.Add(Button("Save copy name", Glyph.Check, "Rename copy", () =>
            {
                try
                {
                    if (!ReferenceEquals(catalog, Session.Catalog) || Session.Active?.Id != id) throw new InvalidOperationException("The active copy changed. Reopen its name editor.");
                    Session.RenameVirtualCopy(id, input.Text); SetStatus("Virtual copy renamed; pixel caches retained.");
                }
                catch (Exception error) when (error is InvalidOperationException or ArgumentException) { SetStatus(error.Message); }
            }));
            panel.Children.Add(Button("Remove virtual copy", Glyph.Trash, "Remove this copy…", () => ConfirmRemoveCopies([id])));
        }
        panel.Children.Add(Theme.Divider());
        panel.Children.Add(Button("Compare copy family", Glyph.Compare, "Survey this original", () =>
        {
            Session.Selection.Clear(); Session.Selection.UnionWith(Session.Catalog.Photos.Where(p => p.Id == root || p.MasterPhotoId == root).Select(p => p.Id));
            RefreshCatalog(); OpenSurvey();
        }));
        panel.Children.Add(Note("Removal deletes only the copy record and its album links, never the original. Create, rename and remove are undoable. Portable backups retain the family with one original payload."));
        _inspector.Children.Add(panel);
    }
    private void ConfirmRemoveCopies(Guid[] ids)
    {
        var catalog = Session.Catalog;
        ShowDialog("Remove virtual copy?", Note("Only this virtual copy and its album references will be removed. The original photograph and its other copies remain. Undo restores the copy and its edit settings."), "Remove copy", () =>
        {
            if (!ReferenceEquals(catalog, Session.Catalog)) throw new InvalidOperationException("The catalog changed. Review the removal again.");
            Session.RemoveVirtualCopies(ids); SetStatus("Virtual copy removed. Original retained; Undo restores the copy.");
            return Task.CompletedTask;
        });
    }
    private void TrackCatalogRecords()
    {
        if (ReferenceEquals(_knownCatalog, Session.Catalog) && _knownCatalogCount == Session.Catalog.Photos.Count) return;
        var next = Session.Catalog.Photos.Select(p => p.Id).ToHashSet();
        var removed = _knownCatalogIds.Where(id => !next.Contains(id)).ToArray();
        _knownCatalog = Session.Catalog; _knownCatalogCount = Session.Catalog.Photos.Count; _knownCatalogIds = next;
        foreach (var id in removed) { _renderer.ReleasePhoto(id); _thumbnails.ReleasePhoto(id); }
        if (_surveyMode && removed.Any(_surveyPhotos.ContainsKey)) ExitSurvey();
    }
    private static string ExportStem(PhotoDocument photo)
    {
        var stem = SafeName(photo.Name);
        if (!photo.IsVirtualCopy) return stem;
        var label = new string(photo.CopyName.Where(c => char.IsLetterOrDigit(c) || c is '-' or '_' or ' ').Take(70).ToArray());
        return $"{stem}-{(string.IsNullOrWhiteSpace(label) ? "copy" : label)}-{photo.Id.ToString("N")[..8]}";
    }
    private VirtualCopyDiagnostics CopiesInfo()
    {
        var active = Session.Active; var root = active?.MasterPhotoId ?? active?.Id;
        return new(active?.Id, active?.MasterPhotoId, active?.CopyName ?? "", _query.Kind.ToString(),
            _visible.Skip(_page * PageSize).Take(PageSize).Select(p => p.Id).ToArray(), Session.Selection.Take(5000).ToArray(),
            root is null ? [] : Session.Catalog.Photos.Where(p => p.Id == root || p.MasterPhotoId == root).Take(PageSize)
                .Select(p => new CopyFamilyItem(p.Id, p.CopyName, p.DisplayName, p.IsVirtualCopy, p.State.Develop.Exposure)).ToArray());
    }
}
