# Continuous crop constraints and composition guides

LightSpace 0.9 completes the crop interaction beyond centered ratio presets. The numerical processing model and catalog format remain unchanged: the result is still an ordinary normalized crop applied after optical/projective correction and before final quarter-turns/flips.

## Workflow

Choose Crop or press R. Original restores the complete frame; 1:1, 4:5, 4:3, 3:2 and 16:9 apply a ratio and enable the lock. Enter positive custom width/height values and choose Set, or press Enter in either ratio field. Invalid, non-finite and geometrically impossible ratios produce an error without modifying the photo. Ratio inputs accept the current locale with invariant numeric parsing as a fallback.

A toggles the lock without creating a photo edit. With locking enabled, corner and edge drags retain the current crop ratio. An unlocked Shift-drag temporarily retains the ratio captured at pointer-down without changing the lock toggle. Alt/Option-drag resizes about the crop center. Modifier policy is captured at gesture start; changing modifier keys midway does not reinterpret earlier movement.

Corners keep their opposite corner fixed. A horizontal edge keeps the opposite edge and vertical center fixed; vertical edges do the corresponding operation. Centered resizing retains both center coordinates. Dragging the interior translates the existing rectangle rather than resizing it. Bounds are clamped at the image frame and the pre-existing approximately one-percent minimum extents. Crossing an anchor clamps rather than silently flipping the handle. Drag outside the crop to create another rectangle; a click without meaningful movement leaves the crop intact.

The requested ratio describes final output width/height. Quarter turns are taken into account when converting it to the unrotated normalized frame, so choosing 7:5 after rotation still exports 7:5, subject to integer pixel rounding. X or Swap W/H inverts the current output ratio without rotating the image or changing flags. Swapping preserves area where feasible and moves the center only as required by frame boundaries. A quarter-turn command itself swaps the existing output orientation; locking then preserves that new ratio.

Arrow keys nudge the crop by one source-image pixel in the corrected frame; Shift increases this to ten. Enter outside text inputs leaves Crop for Edit. Escape cancels a captured gesture. Text fields retain normal input and selection shortcuts. Lock state follows the current crop, not an independently persisted numeric preference, and applies to the next gesture on the active photograph.

## Guides

O cycles Thirds, Grid, Golden ratio, Diagonals, Triangle and None. Shift+O reverses the triangle orientation. The same controls are available in the inspector. Guides are clipped to the crop and corrected image frame. They never enter the image-processing state, export pixels, named versions, XMP or recovery.

`CropGuides.Write` writes at most sixteen line segments into a caller-supplied Span. Grid uses nine divisions. Golden-ratio lines use the two interior golden-section positions; diagonal guides use 45-degree corner segments in view pixels. Triangle auxiliaries meet its diagonal perpendicularly. A golden spiral, selectable print-aspect overlays and a configurable guide cycle are not included.

## Transactions and coordinate lifetime

A drag captures its source identity, initial bounds, handle, modifier policy, pointer ID and interaction frame. Evaluation always uses that opening snapshot rather than successively editing the last result, avoiding accumulated movement error. Each successful drag commits one transaction; cancellation restores the complete opening state with no new history revision. Another pointer cannot release the captured crop gesture.

The viewport recomputes the interaction frame from arranged dimensions before pointer-down rather than relying on the most recently painted frame. During capture it observes root size, display scale, visibility, surface size and root-relative origin. Changes cancel and release the gesture instead of applying old-frame coordinates to a moved/resized view. Listeners are attached only during capture and removed on release, cancellation or disposal. Pointer release checks that evaluation did not cancel before committing.

The previous resize regression issued pointer-up immediately after the browser acknowledged viewport emulation. The retained trace shows those protocol calls only milliseconds apart; that does not establish that Uno processed its deferred root/layout notification before release. The test now holds capture until the application reports cancellation and new arranged geometry, then releases. It still requires exact restoration of crop bounds and unchanged revision. Additional actual-input tests cover A-cancellation and workspace focus-layout changes followed by a successful new drag.

## Reusable API

`CropGeometry`, `CropGesture`, `CropHandle`, `CropGuide`, `CropGuideLine` and `CropGuides` are UI-independent Core types. `CropAspectEditor` is a reusable Uno control; it emits ratio, lock, swap and guide requests rather than owning a session. Its SetContext method updates values without reconstructing captured controls. PhotoViewport exposes the corresponding tool operations and CropToolChanged event.

```csharp
using LightSpace.Core;

var crop = CropGeometry.WithAspect(new CropSettings(), 16d / 9, 6000, 4000);
var gesture = new CropGesture(crop, CropHandle.BottomRight,
    new PointD(crop.Right, crop.Bottom), 6000, 4000,
    aspectLocked: true, fromCenter: false);

RectD next = gesture.Evaluate(new PointD(.8, .7));
crop = CropGeometry.Apply(crop, next);

Span<CropGuideLine> lines = stackalloc CropGuideLine[CropGuides.MaximumLines];
int count = CropGuides.Write(CropGuide.Thirds, new RectD(0, 0, 900, 600), false, lines);
```

The host applies the resulting immutable crop through its ordinary preview/commit/cancel policy. Source width/height are required to interpret the requested output ratio and to project corner motion in image-pixel distance rather than normalized-square distance. Malformed public inputs are validated; non-finite gesture coordinates retain the opening bounds.

## Performance and verification

The engine checks anchors, clamping, center policy, all handles, creation quadrants, output orientation, no-op identity, invalid inputs, undo, finite guides and perpendicular triangle geometry. One warmed benchmark evaluates a locked crop and writes its grid lines 100,000 times with caller-owned storage; it reports managed allocation and CPU time in artifacts/engine/crop-performance.json. Its zero-allocation assertion covers that analytic loop only, not UI events, immutable photo snapshots or GPU work.

Crop-only changes retain decoded source images, development shaders, optical presentation and curve tables. Browser tests compare those counters during real corner/edge drags, verify one transaction per gesture, export a rotated custom crop to check encoded dimensions, and reload recovery to check final bounds. Guide changes leave processing and revision unchanged. A shared immutable full-frame crop avoids repeated default-record allocation in viewport frame calculations.

Rendering remains direct Skia drawing within Uno. Guides add bounded line drawing; they do not require a new shader, image decode or CPU-rendered display intermediate. CPU geometry evaluation is not a GPU benchmark. The existing fatal-slider, mask, optical, Survey, reference, copy, recovery and package-audit tests remain enabled. Chromium/SwiftShader and desktop compilation do not certify every GPU, pen or browser engine.

## Compatibility and boundaries

Catalog schema 6, native XMP settings 5, recovery manifest 1 and IndexedDB 2 remain unchanged. Crop bounds persist; lock state, guide choice, orientation toggle and custom input fields are session-local tool state. A fresh workspace starts unlocked. Existing catalogs and virtual-copy families need no additional migration. Reload the app without clearing site data.

This is an independent crop implementation, not full Lightroom UI or processing parity. Automatic/guided Upright, maximum-area constrained-crop optimization, calibrated profiles, RAW/AI, native-resolution tiling and the remaining feature-ledger items are separate work. Existing one-percent crop minimums, bounded previews and output limits continue to apply.
