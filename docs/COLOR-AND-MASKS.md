# Color grading and local masks

## Four-way grading

Choose Grade beside Auto/B&W or expand Color grading. Shadows, Mids, Highs and Global each have hue, saturation and luminance controls. Drag the wheel or enter numeric values; Blending changes tonal overlap and Balance shifts weighting. Reset range clears the selected range. Double-click resets its color while retaining luminance.

Arrow keys change the focused wheel's hue/saturation, Home resets color and Escape cancels an active pointer gesture. One completed gesture is one transaction. Grading participates in copy/synchronization, versions, native XMP, catalog backup, recovery and image export.

The algorithm is an original LightSpace model. Grading follows monochrome conversion, allowing tinted black-and-white looks. Normalized smooth tonal weights combine zero-luminance linear-sRGB tint vectors and relative luminance multipliers. It is not Adobe's color-grading algorithm or identical output.

## Gradient geometry

Masking offers Radial and Linear tools. Drag on the photograph to create a gradient. Linear masks follow drag direction; three parallel guides show transition bounds. Move the center or drag an endpoint to rotate/change fade distance. Radial masks have center, radius and rotation handles.

Coordinates remain orientation-normalized source coordinates. Source aspect makes circles and angles consistent in pixels. Display mapping includes manual optical correction, projective geometry, crop, flips and quarter turns; pointer input reverses that mapping. Outlines and brush cursors follow the corrected image rather than assuming an unwarped rectangle. [Projection and optical mapping](OPTICS-GEOMETRY.md)

Shift-drag starts another gradient over a pin. Up to eight masks are supported. Escape cancels the current gesture and retains the tool. Each completed transform is one undo transaction.

## Brush modifications

New brush creates a zero-coverage mask. Paint selected adds or subtracts spatial coverage from the selected mask, including analytic gradients and range masks. Size, feather, flow, density and supplied pressure are captured at stroke start. Alt temporarily erases, B selects the brush and brackets change its size.

Distance resampling avoids dependence on pointer-event count. Incremental caching rasterizes appended dabs, while changing local exposure or color does not replay the brush geometry. Coverage is represented as an affine operation on analytic coverage and is capped at a 1024-pixel long edge, including export. This is not native-resolution rasterization for large originals. [Stroke semantics and limits](ADVANCED-EDITING.md#freehand-brush-masks)

## Luminance and color selection

Luminance creates a source-brightness range mask. Minimum/maximum select the range and Smoothness feathers the outside shoulders. Restrict luminance intersects an existing spatial selection before inversion and amount. Brightness uses Rec.709-weighted source sRGB values before clone/development, not scene-linear RAW luminance or Adobe's range algorithm.

Color range creates a selection from up to five source colors. Click replaces samples, Shift-click adds one, and Alt-click a pin or click a swatch to remove one. Sample on an existing mask applies color restriction without discarding its analytic/brush geometry. The swatch row retains its height so adding samples does not move pointer-targeted sliders.

Picking averages a bounded source-preview patch, normally 5×5 pixels. Fully transparent samples are rejected and partial alpha weights the average. Corrected display coordinates are mapped back to the source; development and overlays never enter the sample. Preview downsampling can affect fine detail, so this is not a calibrated RAW or full-resolution eyedropper.

Selection uses the union of Oklab neighborhoods around sample colors. Tolerance sets full-coverage distance; Smoothness fades to zero outside it. Displayed controls are 100 times the Oklab-distance parameter, not CIE Delta-E or Adobe Refine values. Linear-sRGB/Oklab conversion uses [Björn Ottosson's published matrices](https://bottosson.github.io/posts/oklab/), with original LightSpace selection logic.

The order is spatial/brush coverage, luminance restriction, color restriction, inversion, then amount. Source selection is evaluated before correction, preventing exposure changes from feeding back into their own selection. An enabled empty color range selects nothing; inversion complements it. No AI subject/sky/object selection, drag-region sampler or arbitrary mask-group boolean graph is included.

## Local adjustments and inspection

Each mask supports exposure, contrast, relative temperature/tint and saturation, plus enable, invert and amount. Feather affects radial edges; linear transitions use their handles and brush feather belongs to each stroke. Names, duplication and deletion are undoable; duplicates use current settings with a new identity.

Outline controls guides. Coverage shows the selected mask's weighted result in red, including ranges and amount. Exported pixels and histogram analysis never include it. Clipping triangles/J are a separate display aid and also remain outside catalog/export state.

Sample Oklab positions are prepared with uniforms. Source Oklab conversion runs once per pixel only when an active color range requires it. Changing tolerance reuses source decode, curve lookup and brush textures. Moving a same-color pin changes catalog metadata but not pixel equality.

## Reuse and compatibility

`ColorWheel`, `ColorGradingEditor`, `ColorMixerEditor`, `ColorRangeEditor`, `MaskSettingsEditor`, `PointCurveEditor`, `BrushSettingsEditor` and `PhotoCard` are reusable Uno controls. Editors expose preview/commit/cancel contracts; their host supplies transactions and refreshes values after external changes.

```csharp
var editor = new ColorGradingEditor { Value = session.Active!.State.Develop.Grading };
editor.Previewed += value => session.Preview(state => state with
{
    Develop = state.Develop with { Grading = value }
});
editor.Committed += () => session.CommitGesture("Color grading");
editor.Canceled += session.CancelGesture;
```

Current catalogs use schema **5** for geometry/optics in addition to previous masks/curves. Versions 1–4 migrate with neutral missing fields. Native XMP settings version 5 accepts 3–4. Older builds reject unsupported versions. Keep portable pre-upgrade backups when old-version compatibility matters; do not reset IndexedDB to load this release.

Photo snapshots and original arrays are immutable by contract. Renderer/cache objects are owner-thread-confined and disposable. Physical GPUs and pen hardware are not certified by CI's software-rendered browser checks.
