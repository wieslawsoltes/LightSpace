# Virtual copies: independent looks, shared originals

LightSpace 0.8 adds catalog-level alternatives to a photograph without duplicating its encoded source. Unlike a named version, a virtual copy has its own selectable catalog identity and appears in the grid, filmstrip, search, albums and Survey. Unlike a pinned reference, it survives portable backup and recovery reload.

## Create and compare

Open **Manage virtual copies** on the right tool rail. **Create virtual copy** copies the active look; **Create for selection** creates a copy for each selected photograph in one transaction. An active adjustment gesture is committed first so the new copy captures its final look.

Copies receive unique names such as Copy 1 and Copy 2 within their original's family. A copy made from another copy captures that copy's edits but points directly to the original root, never through a chain. Source and copy are adjacent in catalog order; copies inherit source album membership and relative order. After creation the application switches to All photos so a filter cannot hide the new records.

The family panel lists Original and its copies, with bounded paging. Select a member to edit independently, rename a copy, or choose **Survey this original**. Originals and Virtual copies filters distinguish source records and alternatives. Search includes copy names alongside existing metadata. Filmstrip/grid labels identify the looks.

Tone, grading, curves, masks, crop, optics, geometry and clones are independent after an edit. Ratings, flags, captions and keywords are independent too: starting metadata is copied but later changes do not propagate. A copy starts with an empty named-versions list; existing source versions are not duplicated. Reference comparison and selective settings work with any family member.

## Rename and remove safely

Names are trimmed, limited to 100 characters and validated before modification. Names must be unique within the family under case-insensitive comparison. Rename affects metadata, not processing, and is undoable. Reapplying an identical name does not create history.

**Remove this copy…** requires confirmation and deletes only virtual-copy records and their album references. It never deletes an original catalog record, source file or sibling. Mixed original/copy removal requests fail before mutation. Undo restores the same identity, settings, membership positions and selection; redo removes it again.

When the active copy is removed from a multi-selection, the first selected survivor in catalog order becomes active. If no selected survivor remains, its original becomes active and selected. Removing an inactive copy preserves the current active photograph. This keeps toolbar edit targets aligned with the displayed photograph. Undo/redo restore the appropriate selection. [Handoff regression and safety](VIRTUAL-COPY-SELECTION.md)

Creation/removal use reversible record deltas rather than whole-catalog snapshots. Undoing creation does not erase an unrelated photo imported later. Album memberships added after a history entry are recorded when the copy is removed. Batch creation is one transaction. Missing targets, invalid roots and the 5,000-record ceiling are checked before mutation.

Undoing a visible Survey candidate closes Survey before another interaction can target that removed record. Unused per-photo renderer/thumbnail entries are released after structural removal; another family member can keep its shared source alive.

## Image export and portable catalogs

Image export renders the copy's settings, not the original's look. Copy names and a short identity distinguish exported filenames. ZIP exports retain their numeric prefixes. Rendered JPEG/PNG/WebP files do not carry a virtual-copy relationship to another file. Existing 8-bit sRGB, output-size and source-metadata boundaries apply.

Portable catalog **schema 6** records `MasterPhotoId` and `CopyName`. Original records retain their encoded `Original` payload; copy records write an empty payload and reference their root. Serialization never clears live editing source buffers. Loading validates relationships, dimensions, names and any repeated payload before hydrating one shared source array per family.

A root must exist, cannot itself be a copy, and cannot equal the child identity. Missing roots, cycles/chains, conflicting source bytes, mismatched dimensions, duplicate family names and copy fields in an older declared schema are rejected. Schemas 1–5 migrate as original-only records. Older builds reject schema 6; retain a pre-upgrade portable backup for interoperability.

Portable source size is counted by original records rather than multiplying a family's bytes by its alternatives. Equal bytes in distinct original records are not automatically content-hashed by portable serialization. Existing 256 MiB source and 5,000-photo limits remain; copies count as photo records.

## Recovery and XMP versions

