using LightSpace.Core;
namespace LightSpace.Editing;

/// <summary>
/// Session-local frozen reference. Uses a separate render identity so future
/// edits to the active photo cannot replace the reference's shader. Source
/// bytes and normalized immutable edit data are shared, not serialized/copied.
/// </summary>
public sealed class ReferencePhotoSnapshot
{
    public Guid SourcePhotoId { get; }
    public PhotoDocument Photo { get; }
    private ReferencePhotoSnapshot(PhotoDocument source)
    {
        SourcePhotoId = source.Id;
        Photo = new PhotoDocument
        {
            Name = source.Name, Original = source.Original, Width = source.Width, Height = source.Height,
            ImportedAt = source.ImportedAt, State = source.State.Normalize(),
            Camera = source.Camera, Lens = source.Lens, ExposureInfo = source.ExposureInfo
        };
    }
    public static ReferencePhotoSnapshot Capture(PhotoDocument source)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (source.Original.Length == 0 || source.Width <= 0 || source.Height <= 0)
            throw new ArgumentException("A reference requires a valid original and image dimensions.", nameof(source));
        return new(source);
    }
}
