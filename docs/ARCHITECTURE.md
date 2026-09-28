# Architecture

## Dependency graph

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

Core, Catalog, Editing and Storage have no Uno dependency. Imaging and Rendering.Skia require SkiaSharp and matching native assets. Controls and Workbench target Uno browser and desktop. The eight libraries are independently packable; package artifacts do not imply NuGet.org publication.

## Models and transactions

`CatalogDocument` owns photo identities, original encoded bytes, album references and the active photo. `PhotoDocument.State` is replaced with a normalized record snapshot. Arrays are copy-on-write by contract: do not mutate retained originals, point arrays, mask collections or brush dabs in place.

`EditorSession.Preview` opens or updates a gesture. `CommitGesture` records one before/after transaction; cancellation restores its opening state. Batch settings changes form one transaction. Undo is bounded to 100 transactions; redo is cleared after a new committed edit. Imports and album structural operations are not yet undoable.

`Changed` reports committed catalog changes; `ViewChanged` also reports previews and selection. Document revisions can advance for preview rendering, while the session revision advances on committed changes. `PhotoStateEquality.All`, `Shader` and `Pixels` distinguish catalog, shader and crop-sensitive changes without serializing JSON. Metadata and sample-pin position changes do not invalidate identical processed pixels.

Catalog schema 4 records sampled color ranges alongside the previous grading, curves and brush data. Schemas 1–3 migrate with neutral defaults. Future schemas are rejected. Portable catalogs contain originals; recovery manifests deliberately do not.

## Processing order

Image import validates encoded length and dimensions before decoding. Skia normalizes all eight EXIF orientations and converts to sRGB. Retained viewport previews target a 2560-pixel long edge; thumbnails use a separate 384-pixel decode target. Some codecs still require a full-size decode before resizing.

The shader samples the unmodified source for luminance/color range selection, applies feathered clone stamps and detail taps, then performs exposure, relative white balance and tonal operations with explicit sRGB transfer conversion. The legacy five-point curve precedes arbitrary master/channel curves. Hue-weighted mixing, vibrance/saturation, monochrome and four-way grading follow. Local mask adjustments, vignette and grain finish the 8-bit output path.

The algorithm is original LightSpace processing. It is not scene-referred RAW development or Adobe pixel equivalence. Detail taps, LDR clamps and preview downsampling impose quality boundaries. Source-relative spatial coordinates improve consistency but cannot recover detail missing from a preview.

`ToneLookupCache` stores master/R/G/B curve composition in a 2048-entry floating-point image. It reuses tables on unrelated exposure/metadata changes. `BrushCoverageCache` stores an affine operation on analytic coverage, `coverage = multiplier * analyticCoverage + bias`. Append-only dabs update bounded affected raster regions; local tonal adjustments reuse coverage. Undo or changed earlier strokes trigger replay. Texture publication still creates an immutable coefficient image; it is not sparse GPU-texture upload. Brush coverage is capped at 1024 pixels even for export.

Color ranges form a union of up to five Oklab neighborhoods. A bounded source-preview patch supplies each sample. Sample coordinates are transformed into Oklab when uniforms are built; source color is converted once per pixel only when an active color range exists. Spatial/brush coverage intersects luminance and color restrictions before inversion and amount. The optional red coverage overlay is excluded from export and histogram sampling.

Crop, quarter turns and flips compose into an invertible source-to-view matrix. Pointer input uses its inverse. Source-pixel zoom describes geometry, not a guarantee of native-resolution inspection for previews larger than the decode target.

## Rendering and scheduling

The viewport is `Uno.WinUI.Graphics2DSK.SKCanvasElement`, with runtime effects integrated into Uno's existing Skia composition path. Hardware execution depends on the host supplying a GPU-backed canvas. There is no separate WebGPU compute device or texture-sharing backend in this release.

Decoded images, curve lookups and brush coverage have separate bounded caches. Their budgets cover retained buffers, not codec scratch space, original bytes, object graphs or GPU copies. Thumbnails retain at most 128 rendered images. Stable `PhotoCard` instances survive metadata updates when the visible page membership stays unchanged. A changed filter/page can rebuild its bounded 60-photo page.

