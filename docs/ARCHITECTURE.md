# Architecture

## Libraries and ownership

```text
LightSpace.App (browser / native hosts)
  └─ LightSpace.Workbench
       ├─ LightSpace.Storage
       └─ LightSpace.Controls
            ├─ LightSpace.Editing
            │    ├─ LightSpace.Catalog → LightSpace.Core
            │    └─ LightSpace.Storage
            └─ LightSpace.Rendering.Skia
                 └─ LightSpace.Imaging → LightSpace.Core
```

Core, Catalog, Editing and Storage have no Uno dependency. Imaging and Rendering.Skia use SkiaSharp and matched native assets. Controls and Workbench target Uno browser/desktop. All eight libraries are packable, with metadata, symbols and SourceLink. Tagged releases use the existing NuGet Trusted Publishing configuration.

`CatalogDocument` owns identities, encoded original bytes, photo states, album references and active-photo identity. State records and their arrays follow an immutable/copy-on-write contract. Rendering, transactions and persistence are confined to a single logical owner, normally the UI synchronization context. Mutation of retained arrays or source bytes in place violates cache and undo contracts.

## Transactions and schema

`EditorSession.Preview` opens or updates a gesture; commit records one before/after transaction and cancellation restores its opening state. Batch settings changes are one transaction. Undo is bounded to 100 entries and redo is cleared after a new edit. Imports and album structural operations are not yet undoable.

`Changed` reports committed catalog edits; `ViewChanged` also reports previews/selection. The photo revision can advance during previews while the session revision advances only on committed changes. Recovery captures a copy with the opening state of an active gesture, never its visible uncommitted preview.

Schema **5** stores separate `Develop`, `Crop`, `Geometry`, `Optics`, masks and clone settings. Catalogs 1–4 migrate with neutral missing fields. Native XMP settings version 5 accepts versions 3–4. Unsupported future schemas fail explicitly. Portable catalogs contain originals; recovery manifests contain their references. The recovery manifest stays format 1 and IndexedDB stays version 2.

## Processing stages

Decode validates encoded size and image dimensions, normalizes all EXIF orientations and produces sRGB data. Retained viewport previews target a 2560-pixel long edge; thumbnail sources target 384 pixels. Codec-specific full-resolution scratch allocations can precede resizing.

The development runtime effect samples the source for luminance/color-range selection before applying clone stamps, detail taps, exposure, relative white balance and tonal operations. The legacy five-point curve precedes arbitrary master/channel curves. Hue mixing, saturation/vibrance, monochrome and four-way grading follow. Local corrections, creative vignette and grain finish that stage.

`ToneLookupCache` compiles master/channel composition into a 2048-entry floating-point lookup image. `BrushCoverageCache` represents paint/erase as an affine operation on analytic mask coverage: `coverage = multiplier * analyticCoverage + bias`. Appended dabs update bounded raster regions; local tonal changes reuse coverage. Undo or changed earlier strokes cause replay. Coefficient texture publication remains an immutable-image update rather than sparse GPU upload, with a 1024-pixel long-edge cap even for export.

Color ranges are unions of up to five Oklab neighborhoods. Samples come from a bounded original-preview patch. Spatial/brush coverage intersects luminance and color restrictions before inversion and amount. The optional selected-mask overlay is omitted from export and histogram analysis.

The **presentation effect** evaluates the development shader directly. It remaps coordinates for manual distortion, separately samples displaced red/blue channels when requested, applies linear-light lens falloff, and optionally shows LDR clipping. No intermediate CPU image is materialized between the two display stages. Channel alignment can evaluate the development child three times; composition is not a universal performance guarantee.

The **geometry stage** is a cached homography supplied to the draw. It covers perspective, source-aspect-corrected rotation, scale/aspect/offsets and conservative constrained framing. Crop, quarter turns and flips are applied afterward. Source/display mapping uses the corresponding analytic optical inverse and cached projective inverse, keeping masks and source pickers attached to original coordinates.

The model is original LDR processing, not scene-referred RAW, calibrated lens correction or Adobe-rendered pixel equivalence. Clamps, preview downsampling, finite curve tables and bounded brush textures are explicit quality limits. See [optical/projective math](OPTICS-GEOMETRY.md) and [advanced editing](ADVANCED-EDITING.md).

## GPU integration and resource lifetime