Recovery manifest format **1** and IndexedDB version **2** are unchanged. The manifest catalog uses schema 6; copies must reference their master's source key and length. Browser, native publication and managed restore validate this relationship. Unique source blobs are counted once and stored by SHA-256. After the root is saved, adding or renaming a copy requires metadata publication, not another source hash/write under the immutable-array contract.

Restore hydrates a family with one byte array. Source integrity checks, protected unreadable recovery, revision-aware acknowledgement and retry remain enabled. No recovery reset or site-data deletion is required. Orphan cleanup, cross-tab merge, encryption and guaranteed close-time flush remain absent.

**Native XMP settings stay at version 5.** Copy identity is a catalog relationship, not a `PhotoState` processing field. A sidecar preserves a copy's look but does not reconstruct its family or master relationship. Adobe virtual-copy catalog serialization and `.lrcat` interchange are not implemented.

## Reusable API and ownership

Core supplies `PhotoDocument.MasterPhotoId`, `CopyName`, `IsVirtualCopy`, `DisplayName`, `CopyRecord()` and `VirtualCopyNames`. Catalog supplies `VirtualCopyCatalog` validation and `PhotoQuery.Kind` using **`PhotoKindFilter`** (`All`, `Originals`, `VirtualCopies`). Editing supplies reversible creation, rename and copy-only removal:

```csharp
using LightSpace.Core;
using LightSpace.Editing;
using LightSpace.Imaging;

var photo = PhotoCodec.Import("mountains.jpg", File.ReadAllBytes("mountains.jpg"));
var session = new EditorSession(new CatalogDocument
{
    Photos = [photo], ActivePhoto = photo.Id
});
var copy = session.CreateVirtualCopies([photo.Id]).Single();
session.RenameVirtualCopy(copy.Id, "Warm evening");
session.Edit("Warm the copy", state => state with
{
    Develop = state.Develop with { Temperature = 20, Exposure = .35f }
});
System.Diagnostics.Debug.Assert(photo.State.Develop.Exposure == 0);
System.Diagnostics.Debug.Assert(ReferenceEquals(photo.Original, copy.Original));
session.RemoveVirtualCopies([copy.Id]);
session.Undo(); // Restores the same copy and its edited appearance.
```

A copy initially shares normalized immutable state and original bytes; editing replaces state through the session. Its versions list is independent. In-place mutation of arrays/bytes violates rendering, history and persistence contracts. Use copy-on-write edits and session operations. Objects remain confined to one logical owner, normally the UI synchronization context.

## Performance and evidence

Processing identities stay distinct while the source pool shares a decoded image for identical source-array identities. A family Survey can reuse one decode within its Survey renderer. Detail and Survey have separate budgets and can each decode at their own preview resolution; this is not a process-wide texture pool. Distinct visible looks still require shading work.

Engine tests cover independent edits, direct roots, album order, reversible deltas, deduplication, malformed imports, limits, queries, selection handoffs and cache reuse. Browser tests use real controls, exported catalogs/JPEGs and recovery reload to verify distinct pixels, safe removal/undo, filter/Survey integration and original preservation.

`artifacts/engine/virtual-copy-performance.json` records scoped source/cache and portable-size measurements. `artifacts/browser-exports/virtual-copy-performance.json` records a warmed real copy rename with renderer/persistence counters. These show avoided work, not universal GPU timing or GPU-only processing. The full fatal-slider stress suite remains enabled.

## Boundary

Set Copy As Master, automatic stacking, disk-backed relinking, Lightroom catalog interchange and semantic adaptation of masks are not implemented. This removal API does not expose source-file deletion. Many copies can consume considerable processing, version and history metadata even while source bytes are shared.

Adobe's [Create virtual copies](https://helpx.adobe.com/in/lightroom-classic/desktop/manage-catalogs-and-files/photos.html#create_virtual_copies) describes the external workflow reference. LightSpace's family naming, inherited albums, removal safeguards and portable representation are independent choices, not full Adobe compatibility certification.
