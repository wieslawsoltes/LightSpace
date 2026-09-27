using System.Threading.Channels;
using LightSpace.Catalog;
using LightSpace.Core;
using LightSpace.Editing;

internal static class RecoveryTests
{
    public static IReadOnlyList<(string Name, Func<Task> Run)> Cases =>
    [
        ("Recovery excludes active previews", PreviewExcludedAsync),
        ("Recovery drains commits arriving during a write", DrainLatestAsync),
        ("Recovery flush callers share one writer", SharedWriterAsync),
        ("Recovery failure keeps the previous acknowledgement and retries", RetryAsync),
        ("Recovery saved state clears immediately on preview", DirtyPreviewAsync),
        ("Recovery no-op gesture publishes its final state", NoOpGestureAsync),
        ("Recovery snapshot remains stable after later edits", SnapshotIsolatedAsync),
        ("Recovery loading a catalog supersedes an in-flight write", LoadDuringWriteAsync),
        ("Recovery preserves an edit committed while an earlier snapshot is saved", CommitDuringPreviewSaveAsync),
        ("Recovery synchronous storage completion is supported", SynchronousWriterAsync),
        ("Recovery disposal suppresses callbacks and rejects new flushes", DisposeAsync),
        ("Recovery clean flush performs no storage I/O", CleanFlushAsync)
    ];

