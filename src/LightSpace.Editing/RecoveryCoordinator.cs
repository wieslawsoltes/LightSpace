namespace LightSpace.Editing;

/// <summary>
/// Revision-aware, single-writer recovery. The session and this coordinator are
/// confined to one logical owner (normally the UI synchronization context).
/// The host supplies debounce scheduling; FlushAsync captures committed state
/// synchronously and acknowledges only a successfully completed storage write.
/// </summary>
public sealed class RecoveryCoordinator : IDisposable
{
    private readonly EditorSession _session;
    private readonly Func<CommittedCatalogSnapshot, Task> _writeAsync;
    private readonly Func<CommittedCatalogSnapshot> _capture;
    private Task? _drain;
    private long _savedRevision;
    private string? _error;
    private bool _disposed;
    private RecoveryStatus? _published;

    public RecoveryCoordinator(EditorSession session, Func<string, Task> writeAsync, bool initiallySaved = true)
        : this(session, () => session.CaptureCommittedSnapshot(), snapshot => writeAsync(snapshot.Json), initiallySaved)
    { ArgumentNullException.ThrowIfNull(writeAsync); }

    public static RecoveryCoordinator Incremental(EditorSession session, RecoveryPersistence persistence, bool initiallySaved = true)
        => new(session, () => persistence.Capture(session), persistence.CommitAsync, initiallySaved);

    private RecoveryCoordinator(EditorSession session, Func<CommittedCatalogSnapshot> capture, Func<CommittedCatalogSnapshot, Task> writeAsync, bool initiallySaved)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(writeAsync);
        _session = session; _capture = capture;
        _writeAsync = writeAsync;
        _savedRevision = initiallySaved ? session.Revision : -1;
        session.Changed += Publish;
        session.ViewChanged += Publish;
    }

    public RecoveryStatus Status => new(_session.Revision, _savedRevision,
        _session.HasActiveGesture, _drain is not null, _error);

    public event Action<RecoveryStatus>? StatusChanged;

    /// <summary>
    /// Writes the latest committed state. Concurrent calls join the same drain.
    /// Commits arriving while a write is in flight are captured by a subsequent
    /// write, never by mutating that in-flight payload. Previews remain unsaved.
    /// A failed write keeps the previous acknowledgement and can be retried.
    /// </summary>
    public Task FlushAsync()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_drain is not null) return _drain;
        if (_savedRevision == _session.Revision && _error is null) return Task.CompletedTask;

        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _drain = completion.Task;
        _error = null;
        Publish();
        _ = DrainAsync(completion);
        return completion.Task;
    }

    private async Task DrainAsync(TaskCompletionSource completion)
    {
        Exception? failure = null;
        try
        {
            while (!_disposed && _savedRevision != _session.Revision)
            {
                var snapshot = _capture();
                await _writeAsync(snapshot);
                _savedRevision = snapshot.Revision;
                Publish();
            }
        }
        catch (Exception error)
        {
            _error = error.Message;
            failure = error;
        }
        finally
        {
            _drain = null;
            Publish();
        }
        if (failure is null) completion.TrySetResult();
        else completion.TrySetException(failure);
    }

    private void Publish()
    {
        if (_disposed) return;
        var status = Status;
        if (status == _published) return;
        _published = status;
        StatusChanged?.Invoke(status);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _session.Changed -= Publish;
        _session.ViewChanged -= Publish;
        // An already-started atomic write may finish; no new write or UI callback
        // is started after disposal. Hosts should await FlushAsync before closing.
        StatusChanged = null;
    }
}
