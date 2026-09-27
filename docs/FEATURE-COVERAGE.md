# Feature coverage and boundaries

This ledger distinguishes implemented functionality from Lightroom features that are absent or approximate. No entry claims pixel-identical UI or Adobe processing parity.

| Area | Implemented | Boundary |
| --- | --- | --- |
| Application shell | Dark photography workspace, library rail/sidebar, photo center, filmstrip, right inspector/tool rail, custom iconography/chrome | Desktop Lightroom-inspired; not exact branded or pixel-identical UI; no Lightroom Classic modules |
| Reuse | Eight independently packable libraries; shared browser/native host | Some workbench subviews are assembled inside StudioView rather than standalone controls |
| Image input | JPEG, PNG, WebP, BMP, first GIF frame; EXIF orientation and sRGB decode | No camera RAW/demosaic, HEIF/HEIC, TIFF, video or animated-image workflow |
| Catalog | Source retention, albums, selection, token search, star/flag filters, paged grid/filmstrip | In-memory JSON catalog, not an indexed million-photo database; no folder watcher, relinking or Lightroom catalog import |
| Development | Tonal sliders, relative white balance, vibrance/saturation, monochrome | Original approximations; no camera calibration, DCP/ICC profile authoring, eyedropper, RAW processing versions |
| Point curve | Five editable fixed-X points with identity reset | Piecewise linear RGB curve; no per-channel curves or arbitrary control points |
| Color mixing | Eight weighted hue bands with hue/saturation/luminance controls | HSV approximation; no perceptual color-space guarantees or point-color selection |
| Detail/effects | Sharpening, spatial smoothing, texture/clarity/dehaze approximation, vignette and grain | No AI denoise, super-resolution, calibrated lens corrections; spatial effects are resolution-dependent |
| Crop | Free rectangle, edge/corner handles, movement, common centered ratios, quarter turns, flips; click-without-drag protection | No arbitrary straightening angle, perspective/Upright or continuous aspect locking while dragging |
| Local masks | Up to eight radial and vertical linear gradients, feather/exposure/saturation, inversion and deletion | No brush, object/sky/subject AI selection, arbitrary gradient angle, range masks or mask boolean composition |
| Clone | Up to 32 feathered source stamps; Alt-click source | Not healing, content-aware or generative removal; stamps sample the original source |
| Navigation | Fit and source-pixel zoom geometry, display-scale awareness, bounded pan, pointer-anchored wheel zoom, keyboard photo navigation | Preview decoding is capped at 2560 pixels, so larger sources lack native-resolution detail inspection; no navigator minimap |
| Comparison | Original toggle and fixed midpoint split | No draggable split divider, side-by-side reference photo or multi-photo survey view |
| Undo/versions | Coalesced gesture transactions, atomic batch changes, 100-step undo, named snapshots | Undo history is session-only; imports and album structural changes are not undoable |
| Metadata | Captions, keywords, ratings, picks/rejects; source bytes retained | No EXIF/IPTC browser, metadata sidecars, copyright templates, face/location indexing |
| Export | JPEG/PNG/WebP, quality/size selection, ZIP batch, portable native catalog | 8-bit sRGB, maximum 8192-pixel long edge, metadata stripped from rendered copies; no print/soft proof/tethering |
| Recovery | IndexedDB browser recovery and atomic native recovery file | No cloud synchronization, encrypted vault, concurrent-tab merge, crash journal or instant close-time flush |
| Rendering | Skia runtime shader on the Uno canvas, bounded source and thumbnail caches | Actual GPU backend is host-dependent; CPU decode/thumbnails/histogram/export; no separate WebGPU compute graph |
| Validation | Engine suite and real pointer-driven browser acceptance suite; desktop compile matrix | CI SwiftShader is not physical-GPU certification; full native accessibility and device testing remain open |
| Distribution | Build, desktop, Pages and release workflows; NuGet package artifacts | No NuGet.org publication, signed installers, macOS notarization or automatic updater |

## Priority extension points

A production photo pipeline needs a separately licensed/implemented RAW decoding and color-management layer, floating-point working buffers, calibrated camera/lens profiles, and a tiled multi-resolution render graph. Those changes should preserve the normalized coordinate system and version the serialized processing contract.

A production catalog needs durable indexed storage, incremental source management, thumbnails outside the main catalog blob, asynchronous bounded decode scheduling, and a concurrency-aware write journal. Existing `IWorkspaceStorage` is intentionally a small starting contract rather than a full database abstraction.

The next UI milestones are native-resolution tiled inspection, complete keyboard/assistive-technology coverage, robust panel-resize persistence, arbitrary-angle crop/straightening, richer mask manipulation, and more fully extracted public workbench subcomponents.
