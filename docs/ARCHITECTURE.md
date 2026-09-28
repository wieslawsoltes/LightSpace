# Architecture

## Dependency graph

```text
LightSpace.App (browser / native startup and storage)
  └─ LightSpace.Workbench
       ├─ LightSpace.Storage
       └─ LightSpace.Controls
            ├─ LightSpace.Editing
            │    └─ LightSpace.Catalog
            │         └─ LightSpace.Core
            └─ LightSpace.Rendering.Skia
                 └─ LightSpace.Imaging
                      └─ LightSpace.Core
```

Core, Catalog, Editing and Storage do not require a graphics/UI host. Imaging and Rendering.Skia require matched SkiaSharp native assets. Controls and Workbench target Uno browser and desktop.

## Models and transactions

`CatalogDocument` owns original image bytes, identities, albums and active photo identity. Each `PhotoDocument` holds a replaceable `PhotoState`: development/grading, crop, masks, clone spots and catalog metadata. These records use array-backed members; consumers must treat arrays as read-only and edit by replacement. Normalization preserves valid references and clones only collections needing repairs.

`EditorSession.Preview` opens/updates a gesture. `CommitGesture` adds one before/after transaction, and `CancelGesture` restores the opening snapshot. Batch changes form one transaction. Undo is limited to 100 transactions and redo is cleared by a new committed edit. Imports and album structure changes are not currently undoable.

`PhotoStateEquality.All` compares complete normalized values without JSON serialization. `Shader` excludes crop and metadata; `Pixels` adds crop. The renderer also checks source identity. This prevents ratings, captions and mask names from rebuilding identical processing resources while retaining invalidation for replaced images.

`Changed` reports committed mutations; `ViewChanged` also reports previews and selection. The workbench keeps catalog cards alive while the current page's photo-ID sequence is unchanged. Structural mask selection/list changes rebuild the relevant inspector; parameter previews update controls in place.

## Original image-processing model

Encoded inputs are bounded before `SKCodec` inspects size/orientation. Decode normalizes all eight EXIF orientations and converts to sRGB RGBA. Main previews target a 2560-pixel long edge; thumbnail source previews target384.

The runtime effect evaluates clone stamps, lightweight spatial detail, transfer decoding, exposure/relative white balance/tone, transfer encoding, curve/color mixing, saturation/monochrome, color grading, local masks, vignette and grain. Spatial tap/grain coordinates are source-relative. Color grading follows monochrome so black-and-white looks can be tinted.

Mask geometry uses image-height units with aspect-correct rotation. Luminance selection uses imported sRGB-encoded source brightness before clone/global edits. Spatial and range coverage are intersected, optionally inverted, then scaled by amount. Local exposure/contrast/white balance/saturation are combined with that weight. CPU reference coverage and actual shader pixels are compared by tests.

Skia's portable runtime-effect profile requires uniform-array indices to be constant or statically unrollable. Mask helpers receive vector parameters from the fixed-count loop rather than dynamically indexing arrays through a function argument.

Crop, quarter turns and display-space flips form a source-to-view matrix; inverse mapping drives pointer gestures. Handles and coverage use the same source coordinates. Before/after divider position is viewport state, not a document edit. Coverage overlay is absent from histogram/export paths.

This is an original LDR approximation, not Adobe's pipeline, calibrated RAW processing or HDR scene-referred color. Smaller previews lack native-resolution spatial information, so fine-detail preview and full-resolution export may differ. [Color/mask semantics →](COLOR-AND-MASKS.md)

## Skia integration and budgets

`SKCanvasElement` places photo development in Uno's existing Skia composition path. The runtime shader can execute on a hardware-backed canvas when the host provides one and supports software raster execution otherwise. There is no separate WebGPU device/context or compute graph in this release. Managed/native Skia packages are pinned together at3.119.4 with Uno SDK6.7.30.

