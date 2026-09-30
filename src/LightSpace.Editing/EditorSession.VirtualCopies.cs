using LightSpace.Core;
namespace LightSpace.Editing;

public sealed partial class EditorSession
{
    private sealed record CopyNameEdit(Guid Id, string Before, string After);
    private sealed record PhotoSlot(PhotoDocument Photo, int Index);
    private sealed record AlbumSlot(Guid Album, Guid Photo, int Index);
    private sealed record SelectionState(Guid Active, Guid[] Selected)
    {
        public static SelectionState Capture(EditorSession s) => new(s.Catalog.ActivePhoto, s.Selection.ToArray());
        public void Restore(EditorSession s)
        {
            var ids = s.Catalog.Photos.Select(p => p.Id).ToHashSet();
            s.Catalog.ActivePhoto = ids.Contains(Active) ? Active : s.Catalog.Photos.FirstOrDefault()?.Id ?? Guid.Empty;
            s.Selection.Clear(); s.Selection.UnionWith(Selected.Where(ids.Contains));
            if (s.Selection.Count == 0 && s.Catalog.ActivePhoto != Guid.Empty) s.Selection.Add(s.Catalog.ActivePhoto);
        }
    }
    /// <summary>A reversible record delta, not a whole-catalog snapshot. Unrelated later imports/albums are preserved.</summary>
    private sealed class VirtualCopyEdit(bool creation, PhotoSlot[] photos, AlbumSlot[] albums, SelectionState before, SelectionState after)
    {
        private PhotoSlot[] _photos = photos;
        private AlbumSlot[] _albums = albums;
        public void Apply(EditorSession s, bool forward)
        {
            var insert = forward == creation;
            var ids = _photos.Select(p => p.Photo.Id).ToHashSet();
            var current = s.Catalog.Photos.ToDictionary(p => p.Id);
            if (insert)
            {
                if (current.Count + ids.Count > 5000 || ids.Any(current.ContainsKey))
                    throw new InvalidOperationException("The copy operation would duplicate an identity or exceed 5,000 photos.");
                foreach (var item in _photos)
                    if (item.Photo.MasterPhotoId is not Guid masterId || !current.TryGetValue(masterId, out var master) || master.IsVirtualCopy)
                        throw new InvalidOperationException("A virtual copy requires its original catalog record.");
                foreach (var item in _photos.OrderBy(p => p.Index)) s.Catalog.Photos.Insert(Math.Clamp(item.Index, 0, s.Catalog.Photos.Count), item.Photo);
                var albumMap = s.Catalog.Albums.ToDictionary(a => a.Id);
                foreach (var item in _albums.OrderBy(a => a.Index))
                    if (albumMap.TryGetValue(item.Album, out var album) && !album.Photos.Contains(item.Photo))
                        album.Photos.Insert(Math.Clamp(item.Index, 0, album.Photos.Count), item.Photo);
            }
            else
            {
                if (ids.Any(id => !current.TryGetValue(id, out var photo) || !photo.IsVirtualCopy))
                    throw new InvalidOperationException("Only existing virtual copies may be removed.");
                // Capture memberships at removal, including albums created after this history entry.
                _photos = s.Catalog.Photos.Select((p, i) => new PhotoSlot(p, i)).Where(p => ids.Contains(p.Photo.Id)).ToArray();
                _albums = s.Catalog.Albums.SelectMany(a => a.Photos.Select((id, i) => new AlbumSlot(a.Id, id, i)))
                    .Where(a => ids.Contains(a.Photo)).ToArray();
                s.Catalog.Photos.RemoveAll(p => ids.Contains(p.Id));
                foreach (var album in s.Catalog.Albums) album.Photos.RemoveAll(ids.Contains);
            }
            (forward ? after : before).Restore(s);
        }
    }

