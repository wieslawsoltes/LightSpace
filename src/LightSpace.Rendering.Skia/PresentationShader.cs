using LightSpace.Core;
using SkiaSharp;
namespace LightSpace.Rendering.Skia;

[Flags]
public enum ClippingIndicators { None = 0, Shadows = 1, Highlights = 2, Both = Shadows | Highlights }
/// <summary>Actual completed construction counts, not GPU timings or inferred readback measurements.</summary>
public sealed record PresentationStatistics(long ShaderBuilds, long GeometryBuilds);

internal sealed class PresentationShader : IDisposable
{
    private SKShader? _shader, _input;
    private LensCorrectionSettings? _optics;
    private ClippingIndicators _clipping;
    public SKShader? Get(SKImage image, SKShader? input, LensCorrectionSettings optics, ClippingIndicators clipping,
        Func<SKImage, SKShader?, LensCorrectionSettings, ClippingIndicators, SKShader> compile)
    {
        if (optics.IsNeutral && clipping == ClippingIndicators.None) { Dispose(); return input; }
        if (_shader is null || !ReferenceEquals(_input, input) || _optics != optics || _clipping != clipping)
        {
            var next = compile(image, input, optics, clipping);
            _shader?.Dispose(); _shader = next; _input = input; _optics = optics; _clipping = clipping;
        }
        return _shader;
    }
    public void Dispose() { _shader?.Dispose(); _shader = null; _input = null; _optics = null; }
}

public sealed partial class PhotoRenderer
{
    private SKRuntimeEffect? _presentationEffect;
    private long _presentationBuilds, _geometryBuilds;
    public PresentationStatistics Presentation => new(_presentationBuilds, _geometryBuilds);
    private SKShader CreatePresentation(SKImage image, SKShader? input, LensCorrectionSettings optics, ClippingIndicators clipping)
    {
        if (_presentationEffect is null)
        {
            using var stream = typeof(PhotoRenderer).Assembly.GetManifestResourceStream("LightSpace.Rendering.Skia.Shaders.Presentation.sksl")
                ?? throw new InvalidOperationException("Missing presentation shader.");
            using var reader = new StreamReader(stream);
            _presentationEffect = SKRuntimeEffect.CreateShader(reader.ReadToEnd(), out var error)
                ?? throw new InvalidOperationException("Presentation shader: " + error);
        }
        var scope = new RuntimeShaderScope(_presentationEffect);
        try
        {
            scope.Uniforms["imageSize"] = new float[] { image.Width, image.Height };
            scope.Uniforms["optics"] = new[] { optics.Distortion, optics.Vignetting, optics.RedCyan, optics.BlueYellow };
            scope.Uniforms["clipping"] = (int)clipping;
            if (input is null) scope.Bind("developed", image.ToShader(SKShaderTileMode.Clamp, SKShaderTileMode.Clamp, new SKSamplingOptions(SKFilterMode.Linear)));
            else scope.BindBorrowed("developed", input);
            scope.Compile(); scope.ReleaseInputs();
        }
        catch { scope.Dispose(); throw; }
        GC.KeepAlive(image); GC.KeepAlive(input); _presentationBuilds++;
        return scope.TakeResult();
    }
}
