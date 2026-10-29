# EXIF Viewer

See what's stored inside your photos and videos: when and where they were taken, which camera
and settings made them, which way up they go, and more. For macOS, Windows and Linux.

![EXIF Viewer on macOS showing a QuickTime video: preview on the left, grouped metadata with a Show on map link on the right](docs/images/screenshot.webp)

## What it does

- **Shows everything in one place**, grouped by where it came from: camera settings, dates,
  location, image size, editing software, and the file itself.
- **Makes it readable**: `1/250 s` instead of `0.004`, `f/2.8`, `Fired, Auto`, dates with their
  time zone, and sizes the way the picture is actually shown.
- **Shows where a photo was taken** and opens it on a map with one click.
- **Previews the picture**, turned the right way up.
- **Finds things fast**: type in the filter box to show only matching rows.
- **Never changes your files.** It only reads them, and it doesn't go online unless you click a
  map link.

## Works with

- **Photos**: JPEG, PNG, WebP, HEIC (iPhone), AVIF and TIFF
- **Camera RAW**: Canon, Nikon, Sony, Olympus, Panasonic and Pentax, plus DNG and Apple ProRAW
- **Videos**: MOV (iPhone) and MP4, opening instantly however large they are

Details are in [Supported formats](docs/Supported-Formats.md).

## Download

Get the latest version for your system from the [Releases](../../releases) page. Nothing else
needs installing. The first time you open it, macOS or Windows may ask you to confirm; the
[installing guide](docs/Installing.md) shows how.

## Documentation

**[Read the documentation](docs/Home.md)** for how to use the app and what all the data means,
for example:

- [Why does a photo have three different dates?](docs/Dates-and-Times.md)
- [What do shutter speed, f-number and ISO mean?](docs/Camera-Settings.md)
- [Where was this taken, and who can see that?](docs/Location-Data.md)
- [What's inside an iPhone photo or Live Photo?](docs/Apple-Photos-and-Videos.md)

## For developers

Building, testing, the code layout and releases are covered in the
[developer guide](docs/Developer-Guide.md).
