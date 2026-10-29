# EXIF Viewer

A cross-platform desktop app (macOS, Windows, Linux) that shows the metadata inside photos:
EXIF, GPS, XMP and container details, with a preview of the image.

## Dependencies

Few third-party packages, on purpose:

- **ExifViewer.Core** (the parser): none, only the .NET base library.
- **ExifViewer.App** (the UI): [Avalonia](https://avaloniaui.net/) (`Avalonia`, `Avalonia.Desktop`, `Avalonia.Themes.Fluent`).
- **Tests**: xUnit v3, dev only.

Versions live in `Directory.Packages.props`.

## Formats

| Format | What's read |
|---|---|
| JPEG | EXIF (APP1), XMP, JFIF, comments, frame header |
| PNG | eXIf, tEXt/zTXt/iTXt (incl. XMP), IHDR, tIME, pHYs |
| WebP | EXIF, XMP, VP8X/VP8/VP8L headers |
| TIFF, DNG (incl. Apple ProRAW), CR2, NEF, ARW, ORF, RW2, PEF | the TIFF structure: IFDs, Exif, GPS, Interop, SubIFDs, embedded JPEG previews |
| HEIC, HEIF, AVIF | brands, primary image size/rotation/colour, item list, auxiliary images (HDR gain map, depth, mattes), Exif and XMP items |
| MOV, MP4, M4V | movie header, per-track codec/size/rotation/frame rate/audio format, Apple `mdta` keys (make, model, location, Live Photo content identifier), iTunes-style tags, QuickTime user data |

Movies are read without their media data, so multi-gigabyte videos open instantly.

Not yet: CR3, IPTC, ICC profile contents, maker notes (including Apple's).

The preview is the file itself when the platform decoder handles it (JPEG, PNG, WebP), otherwise
the largest embedded JPEG (EXIF thumbnail or RAW preview), rotated by the EXIF orientation.
HEIC and video frames are HEVC/H.264, which would need a codec library, so they have no preview.

## Develop

Needs the .NET 10 SDK and [just](https://github.com/casey/just).

```bash
just run            # open the app
just run photo.jpg  # open a file
just test           # parser tests
just publish osx-arm64
```

## Layout

- `src/ExifViewer.Core/` – `MetadataReader` detects the container; `Containers/` reads JPEG, PNG,
  WebP, HEIF and QuickTime/MP4 (`IsoMedia` walks the shared box structure); `Tiff/` walks IFDs and formats tag values; `Xmp/` flattens XMP packets.
- `src/ExifViewer.App/` – the Avalonia window (code-behind, no MVVM framework).
- `tests/ExifViewer.Core.Tests/` – fixtures are built in code (`Fixtures.cs`), so the repo has no sample photos.