`PhotoRenderer` is owner-thread-confined and exposes count/byte-budget settings. Main previews retain at most five images under a128MiB decoded-image budget. The thumbnail renderer retains at most two384-pixel sources under8MiB; final thumbnail cache has128 entries. These are retained-cache budgets, not peak codec/CPU/GPU memory limits. Auto tone samples96×64 pixels; histogram sampling uses192×128.

Decode, offscreen thumbnail rendering, histogram sampling and export retain synchronous CPU/native work. CI uses SwiftShader, not physical-GPU timing. [Performance measurements and remaining work →](PERFORMANCE.md)

## Controls

`LightButton` uses original chrome and vector `IconView` paths. `AdjustmentSlider` supplies custom rails/knobs, pointer capture, numeric input, keyboard editing, cancellation and a range-value automation peer. `ToneCurveView`, `HistogramView`, `ColorWheel`, `ColorGradingEditor`, `ColorMixerEditor`, `MaskSettingsEditor` and `PhotoCard` are reusable public controls.

Value editors expose Previewed/Committed/Canceled events so hosts choose their transaction policy. Controls are refreshed after external changes such as undo and photo/preset selection. Panel expansion is retained within the running workbench. Standard Uno TextBox, ComboBox, CheckBox, ScrollViewer and native file pickers remain in use; not every primitive is custom.

The grid uses60-photo pages rather than an indexed/fully virtualized database. Narrow desktop widths collapse the library sidebar. Mobile UX and full accessibility parity remain open.

## Storage and migration

`IWorkspaceStorage` separates the shell from user-authorized platform file handling. Browser input uses an HTML file chooser and export uses Blob downloads; recovery waits for an IndexedDB transaction to commit. Native recovery uses temporary-file replacement in the local application-data directory.

`RecoveryCoordinator` acknowledges only committed revisions, excludes active previews, serializes writes and drains newer commits after an in-flight write. Unreadable previous recovery is protected until explicit replacement. Browser unload warnings track unsaved state immediately. This is not a concurrent-tab database or a close-time durability guarantee. [Recovery contract →](RECOVERY.md)

Source-generated JSON supports trimmed WebAssembly builds. Deserialization validates schema, dimensions, sizes and identities and removes orphan album references. Schema1 imports receive neutral defaults for new parameters; all new saves use schema2. Older applications reject schema2 instead of dropping new grading/mask edits. Retain portable backups.

## Embedding and ownership

```csharp
var session = new EditorSession(catalog);
var workspace = new StudioView(session, storage, recoveryLoaded: restoredFromRecovery);
window.Content = workspace;
// Dispose workspace when the containing host closes.
```

`PhotoRenderer`, `ThumbnailCache`, `PhotoViewport` and `StudioView` own native resources or subscriptions and require disposal. A standalone viewport does not dispose an externally supplied renderer. The workbench owns its private renderers/caches. Original arrays remain owned by the catalog and must not be mutated in place.

```csharp
using var renderer = new PhotoRenderer();
var viewport = new PhotoViewport(session, renderer);
var slider = new AdjustmentSlider("Exposure", -5, 5, 0, 0.01);
slider.ValueChanged += value => session.Preview(state => state with
{
    Develop = state.Develop with { Exposure = value }
});
slider.ValueCommitted += () => session.CommitGesture("Exposure");
slider.GestureCanceled += session.CancelGesture;
```

## Validation and deployment

Engine tests exercise actual pixels, coverage geometry, normalization, catalog migration, semantic equality, transactions, recovery races and cache behavior. The performance report explicitly labels CPU microbenchmarks and managed allocations.

Playwright reads arranged control bounds and sends actual pointer, keyboard and file-chooser input. It does not modify photo state through a test-only mutation API. Read-only diagnostics are enabled by the `diagnostics` query parameter; normal browser sessions do not periodically walk/serialize the UI tree.

Build artifacts carry commit identity in `build-info.json`. Pages deploys only a successful trusted main-branch artifact, validates provenance, verifies the live identity and reruns browser tests. Desktop CI certifies compilation, not every native input/accessibility/GPU combination.
