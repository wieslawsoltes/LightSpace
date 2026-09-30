# Virtual copies: independent looks, shared originals

LightSpace 0.8 adds catalog-level alternatives to a photograph without duplicating its encoded source. Unlike a named version, a virtual copy has its own selectable catalog identity and appears in the grid, filmstrip, search, albums and Survey. Unlike a pinned reference, it survives portable backup and recovery reload.

## Create and compare

Open **Manage virtual copies** on the right tool rail. **Create virtual copy** copies the active look; **Create for selection** creates a copy for each selected photograph in one transaction. An active adjustment gesture is committed first so the new copy captures the intended final look.

Copies receive unique names such as Copy 1 and Copy 2 within their original's family. A copy made from another copy captures that copy's edits but points directly to the original root, never through a chain of copies. The source and new copy are adjacent in catalog order; copies inherit their source's album membership and relative album order. The application switches to All photos after creation so a filter cannot hide the new records.

The family panel lists Original and its copies, with bounded paging for large families. Select a member to edit its appearance independently, rename a copy, or choose **Survey this original** to compare the family. Originals and Virtual copies filters distinguish the two kinds. Search matches the copy's display name as well as the existing searchable metadata. Filmstrip/grid labels distinguish the alternative looks.

All processing fields are independent after an edit: tone, color grading, curves, masks, crop, optics, geometry and clone spots. Ratings, flags, captions and keywords are also independent. The starting metadata is copied; later metadata changes do not propagate to siblings. Existing named versions are not duplicated into a new copy: its versions list starts empty. Reference comparison and selective settings continue to work with any family member.

## Rename and remove safely

Names are trimmed, limited to 100 characters, and validated before modification. Names must be unique within the family under case-insensitive comparison. Renaming changes catalog metadata, not processing, and is undoable. Reapplying an identical name does not create another history transaction.

**Remove this copy…** opens an explicit confirmation. Removal deletes only virtual-copy records and their album references. It never deletes an original catalog record or source file, and it never removes a sibling copy. Mixed original/copy removal requests are rejected before any mutation. Undo restores the same identity, settings, membership positions and relevant selection; redo removes it again.

Creation and removal use reversible record deltas rather than whole-catalog snapshots. Thus undoing creation does not erase an unrelated photo imported later, and album memberships added after the history entry are recorded when the copy is removed. Batch creation is one transaction. Missing targets, invalid roots and the 5,000-photo ceiling are checked before creating copies. A failed validation leaves the catalog unchanged.

When undo removes a visible Survey candidate, Survey closes before another interaction can target the removed record. The ordinary detail view selects an available family member. Unused per-photo renderer/thumbnail entries are released on structural removal; another family member can keep the shared source alive.

## Image export and portable catalogs

Image exports render the active copy's settings, not its original's look. Copy names and a short copy identity are added to exported filenames to distinguish sibling images. Batch ZIP exports still include their numeric prefix. Exported images are ordinary rendered JPEG/PNG/WebP files; they do not carry a virtual-copy relationship to another file. Existing 8-bit sRGB, size and source-metadata limitations still apply.

Portable catalog **schema 6** stores `MasterPhotoId` and `CopyName`. Each original record retains its encoded `Original` payload; virtual-copy records write an empty payload and reference the original. Serialization never clears source bytes in the live editing objects. Loading validates roots, dimensions, family names and any supplied duplicate payload before hydrating a single shared source array for the family.

A root must exist, cannot itself be a copy, and cannot equal the child's identity. Missing masters, cycles/chains, conflicting source bytes, mismatched dimensions, duplicate family names and copy identity fields in an older declared schema are rejected rather than silently repaired. Catalogs with schemas 1–5 migrate as original-only records. Older application builds reject schema 6; retain a pre-upgrade portable backup for old-version interoperability.

Portable sources are counted by original records rather than multiplying one family's bytes by its number of alternatives. Distinct original records may still contain equal data; portable serialization does not content-hash unrelated originals. The existing 256 MiB catalog source ceiling and 5,000-record ceiling remain in force.

