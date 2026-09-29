# RGB curves, brush masks and XMP sidecars

## RGB point curves

Open RGB curves in Edit. RGB is the master curve; Red, Green and Blue are independent channel curves. Click to add an interior point, drag it or enter Input/Output values. Endpoints retain input positions 0/255 but can change output. Delete removes interior points and Reset clears the selected channel. Each channel supports 32 points.

Smooth uses shape-preserving cubic Hermite interpolation: harmonic interior slopes when adjacent secants agree, zero slopes at changes of direction, and limited endpoint slopes. Linear interpolates directly. Arrows move the selected point in 1/255 steps. Escape cancels a drag; release commits one transaction.

Processing order is legacy five-point curve, master curve, then independent RGB channels. Keeping the older curve preserves existing presets. Curves operate on normalized sRGB-encoded values, not scene-referred RAW. The interpolation is original, not Adobe-equivalent.

`PointCurve` and `ChannelCurves` are UI-independent. `CompiledPointCurve` copies its points and evaluates without allocation. `ToneLookupCache` composes them into a 2048-entry RGBA-float image, retaining up to eight tables. Exposure, masks, optics, geometry and metadata reuse an unchanged table. Finite texture sampling approximates the analytic curve; exported images remain 8-bit sRGB.

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

`PointCurveEditor` exposes Value, Previewed, Committed and Canceled. The host refreshes Value after undo, loading or photo selection and supplies its own transaction policy. The workbench retains pointer-captured editors during a gesture.

## Freehand brush masks

Choose Brush/B, New brush or Paint selected. New brush starts with zero coverage. Painting existing radial/linear/luminance/color selections modifies spatial coverage; luminance/color restriction, inversion and amount apply afterward. Paint adds, Erase subtracts, and Alt temporarily erases.

Expand Brush settings for size, feather, flow and density. Settings are captured per stroke rather than rewriting earlier strokes. Brackets change size. Mouse input uses full pressure; supplied pen pressure scales radius and flow. Headless browser tests do not certify physical pen hardware.

Pointer coordinates reverse manual optical/projective correction, crop, flips and quarter turns. Radius is source-height-relative with source-aspect correction. The cursor follows the same nonlinear/projective display mapping. Arc-length resampling at one quarter of the base radius avoids density depending on pointer-event frequency. Release commits one stroke; Escape/lost capture cancels it.

A dab stores source X/Y and pressure. Stroke accumulation is `amount += (density - amount) * flow * pressure * falloff`. Feather smooths the inner/outer radius transition. A completed stroke is composited once: erase multiplies current coverage by `1 - amount`, while paint adds `(1 - coverage) * amount`.

### Incremental coverage cache

```text
coverage = multiplier * analyticCoverage + bias
```

The cache retains floating-point accumulation and recognizes append-only dab growth. New dabs update bounded regions using equivalent residual coverage, keeping density as a per-stroke ceiling rather than repeatedly compositing partial snapshots. An immutable two-channel texture stores the coefficients. Local exposure, color/luminance restriction, optical sampling and geometric framing do not replay unchanged brush geometry. Undo or an edited earlier stroke triggers replay.

The texture has a **1024-pixel maximum long edge**, even for export. Coefficients are 8-bit while CPU accumulation is floating-point. This bounds work but is not native-resolution brushing for large originals. Limits are 64 strokes/mask, 4096 dabs/stroke, 65,536 dabs/mask and a 200-million visited-pixel replay budget. Cache budgets exclude transient object arrays, codecs and GPU copies.

`BrushStrokeBuilder`, `BrushStroke`, `BrushDab`, `BrushCoverageCache` and `BrushSettingsEditor` are reusable. Arrays are immutable by contract; create new arrays rather than editing retained data in place. Statistics count actual replay/dabs/pixels/textures, not GPU durations.

## XMP interchange

Photo information/XMP provides user-selected sidecar import and export. Import presents a report before Apply. Metadata only leaves development, geometry, optics, crop, masks and cloning unchanged. Prefer embedded LightSpace settings chooses the native extension when present; otherwise the explicit Camera Raw subset is mapped. Application is one undoable active-photo edit; a changed target during review is rejected.

Standard metadata covers `xmp:Rating`, `xmp:Label`, `dc:description` and `dc:subject`. Captions prefer `x-default`, keywords accept RDF Bag/Seq, and rating -1 maps to rejection. Namespace URIs identify fields, not prefix spelling. Scalars support attributes or simple elements. This is not full IPTC/EXIF editing.

The Camera Raw subset maps Exposure2012, Contrast2012, Highlights2012, Shadows2012, Whites2012, Blacks2012, Vibrance, Saturation, Texture, Clarity2012, Dehaze, Sharpness, LuminanceSmoothing, GrainAmount, ConvertToGrayscale and four ToneCurvePV2012 sequences. Unsupported fields are reported. Camera Kelvin white balance, lens profiles, Adobe AI/masks, crop/projective geometry, grading and proprietary processing versions are not treated as equivalent settings. Equal values do not guarantee equal pixels.

Export writes a separate UTF-8 sidecar without modifying the image. Camera Raw curve coordinates are quantized to 0–255; a combined non-neutral legacy/master curve is sampled to 32 points. Metadata-only export excludes processing. The optional native namespace retains the complete normalized LightSpace state, including optics and geometry. Other applications may ignore or remove it. Standard metadata is read after native state so externally changed ratings/captions can override stale embedded metadata.

Packets are capped at 16 MiB and 64 nesting levels. DTDs and external resolution are disabled. Conflicting scalar values and multiple RDF subjects are rejected. Malformed/out-of-range values are reported or clamped. Unknown third-party properties are not retained when exporting a new packet. Tests use namespace-correct synthetic packets and native round trips, not interactive Adobe application certification.

```csharp
var result = XmpSidecar.Import(xml, session.Active!.State,
    new XmpImportOptions(MetadataOnly: false, PreferLightSpaceSettings: true));
// Present result.Warnings before applying.
session.Edit("Import sidecar", _ => result.State);
var exported = XmpSidecar.Export(session.Active.State);
```

`ISidecarStorage` is an optional host capability alongside `IWorkspaceStorage`; a missing capability produces an explicit unsupported-picker message.

## Catalog and native-settings versions

Current catalogs use **schema 5**. Catalogs 1–4 load with neutral defaults for missing fields and save as5. Native XMP settings version5 accepts3–4. Older builds reject unsupported versions rather than silently drop corrections. The recovery manifest stays format1 and browser database stays version2. Retain older portable backups for older-version interoperability.

## Validation

Engine tests cover interpolation, scalar/raster brush references, append/replay equivalence, actual exported pixels, undo invalidation, source mapping through optical/projective geometry, native round trips, rejected unsafe XML and avoided cache work. Browser tests use actual pointer capture, keyboard input, file pickers, downloads and reload. Opt-in diagnostics show curve values and brush counts but omit brush coordinates; normal sessions do not periodically serialize diagnostic trees.

External format/workflow references: [Adobe XMP documentation](https://developer.adobe.com/xmp/docs/), [XMP Toolkit SDK](https://github.com/adobe/XMP-Toolkit-SDK), and [Lightroom metadata workflow](https://helpx.adobe.com/lightroom-classic/desktop/organize-photos-in-lightroom-classic/metadata-basics-actions.html). These references describe external contracts; they do not certify complete implementation by LightSpace.
