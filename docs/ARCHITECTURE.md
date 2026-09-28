# Architecture

## Package boundaries

```text
LightSpace.App — native/browser startup and platform storage
  └─ LightSpace.Workbench — shell, catalog views, inspectors, commands, recovery UX
       ├─ LightSpace.Storage — storage and optional XMP-picker contracts
       └─ LightSpace.Controls — original reusable Uno controls
            ├─ LightSpace.Editing
            │    └─ LightSpace.Catalog
            │         └─ LightSpace.Core
            └─ LightSpace.Rendering.Skia
                 └─ LightSpace.Imaging
                      └─ LightSpace.Core
```

Core, Catalog, Editing and Storage are UI-independent. Imaging and Rendering.Skia use SkiaSharp with matching native assets. Controls and Workbench target Uno desktop and browser. No new proprietary or copyleft image-processing dependency was introduced for curves, brush masks or XMP; their implementations are original project code.

## Model and transactions

`CatalogDocument` owns original source bytes, photo identities, albums and the active-photo ID. A `PhotoDocument` has a replaceable `PhotoState` containing development, crop, masks, clone spots and metadata. Grading, arbitrary-point RGB curves and brush strokes are part of that state.

Records and array-backed collections follow a copy-on-write contract: never mutate arrays or source bytes in place after publication. Normalization retains valid references and repairs invalid data into new collections. `PhotoStateEquality.All` compares the complete state without JSON allocation. `Shader` excludes metadata/crop and `Pixels` adds crop, giving each cache the appropriate invalidation policy.

`EditorSession.Preview` opens a gesture and replaces visible state. `CommitGesture` records one before/after transaction; cancellation restores the opening state. Batch edit transactions cover all selected photographs atomically. Undo/redo is bounded to 100 transactions in the running session. Named versions are serialized snapshots. Imports and album structural operations are currently outside undo history.

`Changed` denotes committed catalog changes. `ViewChanged` also covers live previews and selection. Controls use preview events for responsive editing; durability follows the separate committed-revision contract. See [recovery architecture](RECOVERY.md).

## Processing sequence

1. Bounded input inspection verifies encoded size and decoded dimensions. `SKCodec` decodes the image, normalizes EXIF orientation and converts to sRGB.
2. The viewport keeps a bounded decoded preview; the thumbnail renderer uses a smaller decode target. Original bytes remain retained for later export.
3. Clone stamps sample the source; lightweight neighboring taps implement spatial effects. Source-relative coordinates preserve geometric intent, but preview downsampling still changes available detail.
4. Exposure, relative white balance and tonal adjustments operate after the sRGB transfer conversion into linear values. The current pipeline returns to bounded display-encoded RGB for curves and mixing; it is not a scene-referred RAW/HDR pipeline.
5. The legacy five-point curve is followed by the arbitrary-point master curve and independent R/G/B curves. A separately cached 2048-entry floating-point table stores the master/channel composition.
6. Hue-weighted color mixing, saturation/vibrance, monochrome and four-way grading are applied. All algorithms are original approximations, not Adobe pixel-equivalence implementations.
7. Each mask starts with analytic radial/linear coverage, unrestricted range coverage, or zero brush coverage. Cached paint/erase coefficients modify spatial coverage as `M * analytic + B`. Luminance restrictions, inversion and amount follow. The mask applies local tone/color adjustments.
8. Vignette and grain finish the image. Optional selected-mask coverage is a preview-only overlay.
9. Source-normalized crop, quarter turns and flips produce the view/export matrix. Its inverse maps pointer input to source geometry.
10. Export decodes original bytes, evaluates processing on an offscreen raster surface and encodes an 8-bit sRGB copy. Preview guides, mask overlay and comparison do not enter exported pixels.

[Detailed curve/brush/XMP semantics](ADVANCED-EDITING.md) and [grading/range behavior](COLOR-AND-MASKS.md) define units, limits and approximation boundaries.

## Why the existing Skia composition path

Uno already owns the `SKCanvasElement` drawing surface. SkSL runtime effects integrate photo processing into that composition path, with hardware execution when the host has a GPU-backed canvas and a software execution path for portable tests/export. A separate WebGPU device would require explicit device lifetime, texture-sharing, synchronization and platform hosting work; it is not implemented in this version.

SkiaSharp managed/native packages remain aligned at 3.119.4 with Uno SDK 6.7.30. Upgrading one package independently can violate the native ABI. CI's Chromium/SwiftShader run demonstrates functionality, not physical-GPU performance. Decode, brush texture publication, catalog serialization and image encoding remain synchronous CPU/native operations.

## Cache ownership and invalidation

`PhotoRenderer` owns decoded-image, shader, curve-table and brush-coverage caches. Original array identity prevents source reuse after reopening a catalog under an existing photo ID. Pixel-aware equality prevents metadata-only work from rebuilding identical processing.

`ToneLookupCache` retains up to eight semantic curve tables independently of exposure/mask parameters. `BrushCoverageCache` retains floating-point accumulators and detects appended dab prefixes. It updates only new dab regions, while an earlier-stroke edit or undo triggers replay. It publishes an immutable coefficient texture after changes; this is not an incremental GPU upload or native-resolution tiled brush engine.

