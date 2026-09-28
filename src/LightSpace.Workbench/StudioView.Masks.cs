namespace LightSpace.Workbench;

public sealed partial class StudioView
{
    private void BuildMasks(PhotoDocument photo)
    {
        var panel = new StackPanel { Spacing = 6, Margin = new(18, 0, 18, 18) };
        var create = Row(); create.Spacing = 2;
        create.Children.Add(Button("Radial gradient", text: "Radial", action: () => ChooseTool(PhotoTool.RadialMask)));
        create.Children.Add(Button("Linear gradient", text: "Linear", action: () => ChooseTool(PhotoTool.LinearMask)));
        create.Children.Add(Button("Luminance range", text: "Luminance", action: CreateRangeMask)); panel.Children.Add(create);
        var brushes = Row(); brushes.Spacing = 4;
        brushes.Children.Add(Button("New brush mask", Glyph.Add, "New brush", () => ChooseBrush(true)));
        var paint = Button("Paint selected mask", text: "Paint selected", action: () => ChooseBrush(false));
        paint.Selected = Viewport.Tool == PhotoTool.Brush; brushes.Children.Add(paint); panel.Children.Add(brushes);
        panel.Children.Add(Button("Color range", Glyph.Search, "Color range", CreateColorMask));
        panel.Children.Add(Note(Viewport.Tool == PhotoTool.ColorRange
            ? "Click a source color. Shift-click adds up to five samples; Alt-click a pin removes it. Refine tolerance and smoothness below."
            : Viewport.Tool == PhotoTool.Brush
            ? "Paint or erase the selected mask. Alt temporarily erases; Escape cancels the stroke."
            : "Drag to create a gradient; refine its pin and handles. Shift-drag starts another mask."));
        var overlays = Row(); overlays.Spacing = 3;
        var outline = Button("Toggle mask overlay", Glyph.Mask, "Outline"); outline.Selected = Viewport.MaskOverlay;
        outline.Click += (_, _) => { Viewport.MaskOverlay = !Viewport.MaskOverlay; outline.Selected = Viewport.MaskOverlay; Viewport.Invalidate(); };
        var coverage = Button("Mask coverage", text: "Coverage"); coverage.Selected = Viewport.ShowMaskCoverage;
        coverage.Click += (_, _) => { Viewport.ShowMaskCoverage = !Viewport.ShowMaskCoverage; coverage.Selected = Viewport.ShowMaskCoverage; Viewport.Invalidate(); };
        overlays.Children.Add(outline); overlays.Children.Add(coverage); panel.Children.Add(overlays);
        if (Viewport.Tool == PhotoTool.Brush)
            panel.Children.Add(Section("Brush settings", CreateBrushSettings(), false));
        for (var i = 0; i < photo.State.Masks.Length; i++)
        {
            var index = i; var button = Button("Select mask " + i, Glyph.Mask, photo.State.Masks[i].Name, () => Viewport.SetActiveMask(index));
            button.HorizontalAlignment = HorizontalAlignment.Stretch; button.HorizontalContentAlignment = HorizontalAlignment.Left; button.Selected = i == Viewport.ActiveMask; panel.Children.Add(button);
        }
        if (photo.State.Masks.Length == 0)
        {
            panel.Children.Add(Note("Paint to create a brush mask, drag a gradient, or select source brightness with Luminance.")); _inspector.Children.Add(panel); return;
        }
        var selected = Math.Clamp(Viewport.ActiveMask, 0, photo.State.Masks.Length - 1); if (selected != Viewport.ActiveMask) Viewport.SetActiveMask(selected);
        var mask = photo.State.Masks[selected];
        var manage = Row(); manage.Spacing = 2;
        manage.Children.Add(Button("Rename mask", text: "Rename", action: () => Run(() => TextPromptAsync("Rename mask", "Mask name", mask.Name,
            name => Session.Edit("Rename mask", s => s with { Masks = s.Masks.Select(m => m.Id == mask.Id ? m with { Name = name } : m).ToArray() })))));
        manage.Children.Add(Button("Duplicate mask", text: "Duplicate", action: () =>
        {
            if (photo.State.Masks.Length >= 8) { SetStatus("The eight-mask limit has been reached."); return; }
            var current = photo.State.Masks.FirstOrDefault(m => m.Id == mask.Id); if (current is null) return;
            Session.Edit("Duplicate mask", s => s with { Masks = [.. s.Masks, current with { Id = Guid.NewGuid(), Name = current.Name + " copy" }] });
            Viewport.SetActiveMask(photo.State.Masks.Length - 1);
        }));
        manage.Children.Add(Button("Delete mask", Glyph.Trash, null, () => Session.Edit("Delete mask", s => s with { Masks = s.Masks.Where(m => m.Id != mask.Id).ToArray() }))); panel.Children.Add(manage);
        _maskEditor = new MaskSettingsEditor { Value = mask };
        _maskEditor.Previewed += value => Session.Preview(s => s with { Masks = s.Masks.Select(m => m.Id == value.Id ? value : m).ToArray() });
        _maskEditor.SampleColorsRequested += () => { Viewport.ShowMaskCoverage = true; ChooseTool(PhotoTool.ColorRange); };
        _maskEditor.Committed += () => Session.CommitGesture("Mask settings"); _maskEditor.Canceled += Session.CancelGesture;
        foreach (var (name, widget) in _maskEditor.Widgets) Register(name, widget);
        panel.Children.Add(_maskEditor); _inspector.Children.Add(panel);
    }
    private void CreateColorMask()
    {
        if (Session.Active is not { } photo) return;
        if (photo.State.Masks.Length >= 8) { SetStatus("The eight-mask limit has been reached."); return; }
        Session.Edit("Create color range", state => state with
        {
            Masks = [.. state.Masks, new LocalMask { Kind = MaskKind.ColorRange, Name = "Color range " + (state.Masks.Length + 1), ColorRange = new() { Enabled = true } }]
        });
        Viewport.SetActiveMask(photo.State.Masks.Length - 1); Viewport.ShowMaskCoverage = true; ChooseTool(PhotoTool.ColorRange);
        SetStatus("Click on the photograph to sample the original source color.");
    }
    private void CreateRangeMask()
    {
        if (Session.Active is not { } photo) return;
        if (photo.State.Masks.Length >= 8) { SetStatus("The eight-mask limit has been reached."); return; }
        Session.Edit("Create luminance mask", s => s with
        {
            Masks = [.. s.Masks, new LocalMask { Kind = MaskKind.LuminanceRange, Name = "Luminance range " + (s.Masks.Length + 1), RangeEnabled = true, RangeMin = .35f, RangeMax = .75f }]
        });
        Viewport.SetActiveMask(photo.State.Masks.Length - 1);
        SetStatus("Luminance selection uses source sRGB brightness before development. Enable Coverage to inspect its selection.");
    }
}