Inspectors keep pointer-captured controls alive during gestures. Structural changes rebuild the relevant panel; expansion state survives reconstruction within the running workbench. Histograms sample a reduced-resolution raster and are scheduled only for pixel changes. Auto tone reads 96×64 pixels rather than copying the entire preview.

Decode, shader configuration, brush texture publication, metadata serialization and image export still use synchronous CPU/native work. Storage commits are asynchronous. A fully tiled rendering graph, asynchronous decode/export scheduling and an indexed durable catalog remain separate workstreams. See [measured performance scopes](PERFORMANCE.md).

## Recovery and interoperability

`IWorkspaceStorage` supplies user-authorized import/export and the legacy recovery contract. `ISidecarStorage` adds an optional XMP picker. `IRecoveryStore` supplies source-by-key retrieval and atomic publication of a source-separated `RecoveryWrite`.

`RecoveryPersistence` builds frozen committed manifests, weakly memoizes original-array hashes and avoids resending known sources. Restore verifies lengths and SHA-256 hashes before exposing a catalog. The browser stores originals as Blobs, checks unchanged references with key-only requests and publishes sources plus manifest in one transaction. Native storage stages sources before atomically replacing its manifest. A failed publication does not advance the acknowledged session revision. Synchronous browser errors explicitly abort the transaction; Promise rejection alone would not roll back queued writes.

Legacy recovery is read only when no modern manifest exists. The next committed save publishes the modern representation. Missing/corrupt recovery is protected until the user explicitly confirms replacement. Unreferenced sources remain retained; there is no automatic garbage collection, encryption, cross-tab merge or crash journal. [Recovery contract](RECOVERY.md) · [Publication hardening](RECOVERY-PUBLICATION.md)

`XmpSidecar` accepts a bounded metadata/Camera Raw subset and reports unsupported fields before application. Namespace URIs identify fields. DTD/external entity resolution is disabled; size, nesting and subject counts are bounded. The versioned LightSpace extension round-trips complete native settings; other applications may ignore it. Unknown external properties are not preserved on re-export. See [XMP mappings](ADVANCED-EDITING.md#xmp-interchange).

## Embedding and ownership

```csharp
var session = new EditorSession(catalog);
var workspace = new StudioView(session, storage, recoveryLoaded);
window.Content = workspace;
// Dispose workspace when its containing host closes.
```

An application that restores through `RecoveryPersistence` should pass that same instance to `StudioView` to reuse its verified-source/hash state. Embedded hosts implementing only `IWorkspaceStorage` retain the source-inclusive snapshot writer.

`PhotoRenderer`, `ThumbnailCache`, `PhotoViewport` and `StudioView` own native resources or subscriptions and must be disposed. A standalone viewport does not dispose its externally supplied renderer. The workbench owns its private renderer and thumbnails. All rendering, transaction and persistence objects are confined to one logical owner, normally the UI synchronization context.

```csharp
var editor = new PointCurveEditor { Value = session.Active!.State.Develop.Channels };
editor.Previewed += channels => session.Preview(state => state with
{
    Develop = state.Develop with { Channels = channels }
});
editor.Committed += () => session.CommitGesture("RGB curves");
editor.Canceled += session.CancelGesture;
```

Hosts refresh editor values after external changes such as undo or photo selection. Grading, mixer, mask and color-range editors follow the same transaction-neutral contract. Brush settings affect subsequent strokes, not earlier stroke snapshots.

## Validation and delivery

The engine runner emits machine-readable model, pixel, interpolation, brush replay, XMP, recovery and performance results. Browser tests drive real Uno pointers, keyboard, file pickers and downloads using read-only arranged bounds. Store-boundary tests deliberately inject transaction failures and inspect actual IndexedDB data. They do not mutate editor state through a testing-only editing API.

Normal sessions do not periodically serialize UI diagnostics. Weak registrations avoid retaining discarded controls; brush coordinates are omitted from diagnostic payloads. CI Chromium uses SwiftShader; desktop jobs verify compilation on three operating systems. Neither proves physical-GPU or pen-hardware behavior.

Build runs attach tested browser bundles, reports, source snapshots and all eight packages. Pages accepts a successful trusted main build, verifies artifact commit identity, deploys and repeats public-site tests. Release packages native/browser archives and checksums; native signing, notarization, installers and NuGet.org publication are not configured.
