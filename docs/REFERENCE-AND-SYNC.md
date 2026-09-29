# Reference comparison and selective settings

This increment builds on the 0.5 optics/geometry pipeline and preserves the 0.4.1 WebAssembly shader-lifetime repair. Reference comparison and settings selection are implemented workflows, not claims of complete Adobe UI or processing parity.

## Frozen reference view

Choose **Reference view** on the left rail, or press **Shift+R**. The current photograph and its current edit snapshot are pinned on the reference side. The active photograph remains editable on the other side. Select another photograph in the filmstrip to compare it with the pinned look, or continue editing the same photograph to judge changes against the captured state.

The header provides **Pin active reference**, **Fit**, **Link navigation**, **Reference layout**, **Apply reference settings**, and **Close**. Layout switches between side-by-side and vertically stacked views. Grid mode temporarily hides the comparison; returning to Detail retains the reference. The reference is session-local and is cleared when closing comparison, replacing the catalog or reloading the app. It is not added to the catalog as another photo and does not modify undo history or recovery merely by being opened.

The reference captures a normalized immutable editing snapshot, not a screenshot. It is rendered through the same development, optical and projective Skia pipeline as the active view. Clipping indicators, local-mask overlays and editing handles are not included on the reference. Subsequent active edits do not change its settings. Original encoded bytes are shared read-only rather than copied. The reference's source and settings are snapshots under the same copy-on-write contract as ordinary undo records; hosts must not mutate their arrays in place.

Wheel zoom and drag panning work independently in each view. When linked, navigation uses **zoom relative to fit and pan relative to viewport dimensions**. This is useful across differently shaped photos but is not registered source-pixel correspondence or image alignment. Each pane clamps its own pan to retain a reachable part of its image. Fit resets both; source-pixel zoom geometry still uses the bounded preview and cannot reveal detail that was not decoded. Full-resolution tiled inspection remains unimplemented.

## Selective copy, paste and synchronization

The left rail exposes **Copy settings**, **Paste selected settings** and **Synchronize settings**. The Edit panel's Copy/Paste/Sync commands use the same implementation. **Ctrl/Cmd+Shift+C** opens group selection; **Ctrl/Cmd+C** copies all processing groups immediately; **Ctrl/Cmd+V** pastes the previously captured groups.

Thirteen groups can be selected independently: Light, White balance, Color/B&W, Tone curves, Color mixer, Color grading, Effects, Detail, Optics, Geometry, Crop/orientation, Local masks and Clone spots. All/None/Global controls adjust the selection. Global deliberately excludes spatial Geometry, Crop, Masks and Clone spots, avoiding accidental transfer of those edits by default. Selection is remembered for subsequent dialogs during the session.

Copy captures a source look and the checked groups. Paste changes only those groups on the current selection. Synchronize captures the active source and the identities of the other selected targets before opening the review dialog. **Apply reference settings** does the same for the frozen reference and the active target. The source is not re-read after the dialog opens, so the approved look cannot silently change underneath it. Changing catalogs invalidates the operation; missing target IDs reject the whole batch instead of partially applying it.

A completed application is **one undoable transaction**, regardless of the number of target photos. Every result is computed and checked before any photo is modified. Duplicate target IDs are deduplicated. Selecting no groups is rejected in the UI and is a no-op at the model layer. Applying settings already present on every target does not add history or advance the committed revision.

Ratings, flags, labels, captions, keywords, original bytes, names, albums and named versions are never overwritten by a settings transfer. Geometry/crop/mask values are transferred in the existing normalized coordinate convention, not reinterpreted as new image content. Masks and clone locations should be reviewed when target photographs have different compositions. This is not automatic exposure matching, semantic mask adaptation or Adobe's processing model.

## Reusable model and UI APIs

`EditSettingsGroup` and `EditSettingsTransfer` live in `LightSpace.Core`. `EditorSession.ApplySettings` and `SyncSelected(groups)` provide atomic editing integration. Existing parameterless `SyncSelected()` still applies all processing groups. `SettingsTransferEditor` exposes only group selection; it does not own a clipboard or document.

```csharp
var look = new EditSettingsTransfer(sourcePhoto.State,
    EditSettingsGroup.Light | EditSettingsGroup.WhiteBalance | EditSettingsGroup.ColorGrading);

// Targets are validated and committed as one transaction. Metadata is retained.
session.ApplySettings(look, selectedPhotoIds);
session.Undo();
```

