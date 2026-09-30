using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using LightSpace.Core;
using LightSpace.Catalog;
using LightSpace.Editing;
using LightSpace.Rendering.Skia;
using LightSpace.Storage;
using SkiaSharp;
using static Fixtures;

internal static class VirtualCopyTests
{
    private static void Reject<T>(Action action) where T : Exception
    {
        try { action(); } catch (T) { return; }
        throw new Exception("Expected " + typeof(T).Name);
    }
    public static void Register(Action<string, Action> test)
    {
        test("Virtual copy shares source but owns independent editing identity", () =>
        {
            var s = new EditorSession(Catalog()); var master = s.Active!;
            s.Edit("Exposure", p => p with { Develop = p.Develop with { Exposure = .5f } }); s.SaveVersion("Start");
            var copy = s.CreateVirtualCopies([master.Id]).Single();
            Check(copy.Id != master.Id && copy.MasterPhotoId == master.Id && copy.CopyName == "Copy 1");
            Check(ReferenceEquals(copy.Original, master.Original) && ReferenceEquals(copy.State, master.State));
            Check(copy.Versions.Count == 0 && master.Versions.Count == 1);
            s.Edit("Copy look", p => p with { Develop = p.Develop with { Exposure = 2 }, Rating = 2 });
            Check(master.State.Develop.Exposure == .5f && copy.State.Develop.Exposure == 2 && master.State.Rating == 0);
        });
        test("Copy of a copy resolves to the original root and fresh family name", () =>
        {
            var s = new EditorSession(Catalog()); var master = s.Active!;
            var first = s.CreateVirtualCopies([master.Id]).Single(); s.Edit("Look", p => p with { Rating = 3 });
            var second = s.CreateVirtualCopies([first.Id]).Single();
            Check(second.MasterPhotoId == master.Id && second.CopyName == "Copy 2" && second.State.Rating == 3);
            Check(ReferenceEquals(first.Original, second.Original));
        });
        test("Copy creation commits the opening gesture before capturing edits", () =>
        {
            var s = new EditorSession(Catalog()); var master = s.Active!;
            s.Preview(p => p with { Develop = p.Develop with { Exposure = 1.3f } });
            var copy = s.CreateVirtualCopies([master.Id]).Single();
            Check(!s.HasActiveGesture && copy.State.Develop.Exposure == 1.3f && s.History.Count == 2);
            s.Undo(); Check(s.Catalog.Photos.Count == 2 && master.State.Develop.Exposure == 1.3f); s.Undo(); Check(master.State.Develop.Exposure == 0);
        });
        test("Batch copies deduplicate targets and inherit album ordering", () =>
        {
            var c = Catalog(3); var ids = c.Photos.Select(p => p.Id).ToArray();
            c.Albums.Add(new() { Photos = [ids[2], ids[0], ids[1]] }); var s = new EditorSession(c);
            s.Select(ids[2]); s.Selection.UnionWith(ids);
            var copies = s.CreateVirtualCopies(ids.Concat(ids));
            Check(copies.Count == 3 && s.Revision == 1 && s.History.Count == 1 && s.Active!.MasterPhotoId == ids[2]);
            var expected = new[] { ids[2], copies[2].Id, ids[0], copies[0].Id, ids[1], copies[1].Id };
            Check(c.Albums[0].Photos.SequenceEqual(expected));
            s.Undo(); Check(c.Photos.Select(p => p.Id).SequenceEqual(ids) && s.Selection.SetEquals(ids) && c.ActivePhoto == ids[2]);
            s.Redo(); Check(c.Albums[0].Photos.SequenceEqual(expected) && s.Selection.SetEquals(copies.Select(p => p.Id)));
        });
        test("Undo copy creation preserves unrelated later imports and albums", () =>
        {
            var s = new EditorSession(Catalog()); var master = s.Active!; var copy = s.CreateVirtualCopies([master.Id]).Single();
            var album = s.CreateAlbum("Later"); var imported = Tiny(); s.Add(imported);
            s.Undo(); Check(s.Catalog.Photos.Contains(imported) && !s.Catalog.Photos.Contains(copy) && s.Catalog.Albums.Contains(album));
            Check(album.Photos.Count == 0); s.Redo(); Check(album.Photos.SequenceEqual(new[] { copy.Id }));
        });
        test("Copy removal is undoable and never deletes masters or source bytes", () =>
        {
            var s = new EditorSession(Catalog()); var master = s.Active!; var copy = s.CreateVirtualCopies([master.Id]).Single();
            s.Edit("Look", p => p with { Rating = 4 }); var album = s.CreateAlbum("Variants");
            var before = s.Revision; s.RemoveVirtualCopies([copy.Id]);
            Check(s.Revision == before + 1 && s.Active == master && s.Catalog.Photos.Contains(master) && album.Photos.Count == 0);
            s.Undo(); Check(s.Active == copy && copy.State.Rating == 4 && album.Photos.Contains(copy.Id) && ReferenceEquals(master.Original, copy.Original));
            s.Redo(); Check(s.Active == master && !s.Catalog.Photos.Contains(copy));
        });
        test("Mixed master/copy removal rejects the entire operation", () =>
        {
            var s = new EditorSession(Catalog()); var master = s.Active!; var copy = s.CreateVirtualCopies([master.Id]).Single(); var revision = s.Revision;
            Reject<InvalidOperationException>(() => s.RemoveVirtualCopies([copy.Id, master.Id]));
            Check(s.Revision == revision && s.Catalog.Photos.Contains(copy) && s.Catalog.Photos.Contains(master));
        });
        test("Missing copy inputs reject before changing an active gesture", () =>
        {
            var s = new EditorSession(Catalog()); s.Preview(p => p with { Rating = 2 });
            Reject<InvalidOperationException>(() => s.CreateVirtualCopies([s.Active!.Id, Guid.NewGuid()]));
            Check(s.HasActiveGesture && s.Revision == 0 && s.Catalog.Photos.Count == 2);
        });
        test("Renaming is metadata-only and undoable with family uniqueness", () =>
        {
            var s = new EditorSession(Catalog()); var master = s.Active!; var first = s.CreateVirtualCopies([master.Id]).Single(); var second = s.CreateVirtualCopies([master.Id]).Single();
            var state = second.State; s.RenameVirtualCopy(second.Id, "  Monochrome  ");
            Check(second.CopyName == "Monochrome" && ReferenceEquals(state, second.State));
            s.Undo(); Check(second.CopyName == "Copy 2"); s.Redo(); Check(second.CopyName == "Monochrome");
            Reject<InvalidOperationException>(() => s.RenameVirtualCopy(first.Id, "monochrome"));
            var revision = s.Revision; s.RenameVirtualCopy(second.Id, "Monochrome"); Check(s.Revision == revision);
            Reject<ArgumentException>(() => s.RenameVirtualCopy(second.Id, "\n"));
            Reject<ArgumentException>(() => s.RenameVirtualCopy(second.Id, new string('a', 101)));
        });
        test("Copy no-ops preserve history and revision", () =>
        {
            var s = new EditorSession(Catalog()); Check(s.CreateVirtualCopies([]).Count == 0); s.RemoveVirtualCopies([]);
            Check(s.Revision == 0 && !s.CanUndo);
        });
        test("Portable virtual copies omit source bytes and hydrate a common array", () =>
        {
            var s = new EditorSession(Catalog(1)); var master = s.Active!; var copy = s.CreateVirtualCopies([master.Id]).Single(); s.RenameVirtualCopy(copy.Id, "Warm");
            var json = CatalogSerializer.Serialize(s.Catalog); using var doc = JsonDocument.Parse(json);
            Check(doc.RootElement.GetProperty("SchemaVersion").GetInt32() == 6);
            Check(doc.RootElement.GetProperty("Photos")[1].GetProperty("Original").GetString() == "");
            Check(master.Original.Length > 0 && copy.Original.Length > 0, "Serialization must not mutate live source arrays.");
            var loaded = CatalogSerializer.Deserialize(json);
            Check(ReferenceEquals(loaded.Photos[0].Original, loaded.Photos[1].Original) && loaded.Photos[1].CopyName == "Warm");
            Check(loaded.Photos[1].MasterPhotoId == master.Id && loaded.Photos[1].DisplayName.EndsWith(" · Warm"));
        });
        test("Portable rejects cycles, missing masters, conflicts and legacy identity fields", () =>
        {
            var s = new EditorSession(Catalog(1)); var copy = s.CreateVirtualCopies([s.Active!.Id]).Single(); var text = CatalogSerializer.Serialize(s.Catalog);
            foreach (var mutation in new Action<JsonNode>[]
            {
                n => n["Photos"]![1]!["MasterPhotoId"] = copy.Id,
                n => n["Photos"]![1]!["MasterPhotoId"] = Guid.NewGuid(),
                n => n["Photos"]![1]!["Original"] = Convert.ToBase64String(new byte[] { 1, 2, 3 }),
                n => n["Photos"]![1]!["Width"] = 999,
                n => n["Photos"]![1]!["CopyName"] = " ",
                n => n["SchemaVersion"] = 5
            })
            { var node = JsonNode.Parse(text)!; mutation(node); Throws(() => CatalogSerializer.Deserialize(node.ToJsonString())); }
        });
        test("Copy snapshot retains identity in committed recovery and preserves opening preview", () =>
        {
            var s = new EditorSession(Catalog(1)); var copy = s.CreateVirtualCopies([s.Active!.Id]).Single();
            s.Preview(p => p with { Develop = p.Develop with { Exposure = 3 } });
            var restored = CatalogSerializer.Deserialize(s.CaptureCommittedSnapshot().Json);
            Check(restored.Photos[1].MasterPhotoId == copy.MasterPhotoId && restored.Photos[1].State.Develop.Exposure == 0);
            Check(copy.State.Develop.Exposure == 3);
        });
        test("Virtual copy and original retain distinct pixels with one decoded source", () =>
        {
            var s = new EditorSession(Catalog(1)); var master = s.Active!; var copy = s.CreateVirtualCopies([master.Id]).Single();
            s.Edit("Exposure", p => p with { Develop = p.Develop with { Exposure = 1 } });
            using var r = new PhotoRenderer(); using var surface = SKSurface.Create(new SKImageInfo(96, 64));
            r.Draw(surface.Canvas, master, SKRect.Create(96, 64)); using var a = surface.Snapshot(); using var original = SKBitmap.FromImage(a);
            r.Draw(surface.Canvas, copy, SKRect.Create(96, 64)); using var b = surface.Snapshot(); using var edited = SKBitmap.FromImage(b);
            Check(edited.GetPixel(20, 20).Red > original.GetPixel(20, 20).Red + 15);
            Check(r.Statistics.ImageDecodes == 1 && r.Statistics.CachedSources == 1);
            var before = r.Statistics; s.RenameVirtualCopy(copy.Id, "Bright"); r.Draw(surface.Canvas, copy, SKRect.Create(96, 64));
            Check(r.Statistics.ImageDecodes == before.ImageDecodes && r.Statistics.ShaderBuilds == before.ShaderBuilds);
        });
        test("One hundred copies retain one source and a compact portable catalog", () =>
        {
            var s = new EditorSession(Catalog(1)); var master = s.Active!; master.Original = new byte[8 * 1024 * 1024];
            var baseline = CatalogSerializer.Serialize(s.Catalog).Length; var start = GC.GetAllocatedBytesForCurrentThread(); var timer = Stopwatch.StartNew();
            for (var i = 0; i < 100; i++) s.CreateVirtualCopies([master.Id]);
            timer.Stop(); var allocated = GC.GetAllocatedBytesForCurrentThread() - start;
            Check(s.Catalog.Photos.All(p => ReferenceEquals(p.Original, master.Original)));
            var json = CatalogSerializer.Serialize(s.Catalog); Check(json.Length - baseline < 1024 * 1024);
            Check(VirtualCopyCatalog.SourceBytes(s.Catalog) == master.Original.Length);
            Directory.CreateDirectory("artifacts/engine"); File.WriteAllText("artifacts/engine/virtual-copy-performance.json", JsonSerializer.Serialize(new
            { copies = 100, sourceBytes = master.Original.Length, sharedSourceArrays = 1, creationMilliseconds = timer.Elapsed.TotalMilliseconds,
                creationAllocatedBytes = allocated, portableAddedCharacters = json.Length - baseline,
                scope = "100 copy creations and portable serialization over one 8 MiB source; source sharing and metadata growth, not GPU timing." }));
        });
    }

