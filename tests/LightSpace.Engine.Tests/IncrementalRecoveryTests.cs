using System.Text;
using System.Text.Json;
using LightSpace.Core;
using LightSpace.Catalog;
using LightSpace.Editing;
using LightSpace.Storage;

internal static class IncrementalRecoveryTests
{
    public static IReadOnlyList<(string Name, Func<Task> Run)> Cases =>
    [
        ("Recovery manifests exclude originals and preserve committed previews", ExcludePreview),
        ("Recovery deduplicates identical source content", Deduplicate),
        ("Recovery metadata commits perform no additional source writes or hashes", WarmMetadata),
        ("Recovery restore validates originals and primes the warm cache", RoundTrip),
        ("Recovery rejects malformed manifests before reading originals", InvalidManifest),
        ("Recovery rejects missing original without acknowledging it", MissingOriginal),
        ("Recovery rejects corrupted original hashes", CorruptOriginal),
        ("Recovery failure restages source bytes on retry", FailedWrite),
        ("Recovery drains later revisions using immutable manifests", DrainRevisions),
        ("Recovery serializes original replacement under reused photo identity", ReplaceOriginal),
        ("File recovery commits sources before atomically publishing a manifest", FileCommit),
        ("File recovery does not publish a dangling manifest", FileMissingReference),
        ("File recovery rejects path traversal and false content hashes", FileReject),
        ("Recovery content source keys are shared across duplicate photos", SharedRestore)
    ];
    private static EditorSession Session(int size = 4096)
    {
        var original = new byte[size]; new Random(173).NextBytes(original);
        var photo = new PhotoDocument { Name = "fixture", Width = 320, Height = 200, Original = original };
        return new(new CatalogDocument { Photos = [photo], ActivePhoto = photo.Id });
    }
    private static void Check(bool condition) => Fixtures.Check(condition);
    private static async Task Reject(Func<Task> action)
    {
        try { await action(); } catch (InvalidDataException) { return; }
        throw new InvalidOperationException("Expected invalid recovery to be rejected.");
    }
    private static async Task ExcludePreview()
    {
        var s = Session(); var store = new MemoryStore(); var persistence = new RecoveryPersistence(store);
        s.Edit("Rating", p => p with { Rating = 2 }); s.Preview(p => p with { Develop = p.Develop with { Exposure = 1 } });
        var snapshot = persistence.Capture(s); var manifest = RecoveryManifest.Parse(snapshot.Json);
        Check(manifest.Catalog.Photos[0].Original.Length == 0 && manifest.Catalog.Photos[0].State.Develop.Exposure == 0);
        Check(s.Active!.State.Develop.Exposure == 1 && snapshot.Revision == 1);
        await persistence.CommitAsync(snapshot); var restored = await new RecoveryPersistence(store).RestoreAsync();
        Check(restored!.Photos[0].State.Rating == 2 && restored.Photos[0].State.Develop.Exposure == 0);
    }
    private static async Task Deduplicate()
    {
        var s = Session(); s.Add(new() { Name = "duplicate", Width = 320, Height = 200, Original = s.Active!.Original.ToArray() });
        var store = new MemoryStore(); var p = new RecoveryPersistence(store); await p.CommitAsync(p.Capture(s));
        Check(store.Blobs.Count == 1 && store.Writes == 1 && RecoveryManifest.Parse(store.Manifest!).Sources.Count == 2);
    }
    private static async Task WarmMetadata()
    {
        var s = Session(8 * 1024 * 1024); var store = new MemoryStore(); var p = new RecoveryPersistence(store);
        await p.CommitAsync(p.Capture(s)); var warm = p.Statistics; var before = GC.GetAllocatedBytesForCurrentThread();
        var timer = System.Diagnostics.Stopwatch.StartNew();
        for (var i = 0; i < 20; i++) { s.Edit("Caption", state => state with { Caption = "Photo " + i }); await p.CommitAsync(p.Capture(s)); }
        timer.Stop(); var after = p.Statistics; var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        var length = Encoding.UTF8.GetByteCount(store.Manifest!);
        Check(after.BlobWrites == warm.BlobWrites && after.BytesHashed == warm.BytesHashed && store.Reads == 0 && length < 16384);
        Directory.CreateDirectory("artifacts/engine");
        File.WriteAllText("artifacts/engine/recovery-performance.json", JsonSerializer.Serialize(new
        {
            sourceBytes = s.Active!.Original.Length, metadataCommits = 20, manifestBytesPerCommit = length,
            additionalBlobWrites = after.BlobWrites - warm.BlobWrites, additionalBytesHashed = after.BytesHashed - warm.BytesHashed,
            elapsedCpuMilliseconds = timer.Elapsed.TotalMilliseconds, managedBytes = allocated,
            scope = "Warm manifest capture and in-memory commits; not disk latency, IndexedDB, startup or GPU timing."
        }));
    }
    private static async Task RoundTrip()
    {
        var s = Session(); s.Edit("Exposure", state => state with { Develop = state.Develop with { Exposure = .75f }, Caption = "hello" });
        var store = new MemoryStore(); var writer = new RecoveryPersistence(store); await writer.CommitAsync(writer.Capture(s));
        var reader = new RecoveryPersistence(store); var catalog = (await reader.RestoreAsync())!;
        Check(catalog.Photos[0].Original.SequenceEqual(s.Active!.Original) && catalog.Photos[0].State.Caption == "hello");
        var warm = reader.Statistics; await reader.CommitAsync(reader.Capture(new(catalog)));
        Check(reader.Statistics.BlobWrites == 0 && reader.Statistics.BytesHashed == warm.BytesHashed);
    }
    private static async Task InvalidManifest()
    {
        var s = Session(); var store = new MemoryStore(); var p = new RecoveryPersistence(store); var json = p.Capture(s).Json;
        foreach (var mutation in new[] { json.Replace("\"Version\":1", "\"Version\":999"), json.Replace("\"Width\":320", "\"Width\":0"), json.Replace("\"Length\":4096", "\"Length\":-1"), json.Replace("\"Format\":\"LightSpace.Recovery\"", "\"Format\":\"Other\"") })
        {
            store.Manifest = mutation; await Reject(async () => { await p.RestoreAsync(); }); Check(store.Reads == 0);
        }
    }
    private static async Task MissingOriginal()
    {
        var s = Session(); var store = new MemoryStore(); var p = new RecoveryPersistence(store); store.Manifest = p.Capture(s).Json;
        await Reject(async () => { await p.RestoreAsync(); }); Check(p.Statistics.BlobWrites == 0);
    }
    private static async Task CorruptOriginal()
    {
        var s = Session(); var store = new MemoryStore(); var p = new RecoveryPersistence(store); await p.CommitAsync(p.Capture(s));
        var key = store.Blobs.Keys.Single(); store.Blobs[key] = new byte[4096];
        await Reject(async () => { await new RecoveryPersistence(store).RestoreAsync(); });
    }
    private static async Task FailedWrite()
    {
        var s = Session(); var store = new MemoryStore(); var p = new RecoveryPersistence(store);
        using var recovery = RecoveryCoordinator.Incremental(s, p, false); await recovery.FlushAsync();
        store.Blobs.Clear(); s.Edit("Rating", state => state with { Rating = 3 });
        await Reject(recovery.FlushAsync); Check(recovery.Status.State == "Failed" && recovery.Status.SavedRevision == 0);
        await recovery.FlushAsync(); Check(recovery.Status.State == "Saved" && store.Blobs.Count == 1);
    }
    private static async Task DrainRevisions()
    {
        var s = Session(); var store = new MemoryStore(); var gate = new TaskCompletionSource(); var entered = new TaskCompletionSource();
        store.Delay = async () => { entered.SetResult(); await gate.Task; store.Delay = null; };
        var p = new RecoveryPersistence(store); using var recovery = RecoveryCoordinator.Incremental(s, p, false);
        var flush = recovery.FlushAsync(); await entered.Task; s.Edit("Rating", state => state with { Rating = 5 }); gate.SetResult(); await flush;
        Check(recovery.Status.SavedRevision == 1 && store.Commits == 2 && store.Writes == 1);
        Check(RecoveryManifest.Parse(store.Manifest!).Catalog.Photos[0].State.Rating == 5);
    }
    private static async Task ReplaceOriginal()
    {
        var s = Session(); var store = new MemoryStore(); var p = new RecoveryPersistence(store); await p.CommitAsync(p.Capture(s));
        s.Active!.Original = [9, 8, 7]; await p.CommitAsync(p.Capture(s));
        var restored = await new RecoveryPersistence(store).RestoreAsync(); Check(restored!.Photos[0].Original.SequenceEqual(new byte[] { 9, 8, 7 }));
    }
    private static async Task WithDirectory(Func<string, Task> action)
    {
        var dir = Path.Combine(Path.GetTempPath(), "LightSpace-" + Guid.NewGuid().ToString("N"));
        try { await action(dir); } finally { if (Directory.Exists(dir)) Directory.Delete(dir, true); }
    }
    private static Task FileCommit() => WithDirectory(async dir =>
    {
        var store = new FileRecoveryStore(dir); var p = new RecoveryPersistence(store); var s = Session(); await p.CommitAsync(p.Capture(s));
        var loaded = await new RecoveryPersistence(new FileRecoveryStore(dir)).RestoreAsync(); Check(loaded!.Photos[0].Original.SequenceEqual(s.Active!.Original));
        Check(Directory.GetFiles(dir, "*.tmp", SearchOption.AllDirectories).Length == 0);
    });
    private static Task FileMissingReference() => WithDirectory(async dir =>
    {
        var store = new FileRecoveryStore(dir); var p = new RecoveryPersistence(store); var s = Session(); await p.CommitAsync(p.Capture(s));
        var old = await store.ReadManifestAsync(); var key = new string('a', 64);
        await Reject(() => store.CommitAsync(new("invalid-next", [key], []))); Check(await store.ReadManifestAsync() == old);
    });
    private static Task FileReject() => WithDirectory(async dir =>
    {
        var store = new FileRecoveryStore(dir);
        await Reject(async () => { await store.ReadBlobAsync("../escape"); });
        await Reject(() => store.CommitAsync(new("test", [new string('a', 64)], [new(new string('a', 64), [1, 2, 3])])));
        Check(await store.ReadManifestAsync() is null);
    });
    private static async Task SharedRestore()
    {
        var s = Session(); var source = s.Active!.Original; s.Add(new() { Original = source, Width = 320, Height = 200 });
        var store = new MemoryStore(); var p = new RecoveryPersistence(store); await p.CommitAsync(p.Capture(s));
        var restored = (await new RecoveryPersistence(store).RestoreAsync())!;
        Check(ReferenceEquals(restored.Photos[0].Original, restored.Photos[1].Original) && store.Reads == 1);
    }
    private sealed class MemoryStore : IRecoveryStore
    {
        public string? Manifest; public Dictionary<string, byte[]> Blobs { get; } = []; public int Reads, Writes, Commits;
        public Func<Task>? Delay;
        public Task<string?> ReadManifestAsync() => Task.FromResult(Manifest);
        public Task<byte[]?> ReadBlobAsync(string key) { Reads++; return Task.FromResult(Blobs.GetValueOrDefault(key)?.ToArray()); }
        public async Task CommitAsync(RecoveryWrite write)
        {
            if (Delay is not null) await Delay();
            foreach (var key in write.References)
                if (!Blobs.ContainsKey(key) && !write.Blobs.Any(b => b.Key == key)) throw new InvalidDataException("Missing source.");
            foreach (var blob in write.Blobs) { Blobs[blob.Key] = blob.Bytes.ToArray(); Writes++; }
            Manifest = write.Manifest; Commits++;
        }
    }
}
