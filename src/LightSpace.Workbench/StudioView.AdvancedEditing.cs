namespace LightSpace.Workbench;

public sealed partial class StudioView
{
    private PointCurveEditor CreatePointCurves(PhotoDocument photo)
    {
        var editor = new PointCurveEditor { Value = photo.State.Develop.Channels };
        editor.Previewed += value => Session.Preview(s => s with { Develop = s.Develop with { Channels = value } });
        editor.Committed += () => Session.CommitGesture("RGB point curve"); editor.Canceled += Session.CancelGesture;
        void Refresh() { if (Session.Active is { } active) editor.Value = active.State.Develop.Channels; }
        editor.Loaded += (_, _) => { Session.ViewChanged += Refresh; RegisterNamedControls(editor); Register("rgb-curve", editor.PlotElement); PublishDiagnostics(); };
        editor.Unloaded += (_, _) => Session.ViewChanged -= Refresh;
        return editor;
    }
    private void RegisterNamedControls(DependencyObject root)
    {
        if (root is LightButton button)
        {
            var name = AutomationProperties.GetName(button); if (!string.IsNullOrWhiteSpace(name)) Register(name, button);
        }
        if (root is AdjustmentSlider slider)
        {
            var name = AutomationProperties.GetName(slider); if (!string.IsNullOrWhiteSpace(name)) Register("advanced-" + name, slider.TrackElement);
        }
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++) RegisterNamedControls(VisualTreeHelper.GetChild(root, i));
    }
    private void ChooseBrush(bool createNew)
    {
        if (Session.Active is not { } photo) return;
        if (createNew)
        {
            if (photo.State.Masks.Length >= 8) { SetStatus("The eight-mask limit has been reached."); return; }
            Session.Edit("Create brush mask", s => s with { Masks = [.. s.Masks, new LocalMask { Kind = MaskKind.Brush, Name = "Brush " + (s.Masks.Length + 1) }] });
            Viewport.SetActiveMask(photo.State.Masks.Length - 1);
        }
        SetGrid(false); Viewport.SetTool(PhotoTool.Brush); ShowInspector("Masks");
        SetStatus("Paint on the photograph. Alt temporarily erases; Escape cancels the current stroke.");
    }
    private UIElement CreateBrushSettings()
    {
        var editor = new BrushSettingsEditor { Value = Viewport.BrushSettings };
        editor.Changed += settings => { Viewport.BrushSettings = settings; Viewport.Invalidate(); };
        editor.Loaded += (_, _) => { RegisterNamedControls(editor); PublishDiagnostics(); };
        return editor;
    }
}
