using LightSpace.Core;
namespace LightSpace.Catalog;

public enum PhotoSort { ImportOrder, Name, Rating, RecentlyEdited }
public sealed record PhotoQuery(string Text = "", int MinimumRating = 0, PhotoFlag? Flag = null, Guid? Album = null, PhotoSort Sort = PhotoSort.ImportOrder)
{
    public IReadOnlyList<PhotoDocument> Execute(CatalogDocument catalog)
    {
        IEnumerable<PhotoDocument> photos = catalog.Photos;
        if (Album is Guid id)
        {
            var ids = catalog.Albums.FirstOrDefault(a => a.Id == id)?.Photos.ToHashSet() ?? [];
            photos = photos.Where(p => ids.Contains(p.Id));
        }
        var tokens = Text.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        photos = photos.Where(p => p.State.Rating >= MinimumRating && (Flag is null || p.State.Flag == Flag) && tokens.All(t =>
            p.Name.Contains(t, StringComparison.OrdinalIgnoreCase) || p.State.Caption.Contains(t, StringComparison.OrdinalIgnoreCase) ||
            p.State.Keywords.Any(k => k.Contains(t, StringComparison.OrdinalIgnoreCase))));
        return (Sort switch
        {
            PhotoSort.Name => photos.OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase),
            PhotoSort.Rating => photos.OrderByDescending(p => p.State.Rating).ThenBy(p => p.ImportedAt),
            PhotoSort.RecentlyEdited => photos.OrderByDescending(p => p.Revision),
            _ => photos.OrderBy(p => p.ImportedAt)
        }).ToArray();
    }
}
