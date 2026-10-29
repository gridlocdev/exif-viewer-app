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
| TIFF, DNG, CR2, NEF, ARW, ORF, RW2, PEF | the TIFF structure: IFDs, Exif, GPS, Interop, SubIFDs, embedded JPEG previews |

Not yet: HEIC/AVIF/CR3/MP4 (ISO base media boxes), IPTC, ICC profiles, maker notes.

The preview is the file itself when the platform decoder handles it (JPEG, PNG, WebP), otherwise
the largest embedded JPEG (EXIF thumbnail or RAW preview), rotated by the EXIF orientation.

## Develop

Needs the .NET 10 SDK and [just](https://github.com/casey/just).

```bash
just run            # open the app
just run photo.jpg  # open a file
just test           # parser tests
just publish osx-arm64
```

## Layout

- `src/ExifViewer.Core/` – `MetadataReader` detects the container; `Containers/` reads JPEG, PNG
  and WebP; `Tiff/` walks IFDs and formats tag values; `Xmp/` flattens XMP packets.
- `src/ExifViewer.App/` – the Avalonia window (code-behind, no MVVM framework).
- `tests/ExifViewer.Core.Tests/` – fixtures are built in code (`Fixtures.cs`), so the repo has no sample photos.
