namespace LightSpace.Workbench;

public sealed partial class StudioView
{
    private EditSettingsGroup _settingsGroups = EditSettingsGroup.Global;
    private EditSettingsGroup _clipboardGroups = EditSettingsGroup.All;

    private void ShowSettingsDialog(bool synchronize, PhotoState? suppliedSource = null, bool activeOnly = false)
    {
        Session.CommitGesture("Adjustment");
        if (Session.Active is not { } active) return;
        var catalog = Session.Catalog;
        var source = suppliedSource ?? active.State;
        var ids = activeOnly ? new[] { active.Id } : Session.Selection.Where(id => id != active.Id).ToArray();
        if (synchronize && ids.Length == 0) { SetStatus("Select another photo with Control-click before synchronizing."); return; }
        var editor = new SettingsTransferEditor { Groups = _settingsGroups };
        foreach (var (id, widget) in editor.Widgets) Register(id, widget);
        var panel = new StackPanel { Spacing = 12 };
        panel.Children.Add(Note(synchronize ? $"Apply selected groups to {ids.Length} photo(s). The source look and target identities are captured for this review." : "Choose which edit groups to copy. Paste applies only these groups to the current selection."));
        panel.Children.Add(editor);
        var title = suppliedSource is not null ? "Match reference settings" : synchronize ? "Synchronize settings" : "Copy settings";
        ShowDialog(title, panel, synchronize ? "Apply selected settings" : "Copy selected settings", () =>
        {
            if (editor.Groups == EditSettingsGroup.None) throw new InvalidOperationException("Select at least one settings group.");
            if (!ReferenceEquals(catalog, Session.Catalog)) throw new InvalidOperationException("The catalog changed. Review the settings again.");
            var transfer = new EditSettingsTransfer(source, editor.Groups);
            if (synchronize)
            {
                Session.ApplySettings(transfer, ids);
                SetStatus($"Applied selected edit groups to {ids.Length} photo(s); catalog metadata retained.");
            }
            else
            {
                _clipboard = transfer.Source; _clipboardGroups = transfer.Groups;
                SetStatus("Selected edit settings copied. Paste applies them without changing target metadata.");
            }
            _settingsGroups = editor.Groups;
            return Task.CompletedTask;
        });
    }
    private void CopyAllSettings()
    {
        Session.CommitGesture("Adjustment"); _clipboard = Session.Active?.State; _clipboardGroups = EditSettingsGroup.All;
        SetStatus("All edit settings copied; metadata excluded. Use Copy settings to choose individual groups.");
    }
    private void PasteSelectedSettings()
    {
        if (_clipboard is null) { SetStatus("Copy edit settings from a photograph first."); return; }
        Session.ApplySettings(new(_clipboard, _clipboardGroups), Session.Selection);
        SetStatus("Pasted the copied edit groups; target metadata retained.");
    }
}
