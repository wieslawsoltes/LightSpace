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
        panel.Children.Add(Note("Drag to create a gradient. Drag its pin, edge handles or rotation handle to refine it. Shift-drag starts a new mask over an existing pin."));
        var overlays = Row(); overlays.Spacing = 3;
        var outline = Button("Toggle mask overlay", Glyph.Mask, "Outline"); outline.Selected = Viewport.MaskOverlay;
        outline.Click += (_, _) => { Viewport.MaskOverlay = !Viewport.MaskOverlay; outline.Selected = Viewport.MaskOverlay; Viewport.Invalidate(); };
        var coverage = Button("Mask coverage", text: "Coverage"); coverage.Selected = Viewport.ShowMaskCoverage;
        coverage.Click += (_, _) => { Viewport.ShowMaskCoverage = !Viewport.ShowMaskCoverage; coverage.Selected = Viewport.ShowMaskCoverage; Viewport.Invalidate(); };
        overlays.Children.Add(outline); overlays.Children.Add(coverage); panel.Children.Add(overlays);
        for (var i = 0; i < photo.State.Masks.Length; i++)
        {
            var index = i; var button = Button("Select mask " + i, Glyph.Mask, photo.State.Masks[i].Name, () => Viewport.SetActiveMask(index));
            button.HorizontalAlignment = HorizontalAlignment.Stretch; button.HorizontalContentAlignment = HorizontalAlignment.Left; button.Selected = i == Viewport.ActiveMask; panel.Children.Add(button);
        }
        if (photo.State.Masks.Length == 0) { panel.Children.Add(Note("No local masks yet. Luminance masks select source brightness without AI or external services.")); _inspector.Children.Add(panel); return; }
        var selected = Math.Clamp(Viewport.ActiveMask, 0, photo.State.Masks.Length - 1); if (selected != Viewport.ActiveMask) Viewport.SetActiveMask(selected);
        var mask = photo.State.Masks[selected];
        var manage = Row(); manage.Spacing = 2;
        manage.Children.Add(Button("Rename mask", text: "Rename", action: () => Run(() => TextPromptAsync("Rename mask", "Mask name", mask.Name,
            name => Session.Edit("Rename mask", s => s with { Masks = s.Masks.Select(m => m.Id == mask.Id ? m with { Name = name } : m).ToArray() })))));
        manage.Children.Add(Button("Duplicate mask", text: "Duplicate", action: () =>
        {
            if (photo.State.Masks.Length >= 8) { SetStatus("The eight-mask limit has been reached."); return; }
            // Settings change without rebuilding this inspector. Resolve the latest
            // snapshot by identity instead of copying the captured construction state.
            var current = photo.State.Masks.FirstOrDefault(m => m.Id == mask.Id);
            if (current is null) return;
            Session.Edit("Duplicate mask", s => s with { Masks = [.. s.Masks, current with { Id = Guid.NewGuid(), Name = current.Name + " copy" }] });
            Viewport.SetActiveMask(photo.State.Masks.Length - 1);
        }));
        manage.Children.Add(Button("Delete mask", Glyph.Trash, null, () => Session.Edit("Delete mask", s => s with { Masks = s.Masks.Where(m => m.Id != mask.Id).ToArray() }))); panel.Children.Add(manage);
        _maskEditor = new MaskSettingsEditor { Value = mask };
        _maskEditor.Previewed += value => Session.Preview(s => s with { Masks = s.Masks.Select(m => m.Id == value.Id ? value : m).ToArray() });
        _maskEditor.Committed += () => Session.CommitGesture("Mask settings"); _maskEditor.Canceled += Session.CancelGesture;
        foreach (var (name, widget) in _maskEditor.Widgets) Register(name, widget);
        panel.Children.Add(_maskEditor); _inspector.Children.Add(panel);
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
