# LightSpace 0.5.0-alpha.1

Adds GPU-composable manual optical correction and independent projective framing to the shared Uno photography workspace. New tools include distortion, lens falloff and channel-alignment sliders; vertical/horizontal perspective, rotation, aspect, scale/offsets and constrained framing; on-canvas horizon straightening; a source-based relative white-balance picker; an interactive five-region histogram and clipping indicators; and resizable side panels with focus/filmstrip controls.

Geometry edits reuse development/optical shaders, source decodes, curve lookups and brush coverage. Source hit testing follows inverse optics, geometry, crop and orientation without image readback. Optical effects evaluate the existing development shader directly rather than materializing a CPU intermediate. The 0.4.1 shader-lifetime crash repair and all-slider stress regression remain enabled.

Catalog schema 5 and native XMP settings version 5 preserve the new corrections. Catalogs 1–4 and native XMP versions 3–4 migrate with neutral defaults. Recovery manifest format 1 and IndexedDB version 2 are unchanged; do not clear site data to upgrade. Older application builds reject new settings, so retain portable pre-upgrade backups when needed.

The existing single-file desktop packaging, six runtime identifiers, SourceLink/symbols, and tag-based NuGet Trusted Publishing workflows are preserved. Build artifacts are not themselves proof of a published NuGet version or signed native release.

These are original manual algorithms and additional Lightroom-style workflows, not full Adobe processing or UI parity. RAW/AI, calibrated camera/lens profiles, automatic Upright, HDR/panorama, native-resolution tiling, printing/proofing and cloud synchronization remain unimplemented. GPU execution depends on the Uno host; decode, histogram, brush texture preparation and export still include CPU/raster work. See OPTICS-GEOMETRY.md, PERFORMANCE.md and FEATURE-COVERAGE.md.