## Recovery and native XMP versions

Recovery manifest format 1 and IndexedDB version 2 are unchanged. The manifest's catalog now uses schema 6; every copy must reference the same source key and length as its master. The browser, native publication validator and managed restore path check these relationships. Unique source blobs are counted once and stored by SHA-256. Once the root has been saved, adding or renaming a copy requires metadata publication but no additional source hash or source-blob write under the immutable-array contract.

Recovery restores the root and copies with a common byte array. Source length/hash validation, protected unreadable recovery, revision-aware acknowledgement and explicit retry remain enabled. No recovery reset or site-data deletion is needed to load this version. Orphan source cleanup, cross-tab merge, encryption and guaranteed close-time flush are still outside the implementation.

**Native XMP settings remain version 5.** Virtual-copy identity is a catalog relationship, not a field in `PhotoState`. An XMP sidecar preserves the selected copy's processing but does not reconstruct a family or master relationship in another catalog. Adobe virtual-copy catalog serialization and `.lrcat` interchange are not implemented.

## Reusable API and ownership

The Core model adds `PhotoDocument.MasterPhotoId`, `CopyName`, `IsVirtualCopy`, `DisplayName`, `CopyRecord()` and `VirtualCopyNames`. Catalog supplies `VirtualCopyCatalog` relationship/portable validation and `PhotoQuery.Kind` through `PhotoKind`. Editing supplies reversible create, rename and copy-only removal operations:

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

// The original retains its initial settings and both records share source bytes.
System.Diagnostics.Debug.Assert(photo.State.Develop.Exposure == 0);
System.Diagnostics.Debug.Assert(ReferenceEquals(photo.Original, copy.Original));

session.RemoveVirtualCopies([copy.Id]);
session.Undo(); // Restores the same copy identity and its edited appearance.
```

A copy initially shares normalized immutable state as well as original bytes; editing replaces state through the session. `CopyRecord()` gives it an independent version list. Mutating arrays or encoded bytes in place violates rendering, history and persistence ownership contracts. Use copy-on-write state changes and the session APIs. The session and native caches remain confined to one logical owner, normally the UI synchronization context.

## Performance and evidence

Per-photo shader identities remain distinct, while the existing source pool shares decoding for identical source-array identities. A family Survey reuses one decode in that Survey renderer. The detail renderer and Survey renderer retain separate budgets and may each decode a preview at their own resolution; this is not a process-wide unified texture cache. Every distinct visible look still performs its own shading work.

`VirtualCopyTests` and `VirtualCopyQueryTests` cover independent edits, copy-of-copy roots, membership order, reversible deltas, source deduplication, malformed imports, source limits, cache reuse, queries and metadata-only renames. Browser tests use actual controls, exported catalogs/JPEGs and recovery reload to verify distinct pixels, safe removal/undo, filter/Survey behavior and source preservation.

`artifacts/engine/virtual-copy-performance.json` reports scoped source-size/cache counters. `artifacts/browser-exports/virtual-copy-performance.json` records a real warmed copy rename with renderer and persistence counters. These are work-avoidance measurements, not universal GPU timings or proof that all processing occurs on a GPU. The full fatal-slider stress suite remains enabled.

## Explicit boundary

This release does not implement Set Copy As Master, automatic stack management, disk-backed source relinking, Lightroom catalog interchange or automatic semantic adaptation of local masks. Source-file deletion is not exposed through the virtual-copy removal operation. The 5,000-photo catalog ceiling includes copies, and many processing/version snapshots can still consume significant metadata/history memory even when original bytes are shared.

The external workflow reference is Adobe's [Create virtual copies](https://helpx.adobe.com/in/lightroom-classic/desktop/manage-catalogs-and-files/photos.html#create_virtual_copies). LightSpace's family naming, inherited albums, removal safety and portable representation are independently implemented and do not constitute full Adobe compatibility.
