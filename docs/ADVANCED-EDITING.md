# RGB curves, brush masks and XMP sidecars

## RGB point curves

Open **RGB curves** in Edit. The RGB tab is the master transfer curve; Red, Green and Blue are independent channel curves. Click the graph to add an interior point, drag a point, or enter its Input/Output value. Endpoints retain input coordinates 0 and 255 but their output levels can change. Delete removes an interior point; Reset clears the selected channel. Up to 32 points are supported per channel.

Smooth mode uses shape-preserving cubic Hermite interpolation. Interior slopes are weighted harmonic means when adjacent secants have the same sign and zero otherwise; endpoint slopes are limited. This avoids segment overshoot, including non-monotone curves. Linear mode interpolates directly. Arrow keys move the selected point in 1/255 steps. Escape cancels the active pointer gesture. One completed drag is one undo transaction.

The processing order is the legacy five-point curve, master point curve, then the independent RGB channel curves. Retaining the legacy curve preserves existing presets and catalogs. New point curves work on normalized sRGB-encoded values, not scene-referred RAW values, and are an original implementation rather than Adobe's interpolation algorithm.

`PointCurve` and `ChannelCurves` are UI-independent models. `CompiledPointCurve` copies its control points and provides allocation-free evaluation. The renderer compiles the master/channel composition into a 2048-entry RGBA-float lookup image. Changing exposure or mask parameters reuses that lookup; a curve edit creates a new one. The cache retains up to eight tables. Linear texture sampling approximates the analytic curve between samples; image output remains 8-bit sRGB in this release.

```csharp
var curve = new PointCurve
{
    Interpolation = CurveInterpolation.Smooth,
    Points = [new(0, 0), new(.25f, .16f), new(.75f, .84f), new(1, 1)]
};
session.Edit("Contrast curve", state => state with
{
    Develop = state.Develop with { Channels = state.Develop.Channels with { Master = curve } }
});
```

`PointCurveEditor` is a reusable Uno control with `Value`, `Previewed`, `Committed` and `Canceled`. A host supplies its transaction policy and refreshes Value after external edits, loading another photo, and undo/redo. The workbench keeps the editor alive during a gesture rather than rebuilding its inspector.

## Freehand brush masks

Choose **Brush** from Edit, press **B**, or use **New brush** / **Paint selected** in Masking. A new brush mask starts with zero spatial coverage. Painting an existing radial, linear or luminance mask modifies its spatial selection: paint adds coverage and erase subtracts it. Luminance restriction, inversion, amount and local adjustments are applied afterward.

Expand **Brush settings** to change size, feather, flow or density. Paint and Erase select the tool mode; holding Alt temporarily erases. The bracket keys change size. Settings are captured at stroke start and do not rewrite earlier strokes. The cursor shows the outer radius and inner fully weighted region. Mouse input uses full pressure; pen input scales radius and flow using the supplied pressure. Physical pen hardware has not been certified by headless browser tests.

Pointer coordinates are mapped back through crop, flips and quarter turns into source-normalized coordinates. Radius is expressed relative to source-image height, with the source aspect ratio preserving circular geometry. Samples are resampled by arc length at one quarter of the base radius; a denser stream of otherwise collinear pointer events does not produce a denser stroke. A stroke is committed on release and canceled with Escape or lost capture.

Each dab has source X/Y and pressure. Within a stroke, accumulated amount follows `amount += (density - amount) * flow * pressure * falloff`. Feather uses smoothstep between the inner and outer radii. Each complete stroke is composited once into the current coverage. Erase multiplies coverage by `1 - amount`; paint adds `(1 - coverage) * amount`.

### Incremental coverage cache

The raster cache represents all brush edits as an affine operation on the analytic spatial mask:

```text
coverage = multiplier * analyticCoverage + bias
```

For a growing stroke, it retains floating-point accumulation and updates only newly appended dabs inside their raster bounds. Incremental updates use the equivalent residual coverage of the new dab, so density remains a per-stroke ceiling rather than repeatedly compositing partial stroke snapshots. An immutable two-channel texture stores multiplier and bias for shader sampling. Local exposure, saturation, range limits and metadata do not replay unchanged brush geometry. Undo or editing an earlier stroke triggers a complete replay; append-only painting does not.

The source-normalized stroke data is retained, but the current coverage texture has a **1024-pixel maximum long edge**, including export. This bounds interactive work but is not native-resolution brush rasterization for very large images. Texture coefficients are 8-bit; CPU accumulation remains floating-point. Fine edges can differ from an unlimited-resolution reference. Each mask allows 64 strokes, 4096 dabs per stroke and 65,536 total dabs; oversized serialized stroke data is rejected. A replay has a 200-million visited-pixel safety budget. Cache retention is bounded separately from decode budgets; it does not bound all transient allocations or GPU copies.

