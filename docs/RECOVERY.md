# Revision-aware recovery

## Contract

`EditorSession.Revision` advances on committed catalog edits. `PhotoDocument.Revision` may advance on live previews for rendering; it is not a durability acknowledgement.

`EditorSession.CaptureCommittedSnapshot()` returns a `CommittedCatalogSnapshot` containing a revision and serialized JSON. For a photo with an active gesture, serialization uses the opening state of that gesture while leaving the visible preview unchanged. The payload is frozen before the first asynchronous write.

`RecoveryCoordinator` is part of `LightSpace.Editing` and has no UI or platform storage dependency. Construct it with an editing session and a `Func<string, Task>` that completes only after the storage operation commits. The browser adapter already waits for IndexedDB transaction completion. The host controls debounce scheduling; explicit flushes use the same coordinator.

```csharp
using var recovery = new RecoveryCoordinator(session, storage.WriteRecoveryAsync,
    initiallySaved: restoredFromRecovery);
recovery.StatusChanged += status => UpdateSaveIndicator(status);

// A host's debounce callback or explicit Save command:
await recovery.FlushAsync();
```

All calls and session mutation must be serialized on the same logical owner, normally the UI synchronization context. This class is not a thread-safe catalog database or a cross-tab write coordinator.

## Invariants

A storage write never captures an uncommitted preview. Only a successfully completed write advances `SavedRevision`. A write of revision N cannot acknowledge N+1. A gesture keeps the workspace dirty even if the preceding committed revision is saved. Concurrent flush callers join one drain; newer commits are captured in subsequent writes, not by modifying existing payloads. Failed writes preserve the previous acknowledgement and may be retried. Disposal suppresses further callbacks and new writes; an already-started atomic write may finish.

`RecoveryStatus` exposes current/saved revisions, active gesture, saving/error state, and `HasUnsavedChanges`. Its state is Saved, Pending, Preview, Saving or Failed. Tests and UI do not infer durability from a stale operation message.

## User-visible behavior

The footer save control is independent of export/import status messages. Click it to flush or retry. Browser unload protection follows the immediate unsaved-change event, not the periodic test diagnostics. Browser termination, operating-system crashes and some mobile lifecycle events can still bypass unload prompts: portable catalog backups remain important.

An unreadable existing recovery record is never automatically replaced with demonstration photographs. The workspace protects it from scheduled and explicit writes until the user confirms “Replace recovery”. The existing record is not repaired or silently deleted.

## Regression coverage

Controlled storage completions verify ordering without timing-dependent sleeps. Browser tests use real pointer capture, hold an exposure gesture across the prior rating edit's autosave deadline, read the actual IndexedDB payload, and then verify the final committed value after reload. Another test cancels that gesture with Escape. A third injects an unsupported recovery schema and verifies that it remains intact until explicit replacement.

This release still uses a single JSON recovery record, not an append-only journal. It does not merge writes from multiple tabs, encrypt local data, or guarantee a synchronous close-time flush. Only completed committed revisions are acknowledged as saved.
