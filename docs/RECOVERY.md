# Committed, content-addressed recovery

## Independent versions and migration

Catalog schema **6** adds original/virtual-copy relationships to existing processing. Recovery manifest format **1** records source references; IndexedDB version **2** supplies workspace/original stores. The 0.8 update does not require resetting or upgrading the database layout.

Restore reads the modern manifest first. With no manifest, source-inclusive legacy recovery remains a fallback. Reading legacy data does not rewrite/delete it; the next committed save publishes the modern representation. Catalogs 1–5 migrate as original-only records. Portable schema-6 catalogs embed original bytes once per family; copies reference their root and hydrate a common array on load. Older builds reject schema 6. Retain pre-upgrade portable backups for older-version interoperability.

## Storage and family contract

`IRecoveryStore` defines asynchronous manifest reads, source-by-key reads and `CommitAsync(RecoveryWrite)`. A write includes the frozen manifest, every referenced key and newly staged `RecoveryBlob` values. Keys are lowercase SHA-256 hashes of encoded original bytes.

`RecoveryPersistence` weakly memoizes source-array identity to hash. It copies committed state, substitutes source references in the copy and serializes metadata before storage starts. Active gestures contribute their opening state rather than visible previews. Source arrays remain read-only; in-place mutation violates persistence, cache and undo contracts.

Each reference includes an expected encoded length. Identical originals share a stored blob and, after restore, a byte array. Copies must resolve directly to an existing original with matching dimensions and source key/length. Missing roots, self/copy chains, duplicate family names and conflicting references are rejected before source retrieval/publication. Managed, browser and native validation retain these family constraints. Unique source keys count once toward the manifest's encoded-source budget.

Restore checks actual source length and hash before exposing the catalog, then primes the warm source cache. Adding/renaming a virtual copy of an already persisted root does not require hashing or writing that same original again; metadata publication and source-key checks still occur. See [virtual-copy validation](VIRTUAL-COPIES.md).

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

Pass the restored persistence instance to `StudioView` to reuse verified sources. A host implementing only `IWorkspaceStorage` retains the legacy string-snapshot delegate through `new RecoveryCoordinator(session, storage.WriteRecoveryAsync, initiallySaved)`. Its portable snapshot uses the same validated copy/root representation.

## Publication, acknowledgement and retry

Browser originals are Blobs in `originals`; the edit manifest is `workspace/manifest-v1`. Newly staged payloads are decoded, bounded and hash-checked before a transaction. One read-write transaction stages sources, checks referenced keys without loading unchanged source values, publishes the manifest and removes obsolete legacy data. Synchronous exceptions explicitly abort queued requests. Completion—not starting a write—acknowledges publication. Strict transaction durability is requested/reported when supported.

Native `FileRecoveryStore` stages validated source files before replacing the manifest. It checks referenced files before publication, and the preceding manifest survives a failed replacement. Staged orphans can remain. File replacement is not a cross-process database transaction or a power-loss-proof journal.

Only successful snapshots advance SavedRevision. Newer commits are drained afterward; overlapping FlushAsync callers share one writer. Active previews remain unsaved even when the preceding committed revision is durable. Failure preserves the previous acknowledgement and clears known-source assumptions so retry can restage originals after missing-key/eviction failures.

Warm commits check source existence, not a hash of every stored original. Restore detects altered/truncated content. This avoids repeatedly reading originals but is not continuous corruption monitoring. [Publication and rollback checks](RECOVERY-PUBLICATION.md)

## User protection

The footer independently reports Saved, Pending, Editing, Saving or Failed; click to flush/retry. Browser unload protection follows immediate unsaved state, not periodic diagnostics. Browser/OS termination can bypass it, and native close-time flushing is not guaranteed.

Unreadable manifests, invalid families, missing/corrupt originals and unsupported legacy data never cause automatic replacement with demonstration photos. Recovery remains protected until explicit **Replace recovery** confirmation. That saves the current workspace; it does not repair the older catalog. Preserve the browser profile/native directory for manual recovery when needed.

Native storage uses the application-data `LightSpace/recovery-manifest-v1.json` and `originals-v1`; `recovery.lightspace` remains a legacy fallback. Browser data is origin/profile-local. Stores are unencrypted and do not merge tabs. Keep source files and portable backups. Loading 0.8 requires reload, not site-data deletion.

## Limits

Metadata is capped at 64 MiB, each original at 64 MiB, unique referenced source data at 256 MiB and catalog records at 5,000 including copies. These are safety ceilings, not recommended loads. Startup restores all referenced sources; initial storage/restore still hashes/transfers originals. Browser staging uses base64 interop before Blobs. Warm saves avoid that source-byte work, not all metadata serialization or I/O.

Orphan sources are deliberately retained. Compaction, paging, encryption, append-only journaling, cross-tab merge and cloud sync are absent. Multiple catalogs can leave storage beyond the active catalog's referenced size. Virtual copies reduce duplicated source payloads but can still accumulate substantial processing/version/history metadata.

## Evidence

Controlled engine tests cover delayed writes, preview/cancel, shared flushes, failure/retry, catalog replacement, synchronous delegates and disposal. Incremental tests cover source deduplication, integrity, frozen manifests and atomic publication. Virtual-copy tests add direct-root/source consistency, compact portable hydration and shared decoded ownership.

Browser checks inspect actual IndexedDB during an in-progress gesture, verify final values after reload, cancel with Escape, inject publication failures and protect unreadable data. Copy tests verify independent looks, root-preserving portable imports, original-byte retention and rename counters. The fatal-slider regression remains enabled.

`recovery-performance.json` measures twenty warm commits with an 8 MiB source through an in-memory store. `virtual-copy-performance.json` records source-sharing and metadata growth; browser artifacts separately record actual rename/commit counters. These measurements do not certify disk latency, total memory, physical GPUs or all browser engines.
