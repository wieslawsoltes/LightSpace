using LightSpace.Core;
namespace LightSpace.Rendering.Skia;

public sealed partial class PhotoRenderer
{
    /// <summary>
    /// Prepare one photo on the renderer owner thread before its first draw.
    /// This is synchronous CPU/native preparation, not a background or GPU decode.
    /// A host can amortize a contact sheet by calling once per dispatcher tick.
    /// </summary>
    public void Prepare(PhotoDocument photo)
    {
        ArgumentNullException.ThrowIfNull(photo);
        var cache = GetImage(photo);
        var development = GetShader(cache, photo, -1);
        _ = cache.Edited.Get(cache.Image, development, photo.State.Optics, ClippingIndicators.None, CreatePresentation);
        _ = GetGeometry(cache, photo.State);
    }
}
