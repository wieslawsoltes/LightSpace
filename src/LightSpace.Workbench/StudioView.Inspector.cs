namespace LightSpace.Workbench;

public sealed partial class StudioView
{
    private ColorMixerEditor? _mixerEditor;
    private void ShowInspector(string mode) { _inspectorMode = mode; BuildInspector(); PublishDiagnostics(); }
    private void BuildInspector()
    {
        if (_buildingInspector) return; _buildingInspector = true;
        try
        {
            _inspectorBuilds++; _inspector.Children.Clear(); _sliders.Clear(); _curve = null; _gradingEditor = null; _maskEditor = null; _mixerEditor = null; _maskStructure = MaskStructure();
            var heading = new Grid { Padding = new(18, 16, 12, 12), ColumnDefinitions = { new() { Width = new(1, GridUnitType.Star) }, new() { Width = GridLength.Auto } } };
            heading.Children.Add(Theme.Text(_inspectorMode == "History" ? "Versions" : _inspectorMode, 20));
            var actions = Row(); actions.Spacing = 0;
            if (_inspectorMode == "Edit")
            {
                actions.Children.Add(Button("Auto tone", text: "Auto", action: () => { if (Session.Active is { } p) Session.Edit("Auto tone", s => s with { Develop = _renderer.Auto(p) }); }));
                actions.Children.Add(Button("Black and white", text: "B&W", action: () => Session.Edit("Black and white", s => s with { Develop = s.Develop with { Monochrome = !s.Develop.Monochrome } })));
                actions.Children.Add(Button("Color grading", text: "Grade", action: () => ShowInspector("Color grading")));
            }
            else actions.Children.Add(Button("Back to editing", Glyph.Edit, null, () => ChooseTool(PhotoTool.Edit)));
            Grid.SetColumn(actions, 1); heading.Children.Add(actions); _inspector.Children.Add(heading);
            if (Session.Active is not { } photo) { _inspector.Children.Add(Note("Import a photograph to start editing.")); return; }
            switch (_inspectorMode)
            {
                case "Presets": BuildPresets(); return;
                case "History": BuildHistory(photo); return;
                case "Info": BuildInfo(photo); return;
                case "Crop": BuildCrop(photo); return;
                case "Masks": BuildMasks(photo); return;
                case "Clone": BuildClone(photo); return;
                case "Color grading":
                    var grading = CreateGrading(photo); grading.Margin = new(18, 0, 18, 18); _inspector.Children.Add(grading); return;
            }
            _histogram.Margin = new(18, 0, 18, 10); _inspector.Children.Add(_histogram);
            var profile = Row(); profile.Margin = new(18, 0, 18, 15); profile.Children.Add(Theme.Text("Profile", 11, true)); profile.Children.Add(Theme.Text(photo.State.Develop.Monochrome ? "LightSpace Monochrome" : "LightSpace Color", 12)); _inspector.Children.Add(profile);
            var light = new StackPanel(); foreach (var name in new[] { "Exposure", "Contrast", "Highlights", "Shadows", "Whites", "Blacks" }) light.Children.Add(DevelopSlider(name)); _inspector.Children.Add(Section("Light", light));
            var curve = new StackPanel(); _curve = new ToneCurveView { Curve = photo.State.Develop.Curve }; Register("tone-curve", _curve);
            _curve.CurveChanged += value => Session.Preview(s => s with { Develop = s.Develop with { Curve = value } }); _curve.Committed += () => Session.CommitGesture("Point curve"); _curve.Canceled += Session.CancelGesture;
            curve.Children.Add(_curve); curve.Children.Add(Note("Drag the five points. Double-click to reset.")); _inspector.Children.Add(Section("Point curve", curve, false));
            var color = new StackPanel(); var wb = Row(); wb.Margin = new(0, 0, 0, 8); wb.Children.Add(Theme.Text("White balance", 11, true));
            wb.Children.Add(Button("Reset white balance", text: "As imported", action: () => Session.Edit("Reset white balance", s => s with { Develop = s.Develop with { Temperature = 0, Tint = 0 } }))); color.Children.Add(wb);
            foreach (var name in new[] { "Temperature", "Tint", "Vibrance", "Saturation" }) color.Children.Add(DevelopSlider(name)); _inspector.Children.Add(Section("Color", color));
            _mixerEditor = new ColorMixerEditor { Value = photo.State.Develop.Mixer };
            _mixerEditor.Previewed += value => Session.Preview(s => s with { Develop = s.Develop with { Mixer = value } });
            _mixerEditor.Committed += () => Session.CommitGesture("Color mixer"); _mixerEditor.Canceled += Session.CancelGesture;
            _inspector.Children.Add(Section("Color mixer", _mixerEditor, false));
            _inspector.Children.Add(Section("Color grading", CreateGrading(photo), false));
            var effects = new StackPanel(); foreach (var name in new[] { "Texture", "Clarity", "Dehaze", "Vignette", "Grain" }) effects.Children.Add(DevelopSlider(name)); _inspector.Children.Add(Section("Effects", effects, false));
            var detail = new StackPanel(); detail.Children.Add(DevelopSlider("Sharpening")); detail.Children.Add(DevelopSlider("NoiseReduction")); detail.Children.Add(Note("Fast spatial smoothing, not AI denoising.")); _inspector.Children.Add(Section("Detail", detail, false));
            var commands = new StackPanel { Margin = new(12, 12, 12, 14), Spacing = 5 }; var copy = Row();
            copy.Children.Add(Button("Copy edit settings", text: "Copy", action: () => { _clipboard = Session.Active?.State; SetStatus("Edit settings copied in this workspace."); }));
            copy.Children.Add(Button("Paste edit settings", text: "Paste", action: PasteSettings)); copy.Children.Add(Button("Sync selected photos", Glyph.Link, "Sync", Session.SyncSelected)); commands.Children.Add(copy);
            commands.Children.Add(Button("Reset all edits", Glyph.Undo, "Reset edits", () => Session.Edit("Reset edits", s => s with { Develop = new(), Crop = new(), Masks = [], CloneSpots = [] }))); _inspector.Children.Add(commands);
        }
        finally { _buildingInspector = false; }
    }
    private ColorGradingEditor CreateGrading(PhotoDocument photo)
    {
        var editor = new ColorGradingEditor { Value = photo.State.Develop.Grading }; _gradingEditor = editor;
        editor.Previewed += value => Session.Preview(s => s with { Develop = s.Develop with { Grading = value } });
        editor.Committed += () => Session.CommitGesture("Color grading"); editor.Canceled += Session.CancelGesture;
        foreach (var (name, widget) in editor.Widgets) Register(name, widget); return editor;
    }
    private AdjustmentSlider DevelopSlider(string name)
    {
        var nonnegative = name is "Grain" or "Sharpening" or "NoiseReduction"; var label = name == "NoiseReduction" ? "Noise reduction" : name;
        var slider = new AdjustmentSlider(label, name == "Exposure" ? -5 : nonnegative ? 0 : -100, name == "Exposure" ? 5 : 100, 0, name == "Exposure" ? .01 : 1) { Value = Session.Active?.State.Develop.Get(name) ?? 0 };
        if (name == "Temperature") { slider.StartColor = SKColor.Parse("#397cbf"); slider.EndColor = SKColor.Parse("#d4bc54"); }
        if (name == "Tint") { slider.StartColor = SKColor.Parse("#47976d"); slider.EndColor = SKColor.Parse("#ba6baa"); }
        slider.ValueChanged += value => Session.Preview(s => s with { Develop = s.Develop.Set(name, value) }); slider.ValueCommitted += () => Session.CommitGesture(label); slider.GestureCanceled += Session.CancelGesture;
        _sliders[name] = slider; Register("slider-" + name, slider.TrackElement); return slider;
    }
    private static TextBlock Note(string text) => new()
    {
        Text = text, FontFamily = Theme.Font, FontSize = 11, Foreground = Theme.Muted, TextWrapping = TextWrapping.Wrap,
        TextTrimming = TextTrimming.None, Margin = new(0, 8, 0, 10)
    };
    private void BuildPresets()
    {
        _inspector.Children.Add(new Border { Margin = new(18, 0, 18, 12), Child = Note("Presets replace global development settings, retaining your crop and local masks.") });
        foreach (var group in BuiltInPresets.All.GroupBy(p => p.Group))
        {
            var panel = new StackPanel { Spacing = 4 };
            foreach (var preset in group)
            {
                var button = Button("Preset " + preset.Name, Glyph.Presets, preset.Name, () => { Session.Edit("Preset: " + preset.Name, s => s with { Develop = preset.Settings }); SetStatus("Applied " + preset.Name); });
                button.HorizontalAlignment = HorizontalAlignment.Stretch; button.HorizontalContentAlignment = HorizontalAlignment.Left; panel.Children.Add(button);
            }
            _inspector.Children.Add(Section(group.Key, panel));
        }
    }
    private void BuildHistory(PhotoDocument photo)
    {
        var versions = new StackPanel { Spacing = 5 }; versions.Children.Add(Button("Save version", Glyph.Add, "Create named version", () => Run(SaveVersionAsync)));
        foreach (var version in photo.Versions.AsEnumerable().Reverse()) versions.Children.Add(Button("Restore " + version.Name, Glyph.History, version.Name, () => Session.Edit("Restore version: " + version.Name, _ => version.State)));
        if (photo.Versions.Count == 0) versions.Children.Add(Note("Preserve a look before exploring another direction.")); _inspector.Children.Add(Section("Named versions", versions));
        var history = new StackPanel { Spacing = 8 }; var buttons = Row(); buttons.Children.Add(Button("History undo", Glyph.Undo, "Undo", Session.Undo)); buttons.Children.Add(Button("History redo", Glyph.Redo, "Redo", Session.Redo)); history.Children.Add(buttons);
        foreach (var item in Session.History.Take(30)) history.Children.Add(Theme.Text(item, 12, true)); if (!Session.CanUndo) history.Children.Add(Note("Your original image is unchanged.")); _inspector.Children.Add(Section("Edit history", history));
    }
    private void BuildInfo(PhotoDocument photo)
    {
        var info = new StackPanel { Spacing = 12, Margin = new(18, 0, 18, 18) };
        var filename = Theme.Text(photo.Name, 14); filename.TextWrapping = TextWrapping.Wrap; info.Children.Add(filename);
        info.Children.Add(Theme.Text($"{photo.Width:N0} × {photo.Height:N0} pixels", 12, true)); info.Children.Add(Theme.Text($"{photo.Original.Length / 1048576d:0.0} MiB · source retained", 11, true));
        if (!string.IsNullOrEmpty(photo.Camera)) info.Children.Add(Note(photo.Camera)); if (!string.IsNullOrEmpty(photo.ExposureInfo)) info.Children.Add(Note(photo.ExposureInfo));
        info.Children.Add(Theme.Text("Caption", 11, true)); var caption = Theme.Input("Add a caption", "Photo caption", photo.State.Caption); caption.AcceptsReturn = true; caption.Height = 84; caption.TextWrapping = TextWrapping.Wrap; info.Children.Add(caption);
        info.Children.Add(Theme.Text("Keywords", 11, true)); var keywords = Theme.Input("landscape, mountains, travel", "Photo keywords", string.Join(", ", photo.State.Keywords)); info.Children.Add(keywords);
        info.Children.Add(Button("Save photo information", Glyph.Check, "Save information", () => Session.Edit("Photo information", s => s with { Caption = caption.Text, Keywords = keywords.Text.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries) })));
        if (Session.Catalog.Albums.Count > 0) { info.Children.Add(Theme.Text("Add selection to album", 11, true)); foreach (var album in Session.Catalog.Albums) info.Children.Add(Button("Add to " + album.Name, Glyph.Folder, album.Name, () => { Session.AddSelectionToAlbum(album.Id); SetStatus("Added selection to " + album.Name); })); }
        info.Children.Add(Note("Rendered image exports do not retain EXIF/IPTC metadata. Catalog backups retain the original bytes.")); _inspector.Children.Add(info);
    }
    private void BuildCrop(PhotoDocument photo)
    {
        var panel = new StackPanel { Spacing = 10, Margin = new(18, 0, 18, 18) }; panel.Children.Add(Note("Drag a new crop, resize its handles, or move an existing crop. Escape cancels the active gesture.")); panel.Children.Add(Theme.Text("Aspect ratio", 12, true));
        foreach (var (label, ratio) in new[] { ("Original", 0d), ("1 × 1", 1d), ("4 × 3", 4d / 3), ("3 × 2", 1.5d), ("16 × 9", 16d / 9) })
            panel.Children.Add(Button("Crop " + label, text: label, action: () =>
            {
                var w = 1f; var h = 1f; if (ratio > 0) { var original = (double)photo.Width / photo.Height; if (original > ratio) w = (float)(ratio / original); else h = (float)(original / ratio); }
                Session.Edit("Crop " + label, s => s with { Crop = s.Crop with { Left = (1 - w) / 2, Right = (1 + w) / 2, Top = (1 - h) / 2, Bottom = (1 + h) / 2 } });
            }));
        var transform = Row(); transform.Children.Add(Button("Rotate right", Glyph.Rotate, null, () => Session.Edit("Rotate right", s => s with { Crop = s.Crop with { QuarterTurns = s.Crop.QuarterTurns + 1 } })));
        transform.Children.Add(Button("Flip horizontal", Glyph.Flip, null, () => Session.Edit("Flip horizontal", s => s with { Crop = s.Crop with { FlipX = !s.Crop.FlipX } })));
        transform.Children.Add(Button("Flip vertical", text: "Flip Y", action: () => Session.Edit("Flip vertical", s => s with { Crop = s.Crop with { FlipY = !s.Crop.FlipY } }))); panel.Children.Add(transform);
        panel.Children.Add(Button("Reset crop", Glyph.Undo, "Reset crop", () => Session.Edit("Reset crop", s => s with { Crop = new() }))); panel.Children.Add(Button("Apply crop", Glyph.Check, "Done", () => ChooseTool(PhotoTool.Edit))); _inspector.Children.Add(panel);
    }
    private void BuildClone(PhotoDocument photo)
    {
        var panel = new StackPanel { Spacing = 10, Margin = new(18, 0, 18, 18) }; panel.Children.Add(Note("Alt-click sets the source. Click a destination to clone from that source. This is feathered cloning, not generative removal."));
        var radius = new AdjustmentSlider("Size (%)", .2, 25, 4, .1) { Value = Viewport.CloneRadius * 100 }; radius.ValueChanged += value => Viewport.CloneRadius = value / 100; panel.Children.Add(radius);
        panel.Children.Add(Theme.Text($"{photo.State.CloneSpots.Length} / 32 spots", 11, true)); panel.Children.Add(Button("Remove last clone spot", Glyph.Undo, "Undo last edit", Session.Undo)); panel.Children.Add(Button("Clear clone spots", Glyph.Trash, "Clear spots", () => Session.Edit("Clear clone spots", s => s with { CloneSpots = [] }))); _inspector.Children.Add(panel);
    }
    private void PasteSettings()
    {
        if (_clipboard is not { } source) { SetStatus("Copy edit settings from a photograph first."); return; }
        Session.Edit("Paste edit settings", s => s with { Develop = source.Develop, Crop = source.Crop, Masks = source.Masks, CloneSpots = source.CloneSpots }, true);
    }
}
