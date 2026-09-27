# Third-party notices

LightSpace's source code is MIT licensed. Dependencies and optional assets retain their own licenses; the application license does not replace those terms.

| Component | License / source | Use |
| --- | --- | --- |
| Uno Platform | Apache-2.0 — https://github.com/unoplatform/uno | Shared UI and native/browser hosting |
| SkiaSharp | MIT — https://github.com/mono/SkiaSharp | Managed graphics, codec and shader API |
| Skia | BSD-style — https://skia.googlesource.com/skia/+/main/LICENSE | Native rendering and codec implementation; transitive notices are retained in native packages |
| Noto Sans | SIL Open Font License 1.1 — https://github.com/google/fonts/tree/main/ofl/notosans | Cross-platform application typography |
| Playwright | Apache-2.0 — https://github.com/microsoft/playwright | Development-time browser acceptance testing |
| Optional demonstration photos | Unsplash License — https://unsplash.com/license | Sample catalog photographs, not application code |

The build verifies the Noto Sans font and its OFL text against pinned SHA-256 hashes. The resulting application bundle contains the font's license text. `scripts/fetch-assets.py` records each downloaded demonstration image's source URL and actual SHA-256 in `artifacts/demo-assets.json`. Those photos are optional, are not MIT licensed, and are not fetched from user sessions. They must not be redistributed as a competing image library or sold as unmodified images. The alternative generated landscape samples are original LightSpace code-generated artwork.

Optional photo source identifiers:

- `photo-1464822759023-fed622ff2c3b` — alpine sample
- `photo-1470770841072-f978cf4d019e` — lake sample
- `photo-1501785888041-af3ef285b470` — shores sample
- `photo-1472396961693-142e6e269027` — wild-country sample
- `photo-1469474968028-56623f02e42e` — valley sample
- `photo-1441974231531-c6227db76b6e` — forest sample

These labels are LightSpace catalog labels, not claims about the photographer, camera or exact location. Image source URLs are assembled explicitly in the asset script and included in the build manifest.

No Adobe artwork, icons, fonts, source code, camera profiles or proprietary decoding libraries are bundled. Lightroom is a comparative product reference, not a dependency or affiliation.
