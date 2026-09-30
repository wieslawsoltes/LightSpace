using LightSpace.Core;
using LightSpace.Catalog;
namespace LightSpace.Editing;

public sealed partial class EditorSession
{
    private sealed record Change(Guid Id, PhotoState Before, PhotoState After);
    private sealed record Transaction(string Name, Change[] Changes);
    private readonly List<Transaction> _undo = [];
    private readonly Stack<Transaction> _redo = [];
    private PhotoState? _gestureBefore;
    private Guid _gesturePhoto;
    public CatalogDocument Catalog { get; private set; }
    public PhotoDocument? Active => Catalog.Photos.FirstOrDefault(p => p.Id == Catalog.ActivePhoto);
    public HashSet<Guid> Selection { get; } = [];
    public long Revision { get; private set; }
    public bool HasActiveGesture => _gestureBefore is not null;
    public bool CanUndo => _undo.Count > 0;
    public bool CanRedo => _redo.Count > 0;
    public IReadOnlyList<string> History => _undo.Select(t => t.Name).Reverse().ToArray();
    public event Action? Changed;
    public event Action? ViewChanged;
    public EditorSession(CatalogDocument? catalog = null) { Catalog = catalog ?? new(); if (Active is { } p) Selection.Add(p.Id); }
    public void Load(CatalogDocument catalog)
    {
        CancelGesture(); Catalog = catalog; _undo.Clear(); _redo.Clear(); Selection.Clear();
        if (Active is { } p) Selection.Add(p.Id); Revision++; Changed?.Invoke();
    }
    public void Select(Guid id, bool extend = false)
    {
        CommitGesture("Adjustment"); if (!Catalog.Photos.Any(p => p.Id == id)) return;
        Catalog.ActivePhoto = id; if (!extend) Selection.Clear();
        if (extend && Selection.Contains(id) && Selection.Count > 1) Selection.Remove(id); else Selection.Add(id);
        ViewChanged?.Invoke();
    }
    public void Add(PhotoDocument photo)
    {
        if (Catalog.Photos.Any(p => p.Id == photo.Id)) throw new InvalidOperationException("Photo already in catalog.");
        CommitGesture("Adjustment"); Catalog.Photos.Add(photo); Catalog.ActivePhoto = photo.Id; Selection.Clear(); Selection.Add(photo.Id); Notify();
    }
    public void BeginGesture()
    {
        if (_gestureBefore is not null || Active is not { } photo) return;
        _gesturePhoto = photo.Id; _gestureBefore = photo.State; ViewChanged?.Invoke();
    }
    public void Preview(Func<PhotoState, PhotoState> edit)
    {
        if (Active is not { } photo) return;
        BeginGesture(); var next = edit(photo.State).Normalize();
        if (PhotoStateEquality.All(photo.State, next)) return;
        photo.State = next; photo.Revision++; ViewChanged?.Invoke();
    }
    public void CommitGesture(string name)
    {
        if (_gestureBefore is null) return;
        var before = _gestureBefore; _gestureBefore = null;
        var photo = Catalog.Photos.FirstOrDefault(p => p.Id == _gesturePhoto);
        if (photo is not null && !PhotoStateEquality.All(before, photo.State))
        { Push(new(name, [new(photo.Id, before, photo.State)])); Notify(); }
        else ViewChanged?.Invoke();
    }
    public void CancelGesture()
    {
        if (_gestureBefore is null) return;
        var photo = Catalog.Photos.FirstOrDefault(p => p.Id == _gesturePhoto);
        if (photo is not null) { photo.State = _gestureBefore; photo.Revision++; }
        _gestureBefore = null; ViewChanged?.Invoke();
    }
    public void Edit(string name, Func<PhotoState, PhotoState> edit, bool selected = false)
    {
        CommitGesture("Adjustment");
        var targets = selected ? Catalog.Photos.Where(p => Selection.Contains(p.Id)).ToArray() : Active is { } active ? [active] : Array.Empty<PhotoDocument>();
        ApplyTransaction(name, targets, edit);
    }
    private void ApplyTransaction(string name, PhotoDocument[] targets, Func<PhotoState, PhotoState> edit)
    {
        // Stage every result before mutating any photo. Indexed targets avoid an
        // O(catalog size * target count) lookup loop for batch synchronization.
        var staged = targets.Select(photo => (Photo: photo, After: edit(photo.State).Normalize()))
            .Where(item => !PhotoStateEquality.All(item.Photo.State, item.After)).ToArray();
        if (staged.Length == 0) return;
        var changes = staged.Select(item => new Change(item.Photo.Id, item.Photo.State, item.After)).ToArray();
        foreach (var item in staged) { item.Photo.State = item.After; item.Photo.Revision++; }
        Push(new(name, changes)); Notify();
    }
    public void ApplySettings(EditSettingsTransfer transfer, IEnumerable<Guid> photoIds)
    {
        ArgumentNullException.ThrowIfNull(transfer); ArgumentNullException.ThrowIfNull(photoIds);
        var ids = photoIds.ToHashSet();
        var targets = Catalog.Photos.Where(p => ids.Contains(p.Id)).ToArray();
        if (targets.Length != ids.Count) throw new InvalidOperationException("A target photo is no longer in this catalog. Review the selection again.");
        if (ids.Count == 0 || transfer.Groups == EditSettingsGroup.None) return;
        CommitGesture("Adjustment"); ApplyTransaction("Apply selected edit settings", targets, transfer.Apply);
    }
    private void Push(Transaction transaction) { _undo.Add(transaction); if (_undo.Count > 100) _undo.RemoveAt(0); _redo.Clear(); }
    public void Undo()
    {
        CancelGesture(); if (!CanUndo) return;
        var t = _undo[^1]; _undo.RemoveAt(_undo.Count - 1); Apply(t, false); _redo.Push(t); Notify();
    }
    public void Redo()
    {
        CancelGesture(); if (!_redo.TryPop(out var t)) return; Apply(t, true); _undo.Add(t); Notify();
    }
    private void Apply(Transaction t, bool forward)
    {
        var targets = Catalog.Photos.ToDictionary(p => p.Id);
        foreach (var c in t.Changes) if (targets.TryGetValue(c.Id, out var p)) { p.State = forward ? c.After : c.Before; p.Revision++; }
    }
    public void SyncSelected() => SyncSelected(EditSettingsGroup.All);
    public void SyncSelected(EditSettingsGroup groups)
    {
        CommitGesture("Adjustment");
        if (Active is not { } p) return;
        ApplySettings(new(p.State, groups), Selection);
    }
    public void SaveVersion(string name)
    {
        if (Active is not { } p || string.IsNullOrWhiteSpace(name)) return;
        CommitGesture("Adjustment"); p.Versions.Add(new(name.Trim(), p.State, DateTimeOffset.UtcNow));
        if (p.Versions.Count > 100) p.Versions.RemoveAt(0); Notify();
    }
    public Album CreateAlbum(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Album name is required.");
        var album = new Album { Name = name.Trim(), Photos = Selection.ToList() }; Catalog.Albums.Add(album); Notify(); return album;
    }
    public void AddSelectionToAlbum(Guid id)
    {
        var album = Catalog.Albums.First(a => a.Id == id); album.Photos = album.Photos.Concat(Selection).Distinct().ToList(); Notify();
    }
    public CommittedCatalogSnapshot CaptureCommittedSnapshot() => new(Revision, CatalogSerializer.Serialize(CopyCommittedCatalog()));
    public CatalogDocument CopyCommittedCatalog() => new()
    {
        SchemaVersion = Catalog.SchemaVersion, ActivePhoto = Catalog.ActivePhoto,
        Albums = Catalog.Albums.Select(album => new Album { Id = album.Id, Name = album.Name, Photos = [.. album.Photos] }).ToList(),
        Photos = Catalog.Photos.Select(photo => new PhotoDocument
        {
            Id = photo.Id, Name = photo.Name, Original = photo.Original, Width = photo.Width, Height = photo.Height, ImportedAt = photo.ImportedAt,
            Camera = photo.Camera, Lens = photo.Lens, ExposureInfo = photo.ExposureInfo,
            State = _gestureBefore is not null && photo.Id == _gesturePhoto ? _gestureBefore : photo.State, Versions = [.. photo.Versions]
        }).ToList()
    };
    public void Notify() { Revision++; Changed?.Invoke(); ViewChanged?.Invoke(); }
}
