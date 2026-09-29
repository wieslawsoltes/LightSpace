# Committed, content-addressed recovery

## Independent versions and migration

Catalog schema **5** records processing, including geometry/optics. Recovery manifest format **1** records source references. IndexedDB version **2** supplies workspace/original object stores. These are independent contracts; the 0.5 processing update does not require resetting or upgrading the database layout.

Restore reads the modern manifest first. With no manifest, the previous source-inclusive recovery is accepted as a legacy fallback. Reading it does not rewrite or delete it; the next committed save publishes the modern representation. Catalogs1–4 migrate with neutral missing fields to5. Portable `.lightspace` exports still embed all originals. Older builds reject new processing schemas, so retain pre-upgrade portable backups when old-version interoperability matters.

## Storage contract

`IRecoveryStore` defines asynchronous manifest reads, source-by-key reads and `CommitAsync(RecoveryWrite)`. A write includes the frozen edit manifest, every referenced source key and newly staged `RecoveryBlob` values. Keys are lowercase SHA-256 hashes of encoded original bytes.

`RecoveryPersistence` weakly memoizes source-array identity to hash. It copies committed catalog state, replaces original bytes in that copy with references and serializes metadata synchronously before storage starts. For an active gesture, the copied photo uses its opening state, not the visible preview. Original arrays are shared read-only; mutation in place violates persistence/cache/undo contracts.

Each reference includes expected encoded length. Identical originals share a stored blob and, after restore, a shared byte array. Manifest validation bounds metadata, photo counts, dimensions, IDs, source lengths and total referenced bytes before fetching sources. Restore checks actual length and hash before exposing the catalog, then primes the warm source cache.

The session, coordinator and persistence adapter have one logical owner, normally the UI synchronization context. They are not a thread-safe or cross-tab catalog database.

```csharp
var persistence = new RecoveryPersistence(store);
var restored = await persistence.RestoreAsync();
var session = new EditorSession(restored ?? initialCatalog);
using var recovery = RecoveryCoordinator.Incremental(session, persistence,
    initiallySaved: restored is not null);
recovery.StatusChanged += status => UpdateSaveIndicator(status);
await recovery.FlushAsync();
```

Pass the same restored persistence instance to `StudioView` to reuse verified source state. A host implementing only `IWorkspaceStorage` retains the legacy string-snapshot delegate through `new RecoveryCoordinator(session, storage.WriteRecoveryAsync, initiallySaved)`.

## Publication, acknowledgement and retry

Browser originals are Blobs in `originals`; the edit manifest is `workspace/manifest-v1`. Newly staged payloads are decoded, bounded and hash-checked before a transaction starts. A read-write transaction stages sources, checks all referenced keys without loading unchanged source values, publishes the manifest and removes the obsolete legacy record. Synchronous exceptions explicitly abort any already-enqueued requests. Completion, not starting a write, acknowledges publication. Strict transaction durability is requested and reported when supported.

Native `FileRecoveryStore` stages validated source files before replacing the manifest file. The previous manifest survives a failed publication. It validates every referenced file exists before replacement; staged orphans may remain. File replacement is not a cross-process database transaction or a power-loss-proof write journal.

Only a successfully completed snapshot advances SavedRevision. Newer commits are drained after the in-flight write, overlapping FlushAsync callers share one writer, and active previews remain unsaved even when their preceding committed revision is durable. A failure preserves the earlier acknowledgement and clears known-source assumptions so retry can restage originals after missing-key or eviction failures.

Warm commits check existence, not hashes of every stored original. Restore detects altered/truncated data. This avoids repeated original reads but is not continuous corruption monitoring. [Publication validation and rollback tests](RECOVERY-PUBLICATION.md)

## User protection

The footer independently reports Saved, Pending, Editing, Saving or Failed; click to flush/retry. Browser unload protection follows immediate unsaved state, not periodic test diagnostics. Browser/OS termination can bypass it, and native close-time flushing is not guaranteed.

Unreadable manifests, missing/corrupt originals and unsupported legacy recovery never cause automatic replacement with demonstration photos. The old recovery stays protected until explicit **Replace recovery** confirmation. That action saves the current workspace; it does not repair a damaged older catalog. Preserve the browser profile/native directory for manual recovery when needed.

The current native layout is the application-data `LightSpace/recovery-manifest-v1.json` plus `originals-v1`; `recovery.lightspace` remains a legacy fallback. Browser data is origin/profile-local. Stores are unencrypted and do not merge competing tabs. Keep original files and portable backups.

## Limits

Metadata is capped at64MiB, a source at64MiB and catalog encoded-source references at256MiB. These are safety ceilings, not recommended loads. Startup still restores all referenced sources; first storage/restore still hashes and transfers original bytes. Browser staging uses base64 interop before storing Blobs. Warm metadata saves avoid that original-byte path, not all serialization or storage I/O.

Orphan sources are deliberately retained. Automatic compaction/garbage collection, paging, encryption, append-only journaling, multi-tab merge and cloud sync are not implemented. Several catalogs can leave storage beyond one active catalog's referenced size.

## Evidence

Controlled asynchronous engine tests cover delayed writes, preview/cancel, shared flushes, failures/retry, loading a replacement catalog, synchronous delegates and disposal. Incremental tests validate source identity/deduplication, integrity, missing-key retry, frozen manifests and native atomic publication.

Browser tests inspect actual IndexedDB snapshots while a real slider gesture crosses an earlier edit's autosave deadline, verify final values after reload, test Escape, inject publication failures and protect unreadable recovery. The full slider-crash regression also checks every saved adjustment after restart. New geometry/optics follow the same committed-state boundary.

Engine `recovery-performance.json` measures twenty warm commits with an8MiB source through an in-memory store. Browser counters separately verify avoided source reads/writes/hashing. Timings do not certify disk latency, total memory, physical GPUs or all browser engines.
