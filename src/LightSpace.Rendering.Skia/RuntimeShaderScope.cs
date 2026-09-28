using SkiaSharp;
namespace LightSpace.Rendering.Skia;

/// <summary>
/// Owns the staging inputs and the compiled output independently. The output is
/// rooted in a field while input disposal executes, then transferred after all
/// cleanup has completed. No compiled shader is returned through a finally.
/// </summary>
internal sealed class RuntimeShaderScope : IDisposable
{
    private readonly SKRuntimeEffect _effect;
    private readonly SKRuntimeEffectChildren _children;
    private readonly List<SKShader> _inputs = new(10);
    private SKShader? _result;
    private bool _inputsReleased;
    public SKRuntimeEffectUniforms Uniforms { get; }

    public RuntimeShaderScope(SKRuntimeEffect effect)
    {
        _effect = effect;
        Uniforms = new(effect);
        try { _children = new(effect); }
        catch { Uniforms.Dispose(); throw; }
    }
    /// <summary>Transfers ownership of an input shader, including on binding failure.</summary>
    public void Bind(string name, SKShader shader)
    {
        ArgumentNullException.ThrowIfNull(shader);
        if (_inputsReleased || _result is not null)
        {
            shader.Dispose();
            throw new InvalidOperationException("Shader inputs are no longer writable.");
        }
        _inputs.Add(shader);
        _children[name] = shader;
    }
    public void Compile()
    {
        if (_inputsReleased || _result is not null) throw new InvalidOperationException("Shader scope has already been compiled or released.");
        _result = _effect.ToShader(Uniforms, _children)
            ?? throw new InvalidOperationException("Unable to create development shader.");
    }
    public void ReleaseInputs()
    {
        if (_inputsReleased) return;
        _inputsReleased = true;
        try
        {
            foreach (var input in _inputs) input.Dispose();
            _inputs.Clear();
        }
        finally
        {
            _children.Dispose();
            Uniforms.Dispose();
        }
    }
    public SKShader TakeResult()
    {
        if (!_inputsReleased) throw new InvalidOperationException("Release staging inputs before transferring the shader.");
        var result = _result ?? throw new InvalidOperationException("No compiled shader is available.");
        _result = null;
        return result;
    }
    public void Dispose()
    {
        try { ReleaseInputs(); }
        finally { _result?.Dispose(); _result = null; }
    }
}
