# LightSpace 0.8.0-alpha.1

Adds persistent virtual copies with independent processing and metadata while sharing original bytes and matching decoded previews. Copies can be created individually or for a selection, renamed, filtered, compared as a family in Survey, and removed without deleting originals. Create/rename/remove operations are undoable, with reversible record deltas retaining selection and album order rather than restoring an entire stale catalog snapshot.

Portable schema-6 catalogs keep the encoded payload on the original and store direct master references on copies. Loading validates families and hydrates shared arrays. Recovery reuses SHA-256 source blobs and rejects inconsistent family/source relations. Rename does not rebuild pixel shaders or rehash/rewrite stored originals. Rendered export names identify the copy.

Catalogs 1–5 migrate to schema 6. Native XMP settings stay at 5; recovery manifest/IndexedDB stay at 1/2. Older builds reject schema 6. Keep a portable pre-upgrade backup; do not clear site data. Set Copy As Master, automatic stacks and Lightroom lrcat interchange are not included.

The Build workflow now audits every package/symbol pair for the expected version, full source commit, internal dependency consistency, target-framework payloads and package metadata. Twelve fault-injection tests protect the gate. Packages and publication remain separate: a source version bump does not create a release tag or publish to NuGet.org.

The all-slider fatal-crash regression and existing photography, Survey, reference, XMP and recovery checks remain enabled. Full Lightroom UI/processing parity, RAW/AI, calibrated profiles, HDR/panorama, native-resolution tiling, printing/proofing and cloud workflows remain outside this release. See VIRTUAL-COPIES.md, FEATURE-COVERAGE.md and the machine-readable validation artifacts for precise behavior and measured scopes.