`BrushStrokeBuilder`, `BrushStroke`, `BrushDab`, `BrushCoverageCache` and `BrushSettingsEditor` are reusable. Snapshot arrays are immutable by contract: create new arrays rather than mutating existing arrays in place. Renderer/cache objects are owner-thread-confined and disposable. `BrushCoverageCache.Statistics` reports replay, dab, pixel-visit and texture-build counts; those are work counters, not GPU durations.

## XMP interchange

Open **Photo information** or **XMP** in Edit. **Import XMP** opens a user-selected sidecar and presents a compatibility report before applying it. **Metadata only** leaves development, crop, masks and cloning unchanged. **Prefer embedded LightSpace settings** controls whether a native extension or the Camera Raw subset supplies processing values. The import is one undoable edit on the active photograph; changing that photograph during review prevents accidental application to the wrong target.

Supported standard metadata is `xmp:Rating`, `xmp:Label`, `dc:description` and `dc:subject`. Captions prefer the `x-default` language alternative. RDF Bag and Seq keyword containers are accepted. Rating -1 maps to rejection; ordinary ratings map to whole stars. Namespace URIs, not textual prefixes, identify properties. Attribute and simple-element representations are supported for scalar mapped fields. This is not a complete IPTC/EXIF metadata editor.

The Camera Raw subset maps Exposure2012, Contrast2012, Highlights2012, Shadows2012, Whites2012, Blacks2012, Vibrance, Saturation, Texture, Clarity2012, Dehaze, Sharpness, LuminanceSmoothing, GrainAmount, ConvertToGrayscale and the four ToneCurvePV2012 sequences. Unsupported fields are reported. Camera Kelvin white balance, lens profiles, Adobe AI/brush-mask serialization, crop geometry, grading and proprietary processing versions are **not** imported as equivalent edits. Values use LightSpace's original algorithms; equal numbers do not guarantee equal Adobe-rendered pixels.

**Export XMP** writes a separate sidecar without modifying the image. The supported Camera Raw curve subset quantizes control coordinates to 0–255. A combined non-neutral legacy/master curve is sampled to 32 points. Metadata-only export omits processing data. The optional LightSpace namespace stores the complete normalized photo state, including masks, brush strokes, curves, crop and grading. LightSpace-to-LightSpace round trips preserve those settings; other applications may ignore or remove the extension. Standard metadata is read after native settings so another application's rating/caption changes can override stale embedded metadata.

Sidecars are UTF-8, at most 16 MiB, with a 64-level nesting limit. DTDs and external resolution are disabled. Conflicting duplicate scalar values and multiple RDF subjects are rejected. Malformed or out-of-range supported values are reported or clamped explicitly. Unknown third-party properties are not preserved on re-export. Interoperability tests use namespace-correct synthetic packets and native round trips; interactive validation in Adobe Lightroom is not claimed.

```csharp
var imported = XmpSidecar.Import(xml, session.Active!.State,
    new XmpImportOptions(MetadataOnly: false, PreferLightSpaceSettings: true));
// Present imported.Warnings before applying.
session.Edit("Import sidecar", _ => imported.State);
var exported = XmpSidecar.Export(session.Active.State);
```

Native/browser hosts implement `ISidecarStorage` as an optional capability alongside `IWorkspaceStorage`. An embedded host without that capability receives an explicit unsupported-picker message rather than a nonfunctional file action.

## Catalog migration and validation

New catalog files use **schema 3**. Schema-1 and schema-2 catalogs load with neutral missing curve/brush defaults and save as schema 3. Older builds reject the new schema instead of silently losing edits. Keep a pre-upgrade backup for interoperability with older builds.

Engine tests cover interpolation, scalar-versus-raster brush coefficients, append/replay equivalence, undo invalidation, actual exported pixels, native round trips, bounded/malformed XML, namespace handling and cache reuse. Browser tests use actual pointer capture, keyboard input, file pickers and downloads. Diagnostics expose curve points and brush counts but deliberately omit stroke coordinates; normal sessions do not enable periodic diagnostics.

### Primary format/workflow references

- Adobe XMP documentation: https://developer.adobe.com/xmp/docs/
- Adobe XMP Toolkit SDK and specifications: https://github.com/adobe/XMP-Toolkit-SDK
- Adobe Lightroom Classic metadata workflow: https://helpx.adobe.com/lightroom-classic/desktop/organize-photos-in-lightroom-classic/metadata-basics-actions.html

These references describe the external formats/workflows; they do not certify that LightSpace implements all their features.
