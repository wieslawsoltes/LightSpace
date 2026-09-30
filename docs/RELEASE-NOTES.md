# LightSpace 0.9.0-alpha.1

Adds continuous aspect-locked crop interaction with fixed or custom output ratios, rotation-aware conversion, anchored corner/edge resizing, centered Alt/Option resizing, temporary Shift locking, orientation swap and pixel nudging. A toggles locking, X swaps width/height, O cycles composition guides and Shift+O reverses the triangle guide. Enter completes Crop; captured Escape cancels without creating history.

Composition guides are bounded, caller-buffered geometry drawn directly on the Uno Skia canvas and excluded from exports and persistence. Crop-only adjustments reuse decoded sources and processing shaders. The analytic benchmark measures 100,000 evaluations with no managed allocations in the warmed geometry/guide loop; it is not whole-application or physical-GPU timing.

Capture now observes root/layout/source changes, releases listeners deterministically and cannot commit after an invalidating move has canceled. The resize test waits for actual Uno cancellation rather than racing pointer-up against browser viewport emulation. Real-pointer regressions cover ratios, anchors, temporary/centered policies, guides, dimensions, undo/cancel, keyboard and layout recovery. The full fatal-slider regression remains enabled.

Catalog schema 6, native XMP settings 5, recovery manifest 1 and IndexedDB 2 are unchanged. Tool lock/guide state is session-local; final crop bounds persist. Reload to update and retain site data. Source versioning does not create a release tag or NuGet.org publication. See CROP-CONSTRAINTS.md for the API, precise semantics and remaining boundaries.
