# RGB curves, brush masks and XMP sidecars

## RGB point curves

Choose **RGB curves** from Edit. RGB is the master transfer curve; Red, Green and Blue are independent channel curves. Click the graph to add a point, drag it, or enter Input/Output values. Delete removes an interior point; Reset clears the selected channel. Endpoints keep input coordinates 0 and 255 but their output can change. Each channel supports up to 32 points.

Smooth uses shape-preserving cubic Hermite interpolation with limited endpoint slopes and harmonic interior slopes where adjacent secants agree. Linear interpolates directly. Arrow keys move the selected point in 1/255 steps. Escape cancels a captured pointer gesture; release commits one undo transaction.

Processing order is the legacy five-point curve, master point curve, then the independent RGB curves. Keeping the legacy stage preserves existing presets. Values are normalized sRGB-encoded components, not scene-referred RAW values. The interpolation is original code, not Adobe's curve implementation.

`PointCurve`, `ChannelCurves` and `CompiledPointCurve` are UI-independent. Compiled evaluation is allocation-free. The renderer composes curves into a 2048-entry RGBA-float lookup with up to eight cached tables. Exposure, mask parameters and metadata reuse the table; a curve edit changes it. Linear texture sampling approximates the analytic curve between entries. Image exports remain 8-bit sRGB.

```csharp
session.Edit("Contrast curve", state => state with
{
    Develop = state.Develop with
    {
        Channels = state.Develop.Channels with
        {
            Master = new PointCurve
            {
                Interpolation = CurveInterpolation.Smooth,
                Points = [new(0, 0), new(.25f, .16f), new(.75f, .84f), new(1, 1)]
            }
        }
    }
});
```

`PointCurveEditor` exposes Value/Previewed/Committed/Canceled. Its host owns transactions and refreshes Value after external changes. The workbench retains it during an active gesture.

## Freehand brush masks

Choose Brush, press B, or use New brush / Paint selected in Masking. A new brush mask starts at zero spatial coverage. On an existing gradient/range mask, paint adds spatial coverage and erase subtracts it. Luminance/color restrictions, inversion and amount are applied afterward.

Brush settings include size, feather, flow, density and supplied pen pressure. Alt temporarily erases; bracket keys change size. Settings are captured at stroke start, not applied retroactively. Mouse input uses full pressure. Physical pen hardware is not certified by headless tests.

Pointer input is transformed back through crop, flips and quarter turns to source-normalized coordinates. Radius is relative to source height with aspect correction. Arc-length resampling uses one quarter of the base radius, avoiding pointer-event-density-dependent strokes. Escape or lost capture cancels a stroke; release creates one undo transaction.

Each dab contains X/Y and pressure. A stroke accumulates `amount += (density - amount) * flow * pressure * falloff`; radial feather uses a smooth transition. Paint adds `(1 - coverage) * amount`; erase multiplies coverage by `1 - amount`. Incremental coverage stores an affine transform on analytic coverage:

```text
coverage = multiplier * analyticCoverage + bias
```

Append-only growth processes new dabs inside their raster bounds instead of replaying earlier dabs. Undo or changed earlier strokes require replay. Local tonal adjustments reuse geometry. Coefficient texture publication still produces an immutable 8-bit texture; CPU accumulation is floating-point, and this is not a sparse GPU update.

Coverage has a **1024-pixel maximum long edge, including export**. Fine edges can differ from a native-resolution reference. Limits are eight masks, 64 strokes per mask, 4096 dabs per stroke and 65,536 total dabs per mask. Replay is limited to 200 million visited pixels. Budgets bound retained coverage, not all scratch/GPU allocations.

`BrushStrokeBuilder`, `BrushStroke`, `BrushDab`, `BrushCoverageCache` and `BrushSettingsEditor` are reusable. Arrays are immutable by contract. Rendering/cache objects are owner-thread-confined and disposable. Statistics count work; they are not GPU timings.

## XMP interchange

Photo information / XMP provides Import XMP and Export XMP. Import presents applied-field and compatibility reports before **Apply**. Metadata only preserves processing; Prefer embedded LightSpace settings selects the native extension when present. Applying is one undoable edit on the reviewed photo; changing that photo during review prevents application to the wrong target.

Standard metadata includes `xmp:Rating`, `xmp:Label`, `dc:description` and `dc:subject`. Captions prefer `x-default`; keywords accept RDF Bag/Seq. Rating -1 maps to rejection. Namespace URIs—not prefix spelling—identify properties. Mapped scalar fields accept attributes and simple elements. This is not a complete EXIF/IPTC editor.

The Camera Raw subset maps Exposure2012, Contrast2012, Highlights2012, Shadows2012, Whites2012, Blacks2012, Vibrance, Saturation, Texture, Clarity2012, Dehaze, Sharpness, LuminanceSmoothing, GrainAmount, ConvertToGrayscale and the four ToneCurvePV2012 sequences. Unsupported fields are reported. Camera Kelvin white balance, lens profiles, Adobe AI/mask serialization, crop/grading and proprietary processing versions are not imported as equivalent edits. Matching parameter numbers do not imply matching Adobe-rendered pixels.

Export writes a separate UTF-8 sidecar without modifying original images. The supported curve subset quantizes coordinates to 0–255; a non-neutral legacy/master combination is sampled to at most 32 points. Metadata-only export omits processing. The optional LightSpace namespace carries complete normalized settings, including brushes, sampled color ranges, curves, crop and grading. Other software may ignore/remove it. Standard metadata is read after native settings so externally edited metadata can override stale embedded values.

Native extension version **4** preserves new color selections and accepts version 3. Full Adobe catalog/preset/profile compatibility is not claimed. Unknown third-party properties are not retained on re-export. Tests use namespace-correct synthetic packets and native round trips, not interactive Adobe-app certification.

Packets are bounded to 16 MiB and 64 XML nesting levels. DTDs/external resolution are disabled. Conflicting scalar properties and multiple RDF subjects are rejected. Invalid supported numbers are reported or explicitly clamped.

```csharp
var result = XmpSidecar.Import(xml, session.Active!.State,
    new XmpImportOptions(MetadataOnly: false, PreferLightSpaceSettings: true));
// Review result.Warnings before applying.
session.Edit("Import XMP", _ => result.State);
var sidecar = XmpSidecar.Export(session.Active.State);
```

`ISidecarStorage` is an optional host capability. A host without it receives an explicit unsupported-picker message.

## Catalog migration and validation

New catalogs use **schema 4**; schemas 1–3 migrate with neutral defaults. Older builds reject unsupported schemas rather than silently drop settings. Preserve pre-upgrade backups when using older readers. Browser IndexedDB version 2 is a separate storage contract; old builds requesting version 1 cannot open the upgraded database.

Tests cover curve interpolation/pixels, scalar-versus-raster brush coefficients, append/replay equivalence, undo/cancellation, bounded XML, namespaces, native round trips, shader/cache reuse and real browser input. [Color/range semantics](COLOR-AND-MASKS.md) · [Recovery](RECOVERY.md) · [Performance evidence](PERFORMANCE.md)

Primary external references: [Adobe XMP documentation](https://developer.adobe.com/xmp/docs/) and the [XMP Toolkit SDK](https://github.com/adobe/XMP-Toolkit-SDK). These define external formats; they do not certify complete LightSpace interoperability.
