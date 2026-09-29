namespace LightSpace.Workbench;

public sealed partial class StudioView
{
    private Border? _dialogOverlay;
    private Task TextPromptAsync(string title, string placeholder, string value, Action<string> apply)
    {
        var input = Theme.Input(placeholder, placeholder, value); var panel = new StackPanel { Spacing = 12 }; panel.Children.Add(input);
        ShowDialog(title, panel, "Create", () =>
        {
            if (string.IsNullOrWhiteSpace(input.Text)) throw new InvalidOperationException("A name is required.");
            apply(input.Text); return Task.CompletedTask;
        });
        return Task.CompletedTask;
    }
    private void ShowDialog(string title, FrameworkElement body, string confirm, Func<Task> apply)
    {
        CloseDialog(); if (Content is not Grid root) return;
        var sheet = new Grid
        {
            MaxWidth = 520, MaxHeight = Math.Max(300, ActualHeight - 60), MinWidth = 340,
            VerticalAlignment = VerticalAlignment.Center,
            Background = Theme.Panel, Padding = new(24), CornerRadius = new(8),
            RowDefinitions = { new() { Height = GridLength.Auto }, new() { Height = GridLength.Auto }, new() { Height = GridLength.Auto } }
        };
        var heading = Theme.Text(title, 21); heading.Margin = new(0, 0, 0, 18); sheet.Children.Add(heading);
        // Bound only the scrollable body. Short forms should not stretch into
        // full-height blank sheets; long reports still leave actions reachable.
        var scroll = new ScrollViewer
        {
            Content = body, MaxHeight = Math.Max(100, ActualHeight - 210),
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };
        Grid.SetRow(scroll, 1); sheet.Children.Add(scroll);
        var actions = Row(); actions.HorizontalAlignment = HorizontalAlignment.Right; actions.Margin = new(0, 18, 0, 0);
        actions.Children.Add(Button("Cancel dialog", text: "Cancel", action: CloseDialog));
        var button = Button(confirm, text: confirm); button.Selected = true;
        button.Click += async (_, _) =>
        {
            button.IsEnabled = false;
            try { await apply(); CloseDialog(); }
            catch (Exception error) { SetStatus(error.Message); button.IsEnabled = true; }
        };
        actions.Children.Add(button); Grid.SetRow(actions, 2); sheet.Children.Add(actions);
        _dialogOverlay = new Border
        {
            Background = Theme.Brush("#aa000000"), Child = sheet,
            HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch
        };
        Register("dialog-sheet", sheet);
        Grid.SetRowSpan(_dialogOverlay, 2); root.Children.Add(_dialogOverlay); PublishDiagnostics();
    }
    private void CloseDialog()
    {
        if (_dialogOverlay is null) return;
        if (Content is Grid root) root.Children.Remove(_dialogOverlay);
        _dialogOverlay = null; PublishDiagnostics();
    }
}
