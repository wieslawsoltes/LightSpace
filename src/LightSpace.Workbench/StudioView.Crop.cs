namespace LightSpace.Workbench;

public sealed partial class StudioView
{
    private CropAspectEditor? _cropAspectEditor;
    private void RefreshCropTool()
    {
        if (Session.Active is { } photo) RefreshCropContext(photo);
        PublishDiagnostics();
    }
    private void RefreshCropContext(PhotoDocument photo) => _cropAspectEditor?.SetContext(photo.State.Crop, photo.Width, photo.Height,
        Viewport.CropAspectLocked, Viewport.CropGuide, Viewport.CropGuideReversed);
    private void BuildCrop(PhotoDocument photo)
    {
        var panel = new StackPanel { Spacing = 7, Margin = new(18, 0, 18, 18) };
        _cropAspectEditor = new CropAspectEditor(); RefreshCropContext(photo);
        _cropAspectEditor.AspectRequested += Viewport.ApplyCropAspect;
        _cropAspectEditor.OriginalRequested += Viewport.RestoreOriginalCropAspect;
        _cropAspectEditor.LockRequested += value => Viewport.CropAspectLocked = value;
        _cropAspectEditor.SwapRequested += SwapCrop;
        _cropAspectEditor.GuideRequested += Viewport.SetCropGuide;
        foreach (var (id, widget) in _cropAspectEditor.Widgets) Register(id, widget);
        panel.Children.Add(_cropAspectEditor);
        var help = Note("Drag a handle to resize, or the interior to move. Shift-drag locks the current aspect; Alt/Option-drag resizes from center. X swaps orientation; O cycles guides.");
        panel.Children.Add(help);
        var transform = Row(); transform.Spacing = 4;
        transform.Children.Add(Button("Rotate right", Glyph.Rotate, null, () => Session.Edit("Rotate right", s => s with { Crop = s.Crop with { QuarterTurns = s.Crop.QuarterTurns + 1 } })));
        transform.Children.Add(Button("Flip horizontal", Glyph.Flip, null, () => Session.Edit("Flip horizontal", s => s with { Crop = s.Crop with { FlipX = !s.Crop.FlipX } })));
        transform.Children.Add(Button("Flip vertical", text: "Flip Y", action: () => Session.Edit("Flip vertical", s => s with { Crop = s.Crop with { FlipY = !s.Crop.FlipY } }))); panel.Children.Add(transform);
        var geometry = CreateGeometryEditor(photo); geometry.Compact = true; panel.Children.Add(geometry);
        var commands = Row();
        commands.Children.Add(Button("Reset crop", Glyph.Undo, "Reset crop", () =>
        {
            Viewport.CropAspectLocked = false;
            Session.Edit("Reset crop", s => s with { Crop = new(), Geometry = new() });
        }));
        commands.Children.Add(Button("Apply crop", Glyph.Check, "Done", () => ChooseTool(PhotoTool.Edit))); panel.Children.Add(commands);
        _inspector.Children.Add(panel);
    }
    private void SwapCrop()
    {
        try { Viewport.SwapCropOrientation(); }
        catch (ArgumentException error) { SetStatus(error.Message); }
    }
    private bool CropKeyboard(VirtualKey key, bool shift)
    {
        if (Viewport.Tool != PhotoTool.Crop) return false;
        if (key == VirtualKey.A) { Viewport.CropAspectLocked = !Viewport.CropAspectLocked; return true; }
        if (key == VirtualKey.O) { if (shift) Viewport.ReverseCropGuide(); else Viewport.CycleCropGuide(); return true; }
        if (Viewport.HasCropGesture) return key is VirtualKey.X or VirtualKey.Enter or VirtualKey.Left or VirtualKey.Right or VirtualKey.Up or VirtualKey.Down;
        switch (key)
        {
            case VirtualKey.X: SwapCrop(); return true;
            case VirtualKey.Enter: ChooseTool(PhotoTool.Edit); return true;
            case VirtualKey.Left: Viewport.NudgeCrop(shift ? -10 : -1, 0); return true;
            case VirtualKey.Right: Viewport.NudgeCrop(shift ? 10 : 1, 0); return true;
            case VirtualKey.Up: Viewport.NudgeCrop(0, shift ? -10 : -1); return true;
            case VirtualKey.Down: Viewport.NudgeCrop(0, shift ? 10 : 1); return true;
            default: return false;
        }
    }
}
