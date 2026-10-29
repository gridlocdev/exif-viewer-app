# Developer guide

EXIF Viewer is C# on .NET 10, with an [Avalonia](https://avaloniaui.net/) 12 UI. The rules in
`CLAUDE.md` at the repository root apply to everyone working on it; the most important is
**keep dependencies minimal**.

## Layout

```
src/ExifViewer.Core/        the parser: no package references at all
  MetadataReader.cs         entry point: detects the container, picks a reader
  Model.cs                  MetadataReport, MetadataDirectory, MetadataTag, GeoPosition, Rational
  ReportBuilder.cs          collects groups, warnings and the preview while readers run
  Binary/EndianReader.cs    bounds-checked big/little-endian reads
  Containers/               JPEG, PNG, WebP, HEIF, QuickTime/MP4 readers; IsoMedia (box walking)
  Tiff/                     IFD walker (TiffReader), tag names, value formatting
  Xmp/                      XMP packet flattening
src/ExifViewer.App/         Avalonia window (code-behind), Quick Look preview, map links
tests/ExifViewer.Core.Tests xUnit v3 tests; fixtures are built in code
scripts/                    packaging for macOS, Windows, Linux
docs/                       these pages
```

## How a file is read

1. `MetadataReader.Read(path)` looks at the first bytes. Movies (ISO media that isn't HEIF) are
   loaded **without their media data** (`IsoMedia.LoadWithoutMediaData`); everything else is read
   whole.
2. It picks a reader by signature: JPEG, PNG, WebP, TIFF (including TIFF-based RAW), HEIF, or
   QuickTime/MP4.
3. Container readers find embedded blocks and hand them on. An EXIF block goes to `TiffReader`,
   an XMP packet to `XmpReader`.
4. Every value goes into a named group through `ReportBuilder`, as display text. Embedded JPEGs
   are offered as preview candidates; the largest one that's displayable wins.
5. `ReportBuilder.Build` returns a `MetadataReport`. The UI only displays it.

### Rules for parsers

- **Never throw on bad input.** `EndianReader` throws `MalformedDataException` on any out-of-range
  read. Readers catch it at the level where they can carry on, call `report.Warn(...)`, and keep
  what they already read.
- **Guard against loops and bombs.** `TiffReader` tracks visited IFD offsets; PNG inflation is
  capped; XML DTDs are prohibited.
- **Format culture-invariantly**, so output (and tests) don't depend on the machine's locale.
- Keep readers independent of absolute media offsets inside `mdat`, since movies are loaded without it.

## Adding a tag

1. Add its name to the right table in `Tiff/TagNames.cs` (TIFF/Exif, GPS or Interop numbering).
2. If the raw value needs translating (units, enum names, bit fields), add a case in
   `Tiff/TagFormatter.cs`. Everything else falls back to a generic rendering of its type.
3. Add a test.

## Adding a format

1. Add a reader under `Containers/` with a `HasHeader` check and a `Read(data, report)` method.
2. Register both in `MetadataReader` (detection and `DescribeFormat`).
3. Add a fixture builder and tests. See `IsoFixtures.cs` for an example.
4. Update [Supported formats](Supported-Formats.md), [Metadata groups](Metadata-Groups.md) and
   the README's format table.

## Tests

```bash
just test
```

Fixtures are **built in code**: `TiffBuilder` and `Ifd` lay out TIFF/EXIF blocks, `Containers`
wraps them in JPEG/PNG/WebP, and `Iso`/`IsoFixtures` build HEIC and MOV files box by box. Don't
commit sample photos; they bloat the repository and may contain personal data. To check against
real files, use files you made yourself, as shown in the commit history.

## Building and releasing

| Command | Does |
|---|---|
| `just run [file]` | Debug build and run |
| `just build-macos` / `build-macos-universal` | `dist/EXIF Viewer.app` |
| `just build-windows` | `dist/exif-viewer-<version>-win-x64.zip` |
| `just build-linux` | `dist/exif-viewer-<version>-linux-x64.tar.gz` |

- **CI** (`.github/workflows/ci.yml`) builds and tests on macOS, Windows and Linux for pushes to
  `main` and pull requests.
- **Releases** (`.github/workflows/release.yml`): bump `<Version>` in
  `src/ExifViewer.App/ExifViewer.App.csproj`, then push a tag such as `v0.2.0`. The workflow runs
  CI, packages all three platforms and publishes a GitHub release. A tag containing `-`
  (`v0.2.0-beta.1`) becomes a pre-release.
- macOS builds are ad-hoc signed; set `MAC_SIGN_IDENTITY` to sign with a Developer ID (then
  notarise). Windows builds are unsigned.

## Documentation

These pages live in `docs/` as plain Markdown: `Home.md` is the start page and `_Sidebar.md` the
navigation. Pages link to each other with relative `.md` links, so they read correctly on GitHub
and in any editor. When the app's behaviour changes, update the page that describes it in the
same commit.
