# Committed, content-addressed recovery

## Two independent versions

Application/catalog schema **4** records processing features. The recovery **manifest format 1** stores source references, and the browser **IndexedDB version 2** adds an originals store. They are separate contracts; a catalog migration is not a database version change.

The application restores the modern manifest first. When no modern manifest exists, it can read the previous source-inclusive JSON recovery. Reading legacy recovery does not delete or rewrite it. The next committed save publishes the modern representation; portable `.lightspace` exports continue to embed all originals.

## Data model and ownership

`IRecoveryStore` defines three asynchronous operations: read a manifest, read a source by key, and commit a `RecoveryWrite`. The write contains a manifest, the complete referenced-key set, and newly staged `RecoveryBlob` values. Keys are lowercase SHA-256 hashes of encoded original bytes.

`RecoveryPersistence` maps immutable original-array identity to a hash with weak references. It builds a source-free `RecoveryManifest` from `EditorSession.CopyCommittedCatalog()`. Active gesture previews are replaced by their opening state in the copy, without changing what the user sees. The JSON payload is frozen before storage begins. Original arrays are shared read-only; mutating them in place violates the cache contract.

Each photo reference includes its key and encoded length. Identical originals share a persisted blob and, after restore, a single byte array. Manifest validation bounds metadata size, photo count, dimensions, identities, source lengths and aggregate referenced bytes before source reads. Restore validates the actual source length and SHA-256 hash. The source hash cache is then ready for later metadata edits.

The coordinator, persistence adapter and session are confined to one logical owner, normally the UI synchronization context. They are not a thread-safe database or a cross-tab conflict-resolution system.

## Host API

```csharp
// A platform store supplies atomic publication and source retrieval.
var persistence = new RecoveryPersistence(store);
var restored = await persistence.RestoreAsync();
var session = new EditorSession(restored ?? initialCatalog);
using var recovery = RecoveryCoordinator.Incremental(session, persistence,
    initiallySaved: restored is not null);
recovery.StatusChanged += status => UpdateSaveIndicator(status);

// Called by the host's debounce timer or explicit save/retry command.
await recovery.FlushAsync();
```

`StudioView` accepts an optional shared `RecoveryPersistence` so a host can reuse the hash/known-source state populated during restore. When no instance is supplied, an `IRecoveryStore` storage implementation selects the incremental path automatically. An embedded host implementing only `IWorkspaceStorage` retains the existing string-based snapshot writer:

```csharp
using var recovery = new RecoveryCoordinator(session, storage.WriteRecoveryAsync,
    initiallySaved: restoredFromRecovery);
```

A store's completion must mean that its publication operation succeeded. Starting an asynchronous write is not a durability acknowledgement.

## Publication and failure behavior

**Browser.** Originals are stored as Blobs in `originals`; the manifest lives under `workspace/manifest-v1`. Newly supplied sources are decoded and hash-checked before starting the transaction. Awaiting WebCrypto inside an otherwise idle transaction would permit it to become inactive, so those operations stay outside it. A single read-write transaction stages new blobs, uses key-only checks for every required source, publishes the manifest, and removes the obsolete legacy record. Any failed request or missing key aborts the transaction. Success is reported only from transaction completion. Unchanged source values are not fetched during this check. See the [IndexedDB transaction lifecycle](https://developer.mozilla.org/en-US/docs/Web/API/IDBTransaction).

**Desktop.** `FileRecoveryStore` writes validated source bytes to temporary files and replaces their content-addressed destinations. It verifies that every required source exists before replacing `recovery-manifest-v1.json`. A failed manifest publication leaves the previous manifest in place; staged but unreferenced originals can remain. This is atomic file publication, not a multi-process database transaction or a power-loss-proof journal.

A successful write acknowledges its captured revision only. Newer committed revisions are drained afterward; overlapping flush calls share one writer. A live preview remains dirty even when the preceding committed revision is saved. Failure retains the last successful acknowledgement, clears the known-source set, and allows an explicit retry to restage originals after missing-key/eviction failures.

Integrity checks on restore detect changed or truncated source contents. Warm commits perform existence checks, not a full rehash of all already-stored blobs. This tradeoff avoids repeatedly reading large originals; it is not continuous corruption monitoring.

## User-visible protection

The footer reports Saved, Pending, Editing, Saving or Failed independently of import/export messages. Click it to flush or retry. Browser unload protection follows immediate unsaved state rather than periodic diagnostic snapshots.

An unreadable manifest, missing source, invalid hash or unsupported legacy recovery does not trigger replacement with sample photographs. The prior recovery remains protected until the user explicitly confirms **Replace recovery**. That confirmation saves the current workspace, not a repair of the damaged previous catalog. Preserve the browser profile/native recovery directory when manual recovery may be needed.

Browser termination and OS crashes may bypass unload prompts. Native close-time flushing is not guaranteed. Retain original source files and portable catalog backups.

## Limits and migration boundaries

Metadata is capped at 64 MiB, each original at 64 MiB, and referenced encoded source data at 256 MiB per catalog. These are safety ceilings, not recommended workloads. First persistence and restore still process original bytes; native hashing and metadata serialization are synchronous, and new browser sources pass through base64 interop before being stored as Blobs. Warm metadata edits avoid that original-byte path.

Unreferenced source blobs/files are deliberately retained. No automatic garbage collection, encrypted vault, append-only edit journal, multi-tab merge or cloud synchronization is implemented. Several catalogs used in one profile can therefore leave retained sources beyond the active catalog's size. There is no UI storage-compaction command yet.

The IndexedDB upgrade is one-way for an older application that explicitly requests database version 1. Older builds also reject catalog schema 4. Keep pre-upgrade portable backups and a separate browser profile for old-version interoperability. Modern native manifests and the legacy native recovery file are distinct; older native builds do not see newer manifest edits.

## Evidence

The engine suite emits `artifacts/engine/recovery-performance.json` for 20 warm metadata commits after storing an 8 MiB source. It verifies no additional source writes or bytes hashed and records manifest size, CPU time and managed allocation. The in-memory adapter used for this measurement is not a disk-latency benchmark.

Browser tests record actual IndexedDB/C# work counters for repeated rating edits, verify source-free manifests, restart the app, remove a source to test transaction abort/retry, protect a missing-source recovery until explicit replacement, and migrate a legacy portable record without changing original bytes. Existing delayed-write, preview/cancel and corrupt-recovery regressions remain enabled.
