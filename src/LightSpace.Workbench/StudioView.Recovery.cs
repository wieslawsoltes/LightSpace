namespace LightSpace.Workbench;

public sealed partial class StudioView
{
    private string? _recoveryProtection;

    public RecoveryStatus Recovery => _recovery.Status;

    private void RecoveryChanged(RecoveryStatus status)
    {
        if (_disposed) return;
        if (_saveButton is not null)
        {
            _saveButton.Text = _recoveryProtection is not null ? "Recovery protected" : status.State switch
            {
                "Saved" => "Saved locally",
                "Saving" => "Saving…",
                "Preview" => "Editing…",
                "Failed" => "Save failed · retry",
                _ => "Unsaved changes"
            };
            ToolTipService.SetToolTip(_saveButton, status.Error is null
                ? $"Committed revision {status.CurrentRevision}; saved revision {status.SavedRevision}. Click to save now."
                : "Recovery failed: " + status.Error + ". Click to retry or export a catalog backup.");
        }
        UnsavedChangesChanged?.Invoke(status.HasUnsavedChanges);
        PublishDiagnostics();
    }

    /// <summary>Preserves an unreadable prior catalog until the user explicitly replaces it.</summary>
    public void ProtectExistingRecovery(string reason)
    {
        _saveTimer.Stop();
        _recoveryProtection = reason;
        if (_saveButton is not null) _saveButton.Click += (_, _) =>
        {
            if (_recoveryProtection is null) return;
            var message = Note("The previous recovery could not be opened. It has not been overwritten. " +
                "Replacing it will discard that stored recovery and save the current workspace instead. " +
                "Keep the existing browser profile or native recovery file if you need to recover its contents. " + _recoveryProtection);
            ShowDialog("Replace unreadable recovery?", message, "Replace recovery", async () =>
            {
                _recoveryProtection = null;
                await SaveRecoveryAsync();
            });
        };
        RecoveryChanged(_recovery.Status);
        SetStatus(reason);
    }

    public async Task SaveRecoveryAsync()
    {
        _saveTimer.Stop();
        if (_recoveryProtection is not null) return;
        try { await _recovery.FlushAsync(); }
        catch (Exception error)
        {
            if (!_disposed) SetStatus("Recovery could not be saved. Export a catalog backup. " + error.Message);
        }
    }
}
