# Supported formats

The app recognises a file by its contents, not its name, so a JPEG renamed to `.png` still opens
as a JPEG.

## Photos

| Format | Typical source | What's read | Preview |
|---|---|---|---|
| **JPEG** (`.jpg`, `.jpeg`) | Almost every camera and phone | EXIF, GPS, XMP, JFIF, comments, image size and encoding | Yes |
| **PNG** (`.png`) | Screenshots, graphics, edited images | Image header, EXIF (`eXIf`), text notes, XMP, last-modified time, pixel density | Yes |
| **WebP** (`.webp`) | The web, some Android phones | Canvas and image size, lossy/lossless, EXIF, XMP | Yes |
| **HEIC / HEIF** (`.heic`, `.heif`) | iPhones and iPads since 2017, many Android phones | Image size and rotation, color, how the image is built, extra images (HDR, depth), EXIF, GPS, XMP | macOS only |
| **AVIF** (`.avif`) | The web, newer phones | Same as HEIF | macOS only |
| **TIFF** (`.tif`, `.tiff`) | Scanners, print workflows | Every TIFF and EXIF tag, GPS, XMP | macOS only (or a stored preview) |

## Camera RAW

RAW files hold the sensor data before any processing. Most are built on TIFF, so the app reads
their full EXIF and usually finds the JPEG preview the camera stored inside.

| Format | Camera maker |
|---|---|
| **DNG** (`.dng`) | Adobe's open format; also **Apple ProRAW**, Leica, Pentax, Google and many phones |
| **CR2** (`.cr2`) | Canon (older models) |
| **NEF / NRW** (`.nef`, `.nrw`) | Nikon |
| **ARW / SRF / SR2** (`.arw`, …) | Sony |
| **ORF** (`.orf`) | Olympus / OM System |
| **RW2** (`.rw2`) | Panasonic |
| **PEF** (`.pef`) | Pentax |

**CR3** (newer Canon cameras) opens, but only the general file structure is shown for now; its
EXIF isn't read yet.

## Video

| Format | Typical source | What's read |
|---|---|---|
| **MOV** (`.mov`) | iPhones, Macs, many cameras | Duration and dates, each track's codec, size, rotation and frame rate, audio channels and sample rate, Apple's metadata (make, model, location, Live Photo identifier) |
| **MP4 / M4V** (`.mp4`, `.m4v`) | Android phones, the web, most cameras | The same, plus iTunes-style tags such as title and artist |
| **3GP** (`.3gp`) | Older phones | The same as MP4 |

Videos open quickly however large they are: the app reads only the small part of the file that
describes the video, not the video itself.

There's a preview for video only on macOS (a still frame from Quick Look).

## Not read yet

- **Maker notes**: each camera maker's private block of extra settings inside EXIF (including
  Apple's). The app shows its size but doesn't decode it.
- **IPTC**: older captions/keywords/copyright format used by news agencies and some editors.
  Most editors also write the same information as XMP, which *is* shown.
- **ICC profile contents**: the app shows that a color profile exists and its size, but not
  what's in it.
- **CR3** EXIF, as above.

See [Metadata groups](Metadata-Groups.md) for what each part of the list means.