    /// <summary>Creates independently editable variants sharing immutable originals. All targets are validated before mutation.</summary>
    public IReadOnlyList<PhotoDocument> CreateVirtualCopies(IEnumerable<Guid> photoIds)
    {
        ArgumentNullException.ThrowIfNull(photoIds);
        var ids = photoIds.ToHashSet();
        var sources = Catalog.Photos.Where(p => ids.Contains(p.Id)).ToArray();
        if (sources.Length != ids.Count) throw new InvalidOperationException("A source photo is no longer in the catalog.");
        if (sources.Length == 0) return [];
        if (Catalog.Photos.Count + sources.Length > 5000) throw new InvalidOperationException("The catalog has reached its 5,000-photo safety limit.");
        var roots = Catalog.Photos.Where(p => !p.IsVirtualCopy).ToDictionary(p => p.Id);
        foreach (var source in sources)
        {
            var masterId = source.MasterPhotoId ?? source.Id;
            if (!roots.TryGetValue(masterId, out var master) || source.Original.Length == 0 ||
                !ReferenceEquals(source.Original, master.Original) || source.Width != master.Width || source.Height != master.Height)
                throw new InvalidOperationException("Virtual copies must reference an unchanged original and matching dimensions.");
        }
        CommitGesture("Adjustment");
        var before = SelectionState.Capture(this);
        var names = Catalog.Photos.Where(p => p.IsVirtualCopy).GroupBy(p => p.MasterPhotoId!.Value)
            .ToDictionary(g => g.Key, g => g.Select(p => p.CopyName).ToHashSet(StringComparer.OrdinalIgnoreCase));
        var copies = new List<PhotoDocument>(); var slots = new List<PhotoSlot>(); var memberships = new List<AlbumSlot>();
        var insertionOffset = 0; var positions = Catalog.Photos.Select((p, i) => (p.Id, i)).ToDictionary(p => p.Id, p => p.i);
        foreach (var source in sources)
        {
            var master = source.MasterPhotoId ?? source.Id;
            if (!names.TryGetValue(master, out var used)) names.Add(master, used = new(StringComparer.OrdinalIgnoreCase));
            var number = 1; while (used.Contains("Copy " + number)) number++;
            var name = "Copy " + number; used.Add(name);
            var copy = source.CopyRecord(); copy.Id = Guid.NewGuid(); copy.MasterPhotoId = master; copy.CopyName = name;
            copy.State = source.State.Normalize(); copy.Versions = []; copy.Revision = 0;
            // Keep the same import timestamp so stable import ordering retains adjacent variants.
            slots.Add(new(copy, positions[source.Id] + 1 + insertionOffset++)); copies.Add(copy);
        }
        var bySource = sources.Select((source, index) => (source.Id, Copy: copies[index])).ToDictionary(p => p.Id, p => p.Copy);
        foreach (var album in Catalog.Albums)
        {
            var offset = 0;
            for (var i = 0; i < album.Photos.Count; i++)
                if (bySource.TryGetValue(album.Photos[i], out var copy)) memberships.Add(new(album.Id, copy.Id, i + 1 + offset++));
        }
        var preferred = sources.Select((p, i) => (p.Id, Index: i)).FirstOrDefault(p => p.Id == before.Active).Index;
        var after = new SelectionState(copies[preferred].Id, copies.Select(p => p.Id).ToArray());
        var change = new VirtualCopyEdit(true, slots.ToArray(), memberships.ToArray(), before, after);
        change.Apply(this, true); Push(new("Create virtual copies", [], change)); Notify();
        return copies.AsReadOnly();
    }

    public void RenameVirtualCopy(Guid id, string name)
    {
        var normalized = VirtualCopyNames.Validate(name);
        var photo = Catalog.Photos.FirstOrDefault(p => p.Id == id);
        if (photo is null || !photo.IsVirtualCopy) throw new InvalidOperationException("Select a virtual copy to rename.");
        if (Catalog.Photos.Any(p => p.Id != id && p.MasterPhotoId == photo.MasterPhotoId && StringComparer.OrdinalIgnoreCase.Equals(p.CopyName, normalized)))
            throw new InvalidOperationException("Another copy of this original already uses that name.");
        if (photo.CopyName == normalized) return;
        CommitGesture("Adjustment"); var change = new CopyNameEdit(id, photo.CopyName, normalized);
        photo.CopyName = normalized; Push(new("Rename virtual copy", [], CopyName: change)); Notify();
    }

    /// <summary>Removes only copy records and their album links. Masters/original files are never deleted.</summary>
    public void RemoveVirtualCopies(IEnumerable<Guid> photoIds)
    {
        ArgumentNullException.ThrowIfNull(photoIds); var ids = photoIds.ToHashSet();
        var photos = Catalog.Photos.Select((p, i) => new PhotoSlot(p, i)).Where(p => ids.Contains(p.Photo.Id)).ToArray();
        if (photos.Length != ids.Count || photos.Any(p => !p.Photo.IsVirtualCopy))
            throw new InvalidOperationException("Only existing virtual copies may be removed. Original photographs are protected.");
        if (photos.Length == 0) return;
        CommitGesture("Adjustment"); var before = SelectionState.Capture(this);
        var active = ids.Contains(before.Active) ? photos.FirstOrDefault(p => p.Photo.Id == before.Active)!.Photo.MasterPhotoId!.Value : before.Active;
        var selected = before.Selected.Where(id => !ids.Contains(id)).ToArray();
        var after = new SelectionState(active, selected.Length > 0 ? selected : [active]);
        var change = new VirtualCopyEdit(false, photos, [], before, after);
        change.Apply(this, true); Push(new("Remove virtual copies", [], change)); Notify();
    }
}
