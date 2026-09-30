using LightSpace.Core;
namespace LightSpace.Editing;

/// <summary>Session-only culling candidates. Exclusion never deletes a catalog photo or changes its flag.</summary>
public sealed class SurveySelection
{
    private Guid[] _candidates = [], _remaining = [];
    private readonly HashSet<Guid> _excluded = [];
    public int PageSize => SurveyLayout.MaximumVisiblePhotos;
    public int Page { get; private set; }
    public int PageCount => Math.Max(1, (_remaining.Length + PageSize - 1) / PageSize);
    public int Total => _candidates.Length;
    public int Remaining => _remaining.Length;
    public int Excluded => _excluded.Count;
    public Guid ActiveId { get; private set; }
    public IReadOnlyList<Guid> VisibleIds { get; private set; } = Array.Empty<Guid>();
    public event Action? Changed;

    public void Open(IEnumerable<Guid> ids, Guid preferred = default)
    {
        ArgumentNullException.ThrowIfNull(ids);
        var input = ids.Take(5001).ToArray();
        if (input.Length > 5000 || input.Contains(Guid.Empty)) throw new ArgumentException("Survey accepts up to 5000 valid photo identities.", nameof(ids));
        _candidates = input.Distinct().ToArray(); _remaining = _candidates; _excluded.Clear();
        ActiveId = _remaining.Contains(preferred) ? preferred : _remaining.FirstOrDefault();
        Page = Math.Max(0, Array.IndexOf(_remaining, ActiveId)) / PageSize;
        Publish();
    }
    public bool Activate(Guid id)
    {
        var index = Array.IndexOf(_remaining, id);
        if (index < 0) return false;
        if (ActiveId == id && Page == index / PageSize) return true;
        ActiveId = id; Page = index / PageSize; Publish(); return true;
    }
    public void MoveActive(int delta)
    {
        if (_remaining.Length == 0) return;
        var index = Array.IndexOf(_remaining, ActiveId);
        Activate(_remaining[(int)Math.Clamp((long)index + delta, 0, _remaining.Length - 1)]);
    }
    public void MovePage(int delta)
    {
        var page = (int)Math.Clamp((long)Page + delta, 0, PageCount - 1);
        if (page == Page || _remaining.Length == 0) return;
        Activate(_remaining[page * PageSize]);
    }
    public void Exclude(Guid id) => ExcludeWhere(candidate => candidate == id);
    public void ExcludeWhere(Func<Guid, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        var removed = _remaining.Where(predicate).ToArray(); // Stage before changing the view on predicate failure.
        if (removed.Length == 0) return;
        var oldIndex = Math.Max(0, Array.IndexOf(_remaining, ActiveId));
        _excluded.UnionWith(removed); _remaining = _candidates.Where(id => !_excluded.Contains(id)).ToArray();
        if (!_remaining.Contains(ActiveId)) ActiveId = _remaining.Length == 0 ? Guid.Empty : _remaining[Math.Min(oldIndex, _remaining.Length - 1)];
        Page = Math.Max(0, Array.IndexOf(_remaining, ActiveId)) / PageSize; Publish();
    }
    public void RestoreAll()
    {
        if (_excluded.Count == 0) return;
        _excluded.Clear(); _remaining = _candidates;
        if (ActiveId == Guid.Empty) ActiveId = _remaining.FirstOrDefault();
        Page = Math.Max(0, Array.IndexOf(_remaining, ActiveId)) / PageSize; Publish();
    }
    private void Publish()
    {
        VisibleIds = Array.AsReadOnly(_remaining.Skip(Page * PageSize).Take(PageSize).ToArray()); Changed?.Invoke();
    }
}