`Uno.WinUI.Graphics2DSK.SKCanvasElement` supplies the host-owned canvas. SkSL effects become stages of the Skia drawing pipeline. On a GPU-backed host canvas, child effects can compose into the GPU shader; software rendering remains supported. There is no independent WebGPU device or browser/native texture-sharing engine. [Skia runtime-effect composition](https://skia.org/docs/user/sksl/) · [Uno canvas integration](https://platform.uno/docs/articles/controls/SKCanvasElement.html)

`RuntimeShaderScope` preserves the 0.4.1 crash repair: staging inputs and compiled output have separate ownership, the output is field-rooted through cleanup, and transfer occurs only after staging release. Owned children are disposed; borrowed children are retained through compilation without taking ownership. Failure releases untransferred resources. Deferred-picture and forced-finalization tests cover composed effects as well as ordinary development.

`PhotoRenderer`, `ThumbnailCache`, `PhotoViewport` and `StudioView` must be disposed. A standalone viewport does not own its externally supplied renderer. The workbench owns its private renderer and thumbnails. Native reference retention does not remove the requirement to dispose managed wrappers correctly.

## Invalidation and scheduling

`PhotoStateEquality.All`, `Shader` and `Pixels` separate full-catalog equality, development inputs and complete pixel effects. Geometry and optics do not invalidate the development shader. Geometry updates rebuild the matrix while retaining source images, presentation/development shaders, brush coverage and curve tables. Metadata updates preserve all pixel stages. Source-array identity detects a changed original under a reused photo ID.

Viewport, original comparison and histogram analysis have separate presentation caches. Histogram refresh does not change viewport clipping or repeatedly rebuild its optical stage. Weak projection caches avoid solving constrained framing for every outline point.

Photo cards survive metadata changes while page membership remains unchanged. A filter/page change can legitimately rebuild the bounded 60-photo page. Pointer-captured editors remain alive through gestures; section expansion survives inspector reconstruction. Histogram calculation is throttled on pixel changes, using a 192×128 raster; Auto uses a 96×64 source sample.

Decode, uniform configuration, brush texture publication, source hashing, metadata serialization and encoded image export retain CPU/native work. Fully tiled processing, asynchronous source paging/export and indexed durable catalogs remain separate workstreams. Cache budgets cover retained buffers, not total process or GPU peak memory. [Performance evidence](PERFORMANCE.md)

## Storage and interchange

`IWorkspaceStorage` supplies user-authorized import/export and legacy string recovery. Optional `ISidecarStorage` adds an XMP picker. `IRecoveryStore` supplies source-by-key reads and atomic publication of a source-separated `RecoveryWrite`.

`RecoveryPersistence` freezes committed manifests, weakly memoizes source hashes and avoids resending known originals. Restore verifies lengths/hashes before exposing a catalog. Browser commits stage Blobs and publish a manifest in one transaction with key-only checks for unchanged originals; native storage stages sources before manifest replacement. Failed writes never advance acknowledged revisions. Synchronous browser errors explicitly abort queued transactions.

Legacy recovery is read only when no modern manifest exists. Missing/corrupt data is protected until explicit replacement. Orphan sources remain retained; encryption, cross-tab conflict resolution, automatic cleanup and a crash journal are not implemented. [Recovery](RECOVERY.md) · [Publication hardening](RECOVERY-PUBLICATION.md)

`XmpSidecar` reports its supported metadata/Camera Raw subset before applying changes. Namespace-aware parsing disables DTD/external resolution and bounds size/nesting/resource count. Native settings preserve complete corrections; standard Camera Raw geometry/profile equivalence is not claimed. Unknown third-party properties are not retained on re-export.

## Embedding controls

```csharp
var session = new EditorSession(catalog);
var workspace = new StudioView(session, storage, recoveryLoaded);
window.Content = workspace;
// Dispose workspace at host shutdown.
```

Pass an existing `RecoveryPersistence` instance after restore to reuse its verified-source/hash state. A host with only `IWorkspaceStorage` retains source-inclusive recovery.

```csharp
var geometry = new GeometryEditor { Value = session.Active!.State.Geometry };
geometry.Previewed += value => session.Preview(state => state with { Geometry = value });
geometry.Committed += () => session.CommitGesture("Geometry");
geometry.Canceled += session.CancelGesture;
```

Optics, curve, grading, mixer, mask and color-range controls follow the same transaction-neutral contract. Histogram events supply the selected tone and a delta from gesture start. Grip events supply stable-parent-coordinate width deltas. Hosts refresh values after undo, loading and photo selection. Brush settings affect subsequent strokes rather than rewriting old stroke snapshots.

## Validation and delivery

The engine suite covers models, pixels, interpolation, brush replay, XMP, geometry/optics, source mapping, recovery, shader lifetimes and scoped work counters. Browser tests use real pointers, keyboard, file pickers and downloaded outputs; store-boundary tests inject failures into real IndexedDB transactions. Normal sessions do not periodically serialize diagnostics, and brush coordinates remain omitted from opt-in payloads.

CI uses Chromium/SwiftShader. Native jobs certify compilation, not every driver or physical pen. Build retains reports, packages and source snapshots. Pages verifies a trusted main artifact's commit, deploys and tests the public URL. Release preserves six-RID single-file native packaging and tag-based NuGet Trusted Publishing; signing/notarization/installers remain separate configuration.