`ReferencePhotoSnapshot` lives in `LightSpace.Editing`. It assigns an independent render identity while sharing immutable source bytes and snapshot data. `ReferencePhotoView` is a reusable Uno control accepting a host-owned `PhotoRenderer`, a Photo and view-only `PhotoNavigationState`. It exposes navigation and render-error events and does not dispose the supplied renderer. The host should release an unused reference's render identity with `PhotoRenderer.ReleasePhoto`.

```csharp
var pinned = ReferencePhotoSnapshot.Capture(session.Active!);
var reference = new ReferencePhotoView(renderer) { Photo = pinned.Photo };
reference.NavigationChanged += navigation => activeViewport.SetNavigation(navigation);
activeViewport.NavigationChanged += navigation => reference.SetNavigation(navigation);
// SetNavigation does not emit a feedback event.

// On replacing/closing the reference:
reference.Photo = null;
renderer.ReleasePhoto(pinned.Photo.Id);
```

Objects are confined to the host's logical UI owner. This is not a thread-safe multi-window renderer service. Dispose the workbench/renderer and unsubscribe externally held navigation listeners at host teardown.

## Shared source cache and GPU work

The renderer separates **decoded source ownership** from **photo-specific processing ownership**. Multiple photo/render identities with the same immutable encoded byte-array identity share one decoded SKImage, while each retains its own development shader, optical presentation, histogram presentation and geometry state. This prevents active edits from replacing the frozen reference shader while avoiding a second source decode for that reference.

Source reference counts are acquired before evicting an old processing entry. This matters when a one-slot renderer evicts the only other identity pointing to the same source: the image must stay alive across the handoff. Per-photo shaders are released before the last source owner is disposed. Explicit reference replacement releases only the old reference identity; it does not clear active-photo caches.

`RendererStatistics.CachedBytes` counts unique retained decoded images. `CachedImages` counts photo-specific processing identities, `CachedSources` counts unique decoded source objects, and `SharedSourceHits` counts reuse when creating another identity. These values are resource counters, not GPU durations or total-process memory. Separately allocated arrays with equal contents are not hashed by the renderer; they share decoding only after a host supplies a common immutable array, as recovery restoration already can do.

Both panes draw through Uno's Skia canvas without an intermediate CPU-rendered screenshot or full-image readback. A GPU-backed host can execute the composed effects on its GPU, but backend choice still belongs to Uno and the device. Two visible views still issue two draws and may shade more total pixels than one. This does not make decoding, export or histograms GPU-only. Divergent painted masks can require distinct coverage work; source sharing does not eliminate every cache invalidation.

Batch edits also stop repeatedly scanning the entire catalog for each changed target. They stage directly addressed photo objects; undo/redo constructs one ID lookup per transaction. This removes the previous nested lookup behavior but does not turn the paged in-memory catalog into an indexed persistent database.

## Regression and performance evidence

`ComparisonTransferTests` covers isolation of every processing group, preservation of metadata, no-op identity, invalid group bits, missing/duplicate target IDs, one-transaction batch undo/redo, captured source state, frozen reference pixels, original replacement, shared ownership across one-slot eviction, forced finalization and warm repeated paired draws.

`artifacts/engine/comparison-performance.json` records 200 warm raster draws of reference/active states sharing one source. Its assertion concerns **no additional decodes or shader builds**, not a universal frame rate. CPU raster elapsed time is included with its scope. The browser suite drives actual reference controls and sliders, checks frozen image pixels, retained source counters, linked/unlinked navigation, layout, selective copy/sync, exported target settings, undo and recovery.

The pre-existing fatal-slider stress, optics/geometry, masks, sidecar, source-integrity and recovery-publication tests remain enabled. CI uses Chromium/SwiftShader, and native jobs check compilation. These checks are not certification of every physical GPU, browser engine, accessibility system or pen device.

## Compatibility boundary

No catalog schema or recovery-store change is needed. Schema 5, native XMP settings 5, recovery manifest 1 and IndexedDB 2 remain unchanged. Reference views, link state, layout and the in-workspace settings clipboard are not persisted in the catalog. Existing optical/projective processing, original-byte retention and portable backups remain intact.

Lightroom's reference and selective-settings workflows informed this feature design; the authoritative external workflow reference is [Adobe's Develop module options](https://helpx.adobe.com/lightroom-classic/desktop/process-and-develop-photos/develop-module-options.html). LightSpace's frozen-snapshot behavior, group boundaries and normalized linked navigation are explicit independent choices, not an Adobe compatibility certification. Survey mode, virtual copies, automatic image alignment, AI masks, RAW processing and full UI parity remain outside this increment.