    public static IReadOnlyList<(string Name, Func<Task> Run)> AsyncCases =>
    [
        ("Virtual copy recovery writes no additional source and restores sharing", async () =>
        {
            var s = new EditorSession(Catalog(1)); var master = s.Active!; var store = new MemoryStore(); var p = new RecoveryPersistence(store);
            await p.CommitAsync(p.Capture(s)); var first = p.Statistics;
            var copy = s.CreateVirtualCopies([master.Id]).Single(); s.RenameVirtualCopy(copy.Id, "Alternative");
            await p.CommitAsync(p.Capture(s));
            Check(p.Statistics.BlobWrites == first.BlobWrites && p.Statistics.BytesHashed == first.BytesHashed);
            var restored = (await new RecoveryPersistence(store).RestoreAsync())!;
            Check(restored.Photos[1].CopyName == "Alternative" && ReferenceEquals(restored.Photos[0].Original, restored.Photos[1].Original));
            var manifest = JsonNode.Parse(store.Manifest!)!;
            manifest["Sources"]![copy.Id.ToString()]!["Key"] = new string('a', 64);
            Throws(() => RecoveryManifest.Parse(manifest.ToJsonString()));
        }),
        ("Removing and undoing virtual copies retain persisted source payloads", async () =>
        {
            var s = new EditorSession(Catalog(1)); var store = new MemoryStore(); var p = new RecoveryPersistence(store);
            var copy = s.CreateVirtualCopies([s.Active!.Id]).Single(); await p.CommitAsync(p.Capture(s));
            s.RemoveVirtualCopies([copy.Id]); await p.CommitAsync(p.Capture(s));
            Check(RecoveryManifest.Parse(store.Manifest!).Catalog.Photos.Count == 1 && store.Blobs.Count == 1);
            s.Undo(); await p.CommitAsync(p.Capture(s)); Check(p.Statistics.BlobWrites == 1 && store.Blobs.Count == 1);
        })
    ];
    private sealed class MemoryStore : IRecoveryStore
    {
        public string? Manifest; public Dictionary<string, byte[]> Blobs = [];
        public Task<string?> ReadManifestAsync() => Task.FromResult(Manifest);
        public Task<byte[]?> ReadBlobAsync(string key) => Task.FromResult(Blobs.GetValueOrDefault(key));
        public Task CommitAsync(RecoveryWrite write)
        {
            foreach (var blob in write.Blobs) Blobs[blob.Key] = blob.Bytes;
            if (write.References.Any(k => !Blobs.ContainsKey(k))) throw new InvalidDataException("Missing source");
            Manifest = write.Manifest; return Task.CompletedTask;
        }
    }
}
