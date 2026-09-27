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

Core, Catalog, Editing and Storage require no graphics or UI host. Imaging and Rendering.Skia require SkiaSharp plus the correct native assets on the target. Controls and Workbench are Uno libraries targeting browser and desktop.

## State and transactions

`CatalogDocument` owns original photo bytes, photo identities, albums and active selection identity. Each `PhotoDocument` has a replaceable `PhotoState` snapshot. Development settings, crop, masks, clone spots and metadata are represented as record values. Array-backed members should be treated as read-only by consumers; edits create new arrays rather than mutating snapshots in place.

`EditorSession.Preview` opens or updates a gesture. `CommitGesture` records a single before/after transaction. `CancelGesture` restores the opening snapshot. Atomic batch edits contain all changed photo states in one transaction. Undo is bounded to 100 transactions; redo is cleared on a new committed edit. Structural imports and album creation are currently not part of undo history.

`Changed` reports committed catalog changes. `ViewChanged` also reports previews and selection changes. UI sliders subscribe to preview events; thumbnail rebuilding, histogram work and debounced recovery are attached to committed changes rather than every pointer movement.

## Development pipeline

1. Encoded input is bounded before decoding. `SKCodec` inspects dimensions and orientation.
2. Decode normalizes all eight EXIF orientations and converts to an sRGB RGBA image. Viewport previews are limited to a 2560-pixel long edge.
3. `PhotoRenderer` retains up to five decoded preview images and their revision-keyed runtime shaders. A shader is reconfigured when the corresponding photo revision changes.
4. Clone stamps sample the source image with feathered destination coverage. Lightweight neighborhood taps implement detail, sharpening, and smoothing approximations.
5. RGB is converted from sRGB transfer encoding to linear values for exposure, relative white balance, and tonal adjustments. It is encoded again for the fixed point curve and hue-weighted color mixer.
6. Global vibrance/saturation/monochrome and up to eight local gradients are evaluated. Local exposure uses another transfer conversion. Vignette and deterministic grain finish the LDR result.
7. Source-normalized crop, quarter-turn orientation, and output-space flips are combined into a source-to-view matrix. The inverse matrix maps pointer input to source coordinates.
8. Exports decode the original, evaluate the same shader on an offscreen raster surface, apply crop/rotation, and encode an 8-bit sRGB output.

This is an original photographic approximation, not Adobe's processing model. It is not a scene-referred RAW or HDR pipeline, and it does not promise Lightroom-equivalent pixels. Texture radius and grain are currently resolution-dependent; preview and full-resolution export need not match perfectly for spatial effects. Numerical identity, actual pixel change, crop dimensions, local-mask coverage, and representative source transforms are covered by engine tests.

## Why Skia runtime effects

Uno already owns a Skia rendering surface through `SKCanvasElement`. Integrating photo development as a runtime effect keeps the viewport in that composition path, enables a hardware-backed execution path when the host provides one, and keeps a portable software implementation for export and deterministic tests. A separate WebGPU context would require independent device ownership, texture interchange, lifetime synchronization, and native/browser hosting integration.

The chosen API is therefore an integration decision, not a claim that one GPU API is universally fastest. There is no separate WebGPU compute backend in this version. CPU-side decode, thumbnail generation, histograms and export remain explicit. Browser CI uses SwiftShader, not a physical-GPU benchmark.

Managed SkiaSharp and the native assets are kept at the same 3.119.4 version compatible with the selected Uno SDK. Do not upgrade a single SkiaSharp package independently. The root version property is the source of truth.

## Controls and layout

`LightButton` has original chrome and a custom control template. `IconView` draws original vector paths without a symbol font. `AdjustmentSlider` provides a custom GPU-capable rail/knob, direct numeric entry, pointer capture, keyboard changes and a range-value automation peer. `ToneCurveView` edits the fixed control points. `HistogramView` draws actual sampled color distributions.

`PhotoViewport` maps pointer interactions into editor transactions. It supports crop handles, panning, wheel zoom, comparison, radial/linear-gradient creation and clone-source selection. `PhotoThumbnail` uses an LRU thumbnail cache; `PhotoWrapPanel` supplies responsive grid layout. `PanelSection` provides original collapsible inspector chrome.

The workbench composes these controls with ordinary Uno layout and input primitives. TextBox, ComboBox, CheckBox and ScrollViewer have not been reimplemented from scratch. Album/navigation rows, file dialogs within the app, development sections and rails are assembled in reusable C# UI code. The native OS file picker is intentionally platform-provided.

The catalog is paged in batches of 60. This bounds instantiated controls, but it is not true indexed/virtualized storage. The sidebar collapses at narrower desktop widths; this is not a polished mobile photography UI.

## Storage and trust boundaries

`IWorkspaceStorage` separates the workbench from platform file handling. The browser implementation uses an HTML file picker only for user-authorized import and Blob downloads for export. Recovery is an IndexedDB transaction that resolves only after commit. The desktop implementation uses platform file pickers and atomic temporary-file replacement for recovery.

Catalog JSON uses a source-generated serializer for trimming-safe WebAssembly builds. Schema versions, source sizes, dimensions, duplicate photo IDs and album references are checked before replacing the active catalog. Limits are intentionally conservative but are not a sandbox: native codecs still process untrusted image bytes and need security updates.

No application telemetry, login, cloud upload, external inference or license server is used. The static host receives ordinary asset requests. Optional demo images and the OFL font are downloaded during build, not from user sessions. Browser storage is origin-local and unencrypted. Multiple tabs do not merge competing catalog writes; use one editing tab per browser profile.

## Embedding and ownership

```csharp
var session = new EditorSession(catalog);
var workspace = new StudioView(session, storage);
window.Content = workspace;
// Dispose the workspace when its containing window is closed.
```

`PhotoRenderer`, `ThumbnailCache`, `PhotoViewport` and `StudioView` own native resources or event subscriptions and must be disposed. A standalone viewport receives a renderer from its host; disposing the viewport does not dispose that external renderer. `StudioView` owns its private renderer and thumbnail cache. Model snapshots and original byte arrays remain owned by the catalog.

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

## Test and release boundaries

The engine console runner exercises model normalization, catalog round trips, malformed input rejection, transactions, masking, actual shader pixels, exports, cache bounds and geometry. It exits nonzero on failure and emits machine-readable results.

Playwright uses read-only diagnostic bounds from actual arranged Uno controls, then sends real mouse, keyboard and file-chooser input. It does not change editor state through a testing-only mutation API. It checks startup, edits, undo/redo, presets, navigation, crop controls, mask gestures, file import, JPEG export and recovery, and records screenshots.

Each published static bundle contains `build-info.json` identifying its commit and host. Pages downloads only artifacts from a trusted successful Build run, verifies provenance, deploys, checks the public identity and reruns the browser acceptance suite. Desktop CI currently certifies compilation; native input, assistive technology, OS integration and physical-GPU behavior still require target-device testing.