    private static EditorSession Session()
    {
        var photo = new PhotoDocument { Name = "fixture", Original = [1, 2, 3], Width = 10, Height = 10 };
        return new(new CatalogDocument { Photos = [photo], ActivePhoto = photo.Id });
    }
    private static void Exposure(EditorSession session, float value) =>
        session.Edit("Exposure", state => state with { Develop = state.Develop with { Exposure = value } });
    private static void Preview(EditorSession session, float value) =>
        session.Preview(state => state with { Develop = state.Develop with { Exposure = value } });
    private static PhotoDocument Photo(string json) => CatalogSerializer.Deserialize(json).Photos[0];
    private static void Check(bool condition, string message = "Recovery invariant failed")
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static async Task PreviewExcludedAsync()
    {
        var session = Session(); var writer = new Writer();
        using var recovery = new RecoveryCoordinator(session, writer.WriteAsync);
        session.Edit("Rating", state => state with { Rating = 2 });
        Preview(session, .16f);
        var task = recovery.FlushAsync(); var pending = await writer.NextAsync();
        Check(Photo(pending.Json).State is { Rating: 2, Develop.Exposure: 0 });
        Check(session.Active!.State.Develop.Exposure == .16f, "Capturing recovery must not mutate the visible preview.");
        pending.Complete(); await task;
        Check(recovery.Status.State == "Preview" && recovery.Status.HasUnsavedChanges);
        session.CancelGesture(); Check(recovery.Status.State == "Saved");
    }
    private static async Task DrainLatestAsync()
    {
        var session = Session(); var writer = new Writer();
        using var recovery = new RecoveryCoordinator(session, writer.WriteAsync);
        Exposure(session, 1); var task = recovery.FlushAsync(); var first = await writer.NextAsync();
        Exposure(session, 2); Exposure(session, 3);
        first.Complete(); var latest = await writer.NextAsync();
        Check(recovery.Status.SavedRevision == 1 && recovery.Status.State != "Saved");
        Check(Photo(latest.Json).State.Develop.Exposure == 3);
        latest.Complete(); await task;
        Check(writer.Count == 2 && recovery.Status.SavedRevision == session.Revision && !recovery.Status.HasUnsavedChanges);
    }
    private static async Task SharedWriterAsync()
    {
        var session = Session(); var writer = new Writer();
        using var recovery = new RecoveryCoordinator(session, writer.WriteAsync);
        Exposure(session, 1); var first = recovery.FlushAsync(); var second = recovery.FlushAsync();
        Check(ReferenceEquals(first, second)); var pending = await writer.NextAsync();
        Check(writer.Count == 1); pending.Complete(); await Task.WhenAll(first, second);
    }
    private static async Task RetryAsync()
    {
        var session = Session(); var writer = new Writer();
        using var recovery = new RecoveryCoordinator(session, writer.WriteAsync);
        Exposure(session, 1); var task = recovery.FlushAsync(); var pending = await writer.NextAsync();
        pending.Fail(new IOException("Simulated quota failure"));
        try { await task; throw new InvalidOperationException("Write should fail."); } catch (IOException) { }
        Check(recovery.Status.State == "Failed" && recovery.Status.SavedRevision == 0 && recovery.Status.HasUnsavedChanges);
        var retry = recovery.FlushAsync(); var next = await writer.NextAsync();
        Check(next.Json == pending.Json); next.Complete(); await retry;
        Check(recovery.Status.State == "Saved" && recovery.Status.Error is null);
    }
    private static Task DirtyPreviewAsync()
    {
        var session = Session(); using var recovery = new RecoveryCoordinator(session, _ => Task.CompletedTask);
        var states = new List<string>(); recovery.StatusChanged += status => states.Add(status.State);
        Preview(session, .5f); Check(states.Last() == "Preview" && recovery.Status.HasUnsavedChanges);
        session.CancelGesture(); Check(states.Last() == "Saved" && !recovery.Status.HasUnsavedChanges);
        return Task.CompletedTask;
    }
    private static Task NoOpGestureAsync()
    {
        var session = Session(); using var recovery = new RecoveryCoordinator(session, _ => Task.CompletedTask);
        var states = new List<string>(); recovery.StatusChanged += status => states.Add(status.State);
        session.BeginGesture(); session.CommitGesture("No change");
        Check(states.SequenceEqual(new[] { "Preview", "Saved" })); Check(!session.CanUndo);
        return Task.CompletedTask;
    }
    private static Task SnapshotIsolatedAsync()
    {
        var session = Session(); Exposure(session, 1); var snapshot = session.CaptureCommittedSnapshot();
        Exposure(session, 2); session.CreateAlbum("Later");
        Check(snapshot.Revision == 1 && Photo(snapshot.Json).State.Develop.Exposure == 1);
        Check(CatalogSerializer.Deserialize(snapshot.Json).Albums.Count == 0);
        return Task.CompletedTask;
    }
    private static async Task LoadDuringWriteAsync()
    {
        var session = Session(); var writer = new Writer();
        using var recovery = new RecoveryCoordinator(session, writer.WriteAsync);
        Exposure(session, 1); var task = recovery.FlushAsync(); var old = await writer.NextAsync();
        var replacement = Session().Catalog; replacement.Photos[0].Name = "replacement"; session.Load(replacement);
        old.Complete(); var next = await writer.NextAsync(); Check(Photo(next.Json).Name == "replacement");
        next.Complete(); await task; Check(recovery.Status.SavedRevision == session.Revision);
    }
    private static async Task CommitDuringPreviewSaveAsync()
    {
        var session = Session(); var writer = new Writer();
        using var recovery = new RecoveryCoordinator(session, writer.WriteAsync);
        session.Edit("Rating", state => state with { Rating = 2 }); Preview(session, .16f);
        var task = recovery.FlushAsync(); var first = await writer.NextAsync();
        Preview(session, .8f); session.CommitGesture("Exposure"); first.Complete();
        var latest = await writer.NextAsync(); Check(Photo(first.Json).State.Develop.Exposure == 0);
        Check(Photo(latest.Json).State.Develop.Exposure == .8f); latest.Complete(); await task;
        Check(recovery.Status.State == "Saved" && recovery.Status.SavedRevision == 2);
    }
    private static async Task SynchronousWriterAsync()
    {
        var session = Session(); var writes = 0;
        using var recovery = new RecoveryCoordinator(session, _ => { writes++; return Task.CompletedTask; }, initiallySaved: false);
        Check(recovery.Status.State == "Pending"); await recovery.FlushAsync();
        Exposure(session, 1); await recovery.FlushAsync(); Check(writes == 2 && recovery.Status.State == "Saved");
    }
    private static async Task DisposeAsync()
    {
        var session = Session(); var writer = new Writer(); var callbacks = 0;
        var recovery = new RecoveryCoordinator(session, writer.WriteAsync);
        recovery.StatusChanged += _ => callbacks++;
        Exposure(session, 1); var task = recovery.FlushAsync(); var pending = await writer.NextAsync();
        recovery.Dispose(); var count = callbacks; Exposure(session, 2); pending.Complete(); await task;
        Check(callbacks == count && writer.Count == 1);
        try { await recovery.FlushAsync(); throw new InvalidOperationException("Disposed writer accepted a flush."); }
        catch (ObjectDisposedException) { }
    }
    private static async Task CleanFlushAsync()
    {
        var session = Session(); using var recovery = new RecoveryCoordinator(session, _ => throw new InvalidOperationException("Unexpected write"));
        await recovery.FlushAsync(); Check(recovery.Status.State == "Saved");
    }

    private sealed class Pending(string json)
    {
        private readonly TaskCompletionSource _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public string Json { get; } = json;
        public Task Task => _completion.Task;
        public void Complete() => _completion.SetResult();
        public void Fail(Exception error) => _completion.SetException(error);
    }
    private sealed class Writer
    {
        private readonly Channel<Pending> _pending = Channel.CreateUnbounded<Pending>();
        private int _count;
        public int Count => Volatile.Read(ref _count);
        public Task WriteAsync(string json)
        {
            Interlocked.Increment(ref _count); var pending = new Pending(json);
            if (!_pending.Writer.TryWrite(pending)) throw new InvalidOperationException("Test writer closed.");
            return pending.Task;
        }
        public Task<Pending> NextAsync() => _pending.Reader.ReadAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5));
    }
}
