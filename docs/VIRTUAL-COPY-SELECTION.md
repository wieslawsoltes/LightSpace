# Safe selection after copy removal

Virtual-copy removal retains selected surviving photographs. When the removed copy was active and selected survivors remain, the first survivor in catalog order becomes the displayed active photograph. When no selected photograph remains, its original becomes active and selected. Removing an inactive copy does not move the existing active photograph. Undo/redo restore the corresponding selection and active identity.

This rule closes a multi-selection handoff defect: previously the original could become active while another copy remained the only selected record. The next toolbar rating or flag command would then change a hidden selected photo rather than the displayed one. The corrected operation keeps the view and the surviving edit targets aligned without implicitly selecting another original.

Three engine regressions exercise the surviving-target choice, exact undo/redo restoration and inactive-copy removal. A real-browser regression creates two copies, removes the active one through its confirmation dialog, rates the displayed survivor, exports a catalog to verify every original's metadata and bytes remain intact, then checks undo/redo and recovery reload.

The copy-kind query enum is `PhotoKindFilter` (`All`, `Originals`, `VirtualCopies`); use it through `PhotoQuery.Kind`. This filter is separate from selection and does not mutate photos or remove records. Copy family identity uses catalog schema 6; native XMP settings and recovery formats remain unchanged.
