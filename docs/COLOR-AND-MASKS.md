# Color grading and local masks

## Four-way color grading

Use **Grade** beside Auto and B&W, or expand **Color grading** in Edit. Choose Shadows, Mids, Highs or Global. Drag the custom wheel to set hue and saturation, or enter numeric values. Luminance adjusts the selected tonal range. Blending changes range overlap; Balance shifts their weighting. Reset range clears only the active range.

The wheel supports keyboard editing: left/right change hue, up/down change saturation, Home resets color, and Escape cancels an active pointer gesture. Double-click resets color while retaining luminance. Each completed gesture is one undo transaction. Grading is included in copied/synchronized settings, versions, recovery, catalog backups and the native XMP extension.

The processing model is original LightSpace code. Grading follows monochrome conversion, allowing tinted black-and-white looks. Shadows/midtones/highlights use normalized smooth weights; Global applies across the image. The tint is a zero-luminance linear-sRGB vector, while luminance uses a relative exposure multiplier. This is not Adobe's grading algorithm or a claim of identical output.

## Rotatable gradients

Choose Masking, then Radial or Linear. Drag on the photograph to create a gradient. A linear mask follows the drag direction. Three parallel guides show its transition; drag the center pin to move it or an endpoint handle to rotate/change fade distance.

A radial gradient has center, radius and rotation handles. Geometry is measured in image pixels with source-aspect correction. Display crop, flips and quarter turns are inverted for pointer input. Shift-drag starts another gradient over an existing pin. Up to eight masks are supported; existing handles remain editable at that limit. Escape cancels the active gesture without leaving the tool. Each completed handle drag is one undo transaction.

## Brush modifications

**New brush** creates a zero-coverage brush mask. **Paint selected** edits the current mask, including an existing gradient or range mask. Paint adds spatial coverage; erase subtracts it. Size, feather, flow, density and pressure are captured at stroke start. Alt temporarily erases, B selects the brush, and brackets adjust size.

Strokes are resampled by arc length and stored non-destructively. The renderer incrementally updates cached coverage for appended dabs. Current coverage rasterization is bounded to a 1024-pixel long edge, including export; this is not native-resolution brush processing for larger sources. See [brush semantics, limits and reusable APIs](ADVANCED-EDITING.md#freehand-brush-masks).

## Luminance selection

**Luminance** creates a mask based on source brightness. Range minimum/maximum select the interval, and Range smoothness feathers its outside shoulders. **Restrict luminance** intersects a gradient or brush mask's spatial coverage with the selected range before inversion and amount.

Brightness is the Rec.709-weighted luminance of imported, orientation-normalized, sRGB-encoded source pixels before clone stamps and development. It is not scene-linear RAW luminance or Adobe's range-mask algorithm. Source-based selection prevents local exposure changes from feeding back into their own coverage.

Inversion complements combined spatial/range coverage. Amount scales it; disabling a mask preserves its settings with zero coverage. Local exposure, contrast, temperature, tint and saturation operate only on selected pixels. Feather controls radial edges; linear feathering uses the transition handles; brush feathering belongs to each stroke.

## Inspect and manage

Outline controls guides; Coverage shows the selected mask's weighted coverage as a red preview overlay, including range and amount. Neither exported images nor histogram samples include that overlay. Select a mask in the panel or by a gradient pin. Rename, Duplicate and Delete are undoable; duplication uses current settings and a new mask identity.

The application does not implement AI subject/sky/object selection, arbitrary mask-group boolean graphs, healing or generative removal. Cloning remains a separate feathered source-stamp tool.

## Curves, XMP and catalog compatibility

Arbitrary master/R/G/B point curves and brush masks were added in 0.3. See [advanced editing and XMP](ADVANCED-EDITING.md) for processing order, precision, source formats and tested boundaries.

New catalogs use schema **4**. Schemas 1–3 migrate with neutral defaults for missing new parameters; unsupported future schemas are rejected. Older LightSpace builds reject schema 4 rather than silently discard brush/curve edits. Keep a pre-upgrade backup for interoperability with old applications.

## Reusable controls

`ColorWheel`, `ColorGradingEditor`, `ColorMixerEditor`, `ColorRangeEditor`, `MaskSettingsEditor`, `PointCurveEditor`, `BrushSettingsEditor` and `PhotoCard` are reusable Uno controls. Value editors expose preview/commit/cancel contracts; the host owns transactions and refreshes Value after external changes.

```csharp
var editor = new ColorGradingEditor { Value = session.Active!.State.Develop.Grading };
editor.Previewed += grading => session.Preview(state => state with
{
    Develop = state.Develop with { Grading = grading }
});
editor.Committed += () => session.CommitGesture("Color grading");
editor.Canceled += session.CancelGesture;
```

Renderer and cache objects are owner-thread-confined and disposable. Snapshot arrays and original byte buffers are immutable by contract.

## Sampled color ranges (0.4)

Choose **Color range** in Masking to create a selection. Click the photograph to replace the selected colors, Shift-click to add a sample, or Alt-click a sample pin to remove it. Up to five samples are supported. The swatches also remove individual samples. **Sample** on an existing mask enables a color restriction without replacing its analytic or brush geometry.

Picking reads a small patch of the retained orientation-normalized source preview. The default footprint is 5×5 pixels (smaller at image edges); transparent samples are ignored and partially transparent pixels are alpha-weighted. The viewport's inverse crop/quarter-turn/flip transform locates the source position. Development and the red coverage overlay never enter the sampled color. This is not a native-resolution or calibrated RAW eyedropper; preview downsampling can affect small-detail samples.

Colors are converted from transfer-encoded sRGB to linear sRGB and then Oklab. Selection is the union of neighborhoods around the sampled colors: full coverage within Tolerance, a smooth transition over Smoothness, and zero coverage outside that transition. Both controls display 100 times the Oklab-distance parameter; they are not CIE Delta-E values or Adobe Refine units. The conversion follows [Björn Ottosson's published Oklab matrices](https://bottosson.github.io/posts/oklab/); the selection model is original LightSpace code.

Color coverage intersects analytic/brush coverage and any luminance restriction **before inversion**, then mask amount is applied. The source color is evaluated before clone stamps or development, preventing a local correction from feeding back into its own selection. An enabled color range with no samples selects nothing; an inverted empty range selects everything, following that explicit inversion rule.

Sample Oklab coordinates are precomputed on the CPU; the shader computes source Oklab once per pixel when an active color range exists. No color-range conversion runs when all ranges are inactive. Adjusting tolerance does not decode the image or rebuild curve/brush textures. Moving a same-color sample pin updates catalog metadata without changing pixel equality.

Samples, restrictions and their positions participate in undo/redo, versions, copy/synchronization, native XMP and recovery. New catalog schema 4 and native XMP settings version 4 prevent older readers from silently dropping these values. Native XMP version 3 is still accepted. Ordinary Adobe XMP masking structures are not converted into equivalent LightSpace color selections.

No drag-region sampler, automatic object/sky/subject selection or arbitrary mask-group boolean graph is included. The existing add/erase brush operations and spatial/luminance intersections remain available.
