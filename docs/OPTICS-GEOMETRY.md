# Optics, geometry and photography controls

## Manual optical correction

Open **Optics** from the right tool rail or expand the Optics section in Edit. Distortion changes barrel/pincushion sampling. Lens vignetting changes peripheral light falloff. Red / Cyan and Blue / Yellow change radial channel alignment relative to green. Reset optics clears only those four corrections.

This is an original manual model, not Adobe's algorithm or a calibrated lens-profile database. No automatic lens recognition or profile download is performed. Corrections participate in gesture undo/cancel, copy/paste, synchronization, named versions, portable catalogs, native XMP and recovery. Global creative presets retain these separately stored settings.

The distortion mapping uses normalized corrected coordinates `(u, v)` and source aspect `a = width / height`:

```text
q = ((u - 0.5) * a, v - 0.5)
r² = 4 * dot(q, q) / (a² + 1)
k = distortion * 0.002
source = 0.5 + (u - 0.5, v - 0.5) / (1 - k * r²)
```

Positive distortion values sample farther from the source center, leaving transparent edges when the corrected frame exceeds the source. Negative values sample inward. Channel alignment scales this source displacement by `1 + correction * 0.0001`. Outside-source samples are transparent; alpha is the minimum of the sampled channels. Vignetting applies a radial, linear-light exposure multiplier before returning to sRGB transfer encoding.

The coordinate inverse is analytic: a source displacement is multiplied by `2 / (1 + sqrt(1 + 4*k*r²))`. There is no per-pointer iterative lens solve. Invalid mappings return non-finite coordinates and are rejected by source-picking/painting paths instead of clamped into a false edge sample.

These sliders operate on a fixed corrected frame; they are not a general lens-calibration or panorama projection system. Fine optical correction is limited by the retained preview resolution. Export decodes the original, but still uses the same original LDR processing model.

## Projective framing

Open **Geometry** on the tool rail or its Edit section. Vertical and Horizontal introduce projective perspective changes; Rotate supports -45° to +45°; Aspect adjusts horizontal shape; Scale ranges from 50% to 200%; X/Y Offset moves the corrected image. **Constrain crop** enlarges the transformed source until the output frame falls inside a conservative valid image region. Reset geometry restores these settings without clearing optical or development adjustments.

The homogeneous transform is computed in double precision. Coordinates are centered, converted to source-aspect-corrected units, projected, scaled, rotated, converted back to normalized units and translated. The resulting matrix is converted to Skia's pixel-coordinate matrix for drawing. Crop, quarter turns and flips are applied afterward.

Constrain crop checks inverse-mapped frame corners against a conservative source rectangle that also accounts for optical distortion and channel displacement. It brackets and binary-searches an additional scale. The bounded solver may crop more than a maximum-area optimizer; it is not Adobe Upright or a guided automatic building-correction algorithm. Extremely narrow images can require a large enlargement. Invalid or unsupported projection ranges fail explicitly rather than publishing NaN matrices.

Projection inverses are weakly cached by immutable settings, source aspect and optical settings. This avoids repeating the constrained-framing solve for each outline point. The cache does not retain discarded photo-state objects indefinitely.

## Crop and straightening

Crop retains free rectangles, handles, movement, centered aspect-ratio presets, quarter turns and flips. The ratio buttons are centered presets, not a continuous aspect lock during later free dragging.

**Straighten** activates a line tool. Drag along a horizon and release to apply its aspect-corrected angular correction as one undoable gesture. Escape restores the opening angle. The line is a guide only and does not appear in export. The tool returns to Crop on successful release; Done returns to Edit.

The crop rectangle is defined in the corrected frame. Mask pins, brush dabs, clone positions and sampled colors remain in orientation-normalized original-source coordinates. Their display mapping is:

```text
source → inverse optical mapping → projective geometry → crop / quarter turns / flips → viewport
```

Pointer input reverses this sequence, including the nonlinear optical stage. Mask outlines and brush cursors follow the same mapping. Color picking and white balance read original source pixels rather than sampling the developed view or its overlays.

Original/Before comparison retains optical and geometric alignment so both sides have the same framing. It compares development rather than showing an entirely uncorrected camera frame.

## White-balance picker

Press **W** or use the picker beside White balance in Edit. Click a neutral patch in the photograph. The picker reads the default bounded 5×5 patch of the retained source preview, with existing alpha weighting and orientation handling, then estimates Temperature and Tint under LightSpace's relative channel-gain model.

The estimator solves the red/blue gain balance in linear sRGB, then balances green against their corrected mean. Black/transparent samples are rejected. Values that require gains outside the available slider range are clamped and reported. A successful pick is one edit and returns the viewport to Edit. Escape leaves the picker.

This is not a calibrated Kelvin, camera-profile or RAW white-balance system. An arbitrary colorful patch is not a useful neutral reference; bounded previews also cannot provide full-resolution tiny-detail sampling.

