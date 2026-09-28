# Color grading and local masks

## Four-way color grading

Use **Grade** beside Auto and B&W, or expand **Color grading** in Edit. Choose Shadows, Mids, Highs or Global. Drag the custom wheel to set hue and saturation, or enter numeric slider values. Luminance adjusts the selected tonal range. Blending changes the overlap between ranges; Balance shifts their weighting. Reset range clears only the active range.

The wheel supports keyboard editing: left/right change hue, up/down change saturation, Home resets hue/saturation, and Escape cancels an active pointer gesture. Double-click resets color while retaining the range's luminance. A completed gesture is one undo transaction. Grading is included in copied/synchronized settings, named versions, recovery and catalog backups.

The processing model is original LightSpace code. Grading follows monochrome conversion, allowing tinted black-and-white looks. Shadows/midtones/highlights use normalized smooth tonal weights, and Global applies across the image. The tint is a zero-luminance linear-sRGB color vector; luminance uses a relative exposure multiplier. Hue is degrees, saturation is percent, and the luminance slider is a relative adjustment—not calibrated scene luminance. This is not Adobe's grading algorithm or a claim of identical output.

## Rotatable gradients

Choose Masking, then Radial or Linear. Drag on the photograph to create a gradient. A linear mask follows the direction of the drag rather than being restricted to vertical orientation. Its three parallel guides indicate the transition. Drag the center pin to move it; drag either transition handle to rotate and change the fade distance.

A radial gradient has center, radius and rotation handles. Rotation uses image-pixel geometry, including the source aspect ratio, rather than treating normalized X and Y as equal pixel distances. Crop, flips and quarter-turn display transforms are inverted when interpreting the pointer.

Shift-drag starts another gradient over an existing pin. Up to eight masks are supported; existing handles remain editable at that limit. Escape restores the state at gesture start. A completed handle drag creates one undo transaction.

## Luminance selection

**Luminance** creates a mask based on source brightness without spatial restriction. Range minimum and maximum define the selected interval, and Range smoothness feathers the interval's outside shoulders. For radial/linear masks, **Restrict luminance** intersects the spatial mask with that range before inversion.

Brightness is the Rec.709-weighted luminance of the imported, orientation-normalized, sRGB-encoded source, before clone stamps and development. It is not scene-linear RAW luminance, exposure values, or Adobe's range-mask computation. Keeping the selection tied to the source prevents local exposure changes from feeding back into their own range selection.

Inversion complements the combined spatial/range selection. Amount scales its opacity; disabling a mask gives zero coverage without deleting settings. Exposure, contrast, temperature, tint and saturation are applied locally. Feather controls radial edges; linear feathering is controlled by transition handles.

## Inspect and manage masks

Outline toggles editing guides. Coverage shows the selected mask's weighted coverage as a red overlay, including range restrictions and amount. Coverage is a viewport aid only: rendered exports and histogram samples exclude it.

Select a mask in the panel or by its pin. Rename, Duplicate and Delete operate on the selected mask and support undo. Duplicate uses the current edited settings and assigns a new mask identity. Mask changes remain non-destructive and never replace original bytes.

This release does not implement brush masks, color-range masks, AI subject/sky/object selection, arbitrary mask boolean composition, healing or generative removal. The existing clone tool remains a separate feathered source-stamp tool.

## Catalog compatibility

Catalog schema **2** records grading and extended mask settings. Schema-1 catalogs open through a migration that supplies neutral defaults for new parameters. New saves use schema 2 so older LightSpace versions reject an unsupported catalog rather than silently drop these edits. Retain a pre-upgrade catalog backup when interoperability with older builds matters.

## Component API

`ColorWheel`, `ColorGradingEditor`, `ColorMixerEditor`, `MaskSettingsEditor` and `PhotoCard` are reusable Uno controls. The grading/mask editors expose values and Previewed/Committed/Canceled events; the application host owns the edit transaction.

```csharp
var editor = new ColorGradingEditor { Value = session.Active!.State.Develop.Grading };
editor.Previewed += grading => session.Preview(state => state with
{
    Develop = state.Develop with { Grading = grading }
});
editor.Committed += () => session.CommitGesture("Color grading");
editor.Canceled += session.CancelGesture;
```

Hosts must also refresh the editor's Value after external changes such as undo, loading another photograph or applying a preset. Renderer objects remain owner-thread-confined and must be disposed.
