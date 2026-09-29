# LightSpace 0.6.0-alpha.1

## Reference comparison

Pin a frozen edited reference beside the active photograph with Reference view or Shift+R. Switch side-by-side or stacked layout, link or unlink fit-relative pan/zoom, choose another active photo, pin a new reference, or apply selected groups from the reference. The reference is session-local, shares original bytes, creates no catalog record and does not change the committed revision merely by opening or navigating it.

Both panes draw through the existing composed development/optical Skia effects and projective matrix. Matching immutable source arrays share one decoded image, while each render identity retains its own shaders and geometry. Closing or replacing a reference releases only its processing identity. Forced-finalization and deferred-draw tests cover ownership across source sharing and eviction.

## Selective editing workflows

Copy, paste and synchronize thirteen independent processing groups. All/None/Global presets make selection explicit; Global excludes geometry, crop, masks and clone locations. Dialogs capture the source look and reviewed target identities. Application stages every target before changing any photo and creates one undoable transaction. Ratings, flags, labels, captions, keywords, original bytes and unselected edits are retained.

Batch application now uses already-resolved target objects rather than repeatedly scanning the catalog for each target. Undo/redo uses one identity lookup. New reusable APIs include EditSettingsTransfer, EditSettingsGroup, ReferencePhotoSnapshot, ReferencePhotoView, SettingsTransferEditor and PhotoNavigationState. RendererStatistics retains its original six-field constructor and deconstruction.

## Workspace and validation

Linked framing survives split-layout and window resizing. Shared dialogs fit short forms while long bodies scroll with reachable actions. In-app help and the [reference/settings guide](REFERENCE-AND-SYNC.md) document workflows, processing groups, ownership and limits.

The reviewed PR #13 build passed 209 engine checks and 41 browser tests, including frozen-reference pixel checks, selective exported values, metadata preservation, linked navigation, compact-dialog geometry, and the full 17-slider crash regression. Main-branch Build and Pages rerun the complete suite; use their commit-specific reports for deployment status. CPU/raster microbenchmarks and resource counters are not physical-GPU frame-rate measurements.

## Compatibility and scope

Catalog schema 5, native XMP settings 5, recovery manifest 1 and IndexedDB version 2 are unchanged. Reload the browser app to load new code; do not clear site data. References and the in-workspace settings clipboard are not persisted. The 0.4.1 shader-lifetime fix remains enabled.

SourceLink, symbol packages, six-RID single-file desktop packaging and the existing NuGet Trusted Publishing workflow remain intact. Updating the source version or building packages does not publish a tag, native release or NuGet version.

This is independent software, not full Lightroom parity. RAW/AI, calibrated camera/lens profiles, automatic Upright, HDR/panorama merging, native-resolution tiling, survey mode, virtual copies, printing/proofing and cloud synchronization remain unsupported. GPU execution depends on the host's Skia backend; decoding, histogram analysis and export retain CPU work. See [feature coverage](FEATURE-COVERAGE.md).
