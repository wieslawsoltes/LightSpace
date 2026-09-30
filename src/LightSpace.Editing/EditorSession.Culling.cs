using LightSpace.Core;
namespace LightSpace.Editing;

public sealed partial class EditorSession
{
    /// <summary>Change the active photo without toggling the multi-photo selection or committing a catalog revision.</summary>
    public void ActivatePhoto(Guid id)
    {
        if (!Catalog.Photos.Any(p => p.Id == id)) throw new InvalidOperationException("The photo is no longer in this catalog.");
        CommitGesture("Adjustment");
        if (Catalog.ActivePhoto == id) return;
        Catalog.ActivePhoto = id; ViewChanged?.Invoke();
    }
    /// <summary>Target one photograph explicitly. A survey rating must not rate every selected candidate.</summary>
    public void EditPhoto(Guid id, string name, Func<PhotoState, PhotoState> edit)
    {
        ArgumentNullException.ThrowIfNull(edit);
        var photo = Catalog.Photos.FirstOrDefault(p => p.Id == id) ?? throw new InvalidOperationException("The photo is no longer in this catalog.");
        CommitGesture("Adjustment"); ApplyTransaction(name, [photo], edit);
    }
}