Viewport previews target 2560 pixels; thumbnail decode targets 384 pixels; final thumbnails are bounded to 240×160. Brush coverage is capped at a 1024-pixel long edge including export. Retained cache budgets do not bound all temporary arrays, codec scratch allocations, GPU copies or original source memory. See [performance methods and evidence](PERFORMANCE.md).

## Controls and workbench

Original controls include `LightButton`, `IconView`, `AdjustmentSlider`, `ToneCurveView`, `PointCurveEditor`, `ColorWheel`, `ColorGradingEditor`, `ColorMixerEditor`, `MaskSettingsEditor`, `BrushSettingsEditor`, `PhotoCard`, `HistogramView` and `PhotoViewport`. Standard Uno TextBox/ComboBox/CheckBox/ScrollViewer and OS file pickers remain deliberate primitives.

The value editors emit preview/commit/cancel events without owning a catalog. Pointer capture keeps each gesture coherent; Escape cancels an active gesture before a later command can leave its tool. Brush settings are tool parameters captured when a new stroke starts, not destructive rewrites of earlier strokes.

Workbench layout, catalog view maintenance, diagnostics, commands, XMP, masking and advanced-editor integration live in separate partial files. Photo cards and parameter editors survive value-only updates. Mask selection/structural changes rebuild the relevant inspector. Diagnostic registrations are weak references, and snapshots omit potentially large brush coordinate arrays. Periodic diagnostics are opt-in rather than a normal-session UI-tree walk.

Catalog browsing uses 60-photo pages, not a virtualized durable database. The sidebar collapses at narrower widths; the application does not claim complete mobile photography UX or assistive-technology parity.

## Storage and XMP trust boundaries

`IWorkspaceStorage` supplies image/catalog picking, explicit exports and recovery. `ISidecarStorage` optionally supplies a user-authorized XMP picker. Browser images/sidecars enter through a native file input and exports use Blob downloads. Native storage uses OS pickers and bounded stream reads; recovery uses temporary-file replacement. Browser recovery acknowledges IndexedDB transaction completion.

Catalog serialization is source-generated for trimmed WebAssembly. Schema 3 preserves RGB curves and strokes; schemas 1 and 2 migrate with neutral defaults. Unknown schemas, malformed identities and oversized source/brush data are rejected. Source bytes remain unmodified.

XMP parses namespace-qualified metadata and a specific Camera Raw parameter/curve subset. Native LightSpace state is stored separately. UI import shows a report before applying a single transaction to the intended photo. DTDs/external resolution are disabled, bytes/nesting are bounded, and conflicting scalar fields or multiple RDF subjects are rejected. Unsupported fields are reported, not interpreted as equivalent Adobe processing. Unknown third-party properties are not retained on re-export.

There is no telemetry, account, cloud photo upload, inference service or licensing server. Local recovery is unencrypted and origin/profile-specific. Multiple browser tabs do not merge competing catalog writes. Static hosting receives ordinary asset requests. Optional photographs/fonts are retrieved at build time, not from user photo sessions.

## Embedding and ownership

```csharp
var session = new EditorSession(catalog);
var workspace = new StudioView(session, storage, recoveryLoaded: restoredFromRecovery);
window.Content = workspace;
// Dispose the workspace when its containing host closes.
```

`StudioView` owns its renderer, thumbnail cache, timers and event subscriptions. A standalone `PhotoViewport` receives an external renderer and does not dispose it. `PhotoRenderer`, `BrushCoverageCache`, `ToneLookupCache` and `ThumbnailCache` must be disposed by their owners. Model snapshots and encoded originals remain catalog-owned.

```csharp
var editor = new PointCurveEditor { Value = session.Active!.State.Develop.Channels };
editor.Previewed += curves => session.Preview(state => state with
{
    Develop = state.Develop with { Channels = curves }
});
editor.Committed += () => session.CommitGesture("RGB curves");
editor.Canceled += session.CancelGesture;
```

Hosts also refresh the editor's Value after undo, presets or photo selection. The workbench attaches/detaches that subscription with editor lifetime. State, controls and caches are owner-thread-confined; the recovery coordinator serializes asynchronous storage completion on that logical owner.

## Verification and delivery boundaries

The engine runner covers normalization, comparisons, transactions, actual exported pixels, curve interpolation, scalar/raster brush equivalence, cache invalidation/reuse, XMP namespace and malformed-input cases, migration and delayed recovery writes. Reports are machine-readable and failures return a nonzero exit code.

Playwright observes read-only arranged-control bounds, then sends real pointer, keyboard, file-picker and download input. It does not use a mutation-only testing API for edits. Screenshots and clipped photo comparisons check actual output; cache counters verify avoided work. Native CI currently certifies compilation on three operating systems, not physical input devices or GPU drivers.

Published bundles include `build-info.json`. Pages downloads only trusted successful main-build artifacts, verifies their commit, deploys and repeats browser tests against the public identity. Release packaging is separate from native signing/notarization and NuGet.org publication, neither of which is configured.
