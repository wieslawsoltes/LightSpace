namespace LightSpace.Workbench;

public sealed partial class StudioView
{
    private void BuildCrop(PhotoDocument photo)
    {
        var panel = new StackPanel { Spacing = 7, Margin = new(18, 0, 18, 18) };
        panel.Children.Add(Note("Drag the crop or its handles. Straighten follows a drawn horizon; Escape cancels a gesture."));
        panel.Children.Add(Theme.Text("Aspect ratio", 11, true));
        var ratios = new Grid { ColumnSpacing = 4, RowSpacing = 4 };
        for (var i = 0; i < 3; i++) ratios.ColumnDefinitions.Add(new() { Width = new(1, GridUnitType.Star) });
        ratios.RowDefinitions.Add(new() { Height = GridLength.Auto }); ratios.RowDefinitions.Add(new() { Height = GridLength.Auto });
        var items = new[] { ("Original", 0d), ("1 × 1", 1d), ("4 × 3", 4d / 3), ("3 × 2", 1.5d), ("16 × 9", 16d / 9) };
        for (var i = 0; i < items.Length; i++)
        {
            var (label, ratio) = items[i];
            var button = Button("Crop " + label, text: label, action: () =>
            {
                var w = 1f; var h = 1f;
                if (ratio > 0) { var original = (double)photo.Width / photo.Height; if (original > ratio) w = (float)(ratio / original); else h = (float)(original / ratio); }
                Session.Edit("Crop " + label, s => s with { Crop = s.Crop with { Left = (1 - w) / 2, Right = (1 + w) / 2, Top = (1 - h) / 2, Bottom = (1 + h) / 2 } });
            });
            button.HorizontalAlignment = HorizontalAlignment.Stretch; button.Padding = new(4, 5, 4, 5);
            Grid.SetColumn(button, i % 3); Grid.SetRow(button, i / 3); ratios.Children.Add(button);
        }
        panel.Children.Add(ratios);
        var transform = Row();
        transform.Children.Add(Button("Rotate right", Glyph.Rotate, null, () => Session.Edit("Rotate right", s => s with { Crop = s.Crop with { QuarterTurns = s.Crop.QuarterTurns + 1 } })));
        transform.Children.Add(Button("Flip horizontal", Glyph.Flip, null, () => Session.Edit("Flip horizontal", s => s with { Crop = s.Crop with { FlipX = !s.Crop.FlipX } })));
        transform.Children.Add(Button("Flip vertical", text: "Flip Y", action: () => Session.Edit("Flip vertical", s => s with { Crop = s.Crop with { FlipY = !s.Crop.FlipY } }))); panel.Children.Add(transform);
        var geometry = CreateGeometryEditor(photo); geometry.Compact = true; panel.Children.Add(geometry);
        var commands = Row();
        commands.Children.Add(Button("Reset crop", Glyph.Undo, "Reset crop", () => Session.Edit("Reset crop", s => s with { Crop = new(), Geometry = new() })));
        commands.Children.Add(Button("Apply crop", Glyph.Check, "Done", () => ChooseTool(PhotoTool.Edit))); panel.Children.Add(commands);
        _inspector.Children.Add(panel);
    }
}