## Interactive histogram and clipping

The histogram has five horizontally arranged tone regions: Blacks, Shadows, Exposure, Highlights and Whites. Drag a region left/right to adjust that tone. The selected region is fixed for the gesture, its starting value is captured once, and release commits one transaction. Escape cancels. Focused arrow keys make small changes to the selected region.

The corner triangles independently toggle shadow and highlight clipping. **J** toggles both indicators. Shadows are shown blue when all displayed channels are near zero; highlights are red when any channel is near full scale. These are LDR display thresholds, not proof that recoverable RAW scene information has been lost. Filled triangle state reflects sampled histogram endpoint bins.

Clipping is a viewport-only shader option. It does not create a document edit, enter a catalog, alter histogram measurements, or contaminate exported pixels. The mask-coverage overlay likewise remains excluded from export and histogram analysis. Histogram data is still generated from a small CPU/raster sample; making its UI interactive does not turn that measurement into a GPU compute histogram.

## Resizable workspace

Drag the divider at the library edge or the left edge of the edit panel. Widths are bounded; Escape restores the starting width, double-click/Home resets it, and focused arrow keys resize in small increments. Focus mode (**F6**) hides the side panels and filmstrip. **F7**, or the filmstrip button, toggles the filmstrip independently. These operations do not change the photo revision.

Widths, focus mode and visibility are held in the current workbench session, not written into the photographic catalog. The compact ratio grid, additional tool-rail entries, histogram triangles and dedicated Optics/Geometry sections extend the existing dark photography layout. This is not pixel-identical Adobe chrome, a complete keyboard-equivalence map, or a finished phone layout.

## GPU composition and invalidation

The retained development shader is passed as the child of a separate presentation runtime effect. That effect evaluates optical sampling, channel alignment, lens falloff and clipping. Geometry is supplied as a draw matrix. There is no intermediate CPU-rendered image between the development and optical display stages, and no image readback is used for their hit testing.

Skia supports directly evaluating a child runtime effect and composing the shader tree into its rendering pipeline. Uno owns the `SKCanvasElement` surface; hardware acceleration depends on that host providing a GPU-backed canvas. LightSpace does not create a separate WebGPU device, and it does not force hardware acceleration on a software-only host. See [Skia runtime effects](https://skia.org/docs/user/sksl/) and [Uno SKCanvasElement](https://platform.uno/docs/articles/controls/SKCanvasElement.html).

Development, optical presentation and geometry use separate invalidation keys. A geometry-only edit reuses decoded images, development and optical shaders, curve tables and brush coverage; only the matrix changes. Histograms have an independent presentation cache so their refreshes do not rebuild the viewport's optical shader or toggle its clipping state. Metadata changes preserve all pixel stages.

Chromatic alignment requires extra child evaluations for displaced red and blue channels. With complex masks/detail processing that can cost more than a single sample; shader composition is not a claim that every combination is universally fastest. Decode, brush coefficient publication, histogram sampling and image encoding remain CPU/native work. Preview/brush/export resolution and memory limits are unchanged.

The 0.4.1 `RuntimeShaderScope` lifetime boundary is preserved. Owned and borrowed child shaders are distinguished, the compiled result remains field-rooted during input cleanup, and ownership is transferred only after cleanup completes. Deferred-draw and forced-finalization regressions exercise the composed stage as well as the original development path.

## Public components and compatibility

`GeometrySettings`, `LensCorrectionSettings`, `ProjectiveTransform`, `GeometryProjection`, `LensMapping` and `WhiteBalanceEstimator` are UI-independent Core types. `GeometryMapping` bridges them to Skia. `GeometryEditor`, `OpticsEditor`, `HistogramView` and `PanelResizeGrip` are reusable Uno controls; their events let an embedding application own transactions.

```csharp
var editor = new GeometryEditor { Value = session.Active!.State.Geometry };
editor.Previewed += geometry => session.Preview(state => state with { Geometry = geometry });
editor.Committed += () => session.CommitGesture("Geometry");
editor.Canceled += session.CancelGesture;
```

Photo state now uses catalog schema **5** and native XMP settings version **5**. Catalogs 1–4 and native XMP versions 3–4 migrate with neutral missing fields. Older builds reject version 5 rather than silently dropping corrections. IndexedDB remains version 2 and the recovery manifest remains format 1; no site-data reset is required. Preserve portable pre-upgrade backups when using older builds.

Tests cover analytical inverses, randomized projective round trips, constrained framing, actual optical/CA/vignette pixels, white-balance neutralization, overlay exclusion, shader lifetime, migration, synchronization and cancellation. Browser tests use actual controls, exported files and recovery reload. Work counters and CPU/raster timings are not physical-GPU frame-time certification.
