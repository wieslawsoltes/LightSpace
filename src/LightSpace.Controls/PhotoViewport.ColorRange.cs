using LightSpace.Core;
namespace LightSpace.Controls;

public sealed partial class PhotoViewport
{
    private void PressColorRange(PointerRoutedEventArgs e)
    {
        if (_session.Active is not { } photo || ActiveMask < 0 || ActiveMask >= photo.State.Masks.Length) return;
        var screen = e.GetCurrentPoint(_surface).Position;
        if (!_imageRect.Contains((float)screen.X, (float)screen.Y)) return;
        static bool Down(VirtualKey key) => Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(key).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);
        var add = e.KeyModifiers.HasFlag(VirtualKeyModifiers.Shift) || Down(VirtualKey.Shift);
        var remove = e.KeyModifiers.HasFlag(VirtualKeyModifiers.Menu) || Down(VirtualKey.Menu);
        var mask = photo.State.Masks[ActiveMask]; var settings = mask.ColorRange;
        Focus(FocusState.Pointer); e.Handled = true;
        if (remove)
        {
            var selected = -1; var distance = 144d;
            for (var i = 0; i < settings.Samples.Length; i++)
            {
                var p = ToView(settings.Samples[i].X, settings.Samples[i].Y);
                var d = (p.X - screen.X) * (p.X - screen.X) + (p.Y - screen.Y) * (p.Y - screen.Y);
                if (d <= distance) { selected = i; distance = d; }
            }
            if (selected < 0) { Status?.Invoke("Alt-click a sample pin to remove it."); return; }
            settings = settings with { Samples = settings.Samples.Where((_, i) => i != selected).ToArray() };
        }
        else
        {
            if (add && settings.Samples.Length >= ColorRangeSettings.MaximumSamples)
            { Status?.Invoke("Five color samples are already selected. Replace one or Alt-click a pin to remove it."); return; }
            var uv = ToSource(screen);
            if (!ValidSource(uv)) { Status?.Invoke("Choose a point inside the corrected source image."); return; }
            ColorSample? sample;
            try { sample = _renderer.SampleSource(photo, uv.X, uv.Y); }
            catch (Exception error) { Status?.Invoke(error.Message); return; }
            if (sample is null) { Status?.Invoke("The sampled source area is transparent."); return; }
            settings = settings with { Enabled = true, Samples = add ? [.. settings.Samples, sample] : [sample] };
        }
        _session.Edit(remove ? "Remove color sample" : add ? "Add color sample" : "Sample color", state => state with
        { Masks = state.Masks.Select(m => m.Id == mask.Id ? m with { ColorRange = settings } : m).ToArray() });
        Invalidate(); ViewChanged?.Invoke();
        Status?.Invoke($"{settings.Samples.Length} of 5 source colors selected. Shift-click adds; Alt-click removes a pin. Coverage shows the selection.");
    }
    private void AddColorHandles(List<ViewportHandle> handles, LocalMask mask)
    {
        for (var i = 0; i < mask.ColorRange.Samples.Length; i++)
        {
            var sample = mask.ColorRange.Samples[i]; var point = ToView(sample.X, sample.Y);
            handles.Add(new("color-sample-" + i, point.X, point.Y));
        }
    }
}
