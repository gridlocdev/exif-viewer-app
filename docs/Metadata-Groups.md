# Metadata groups

A file's information is stored in several separate sections, written by different things: the
camera, the file format itself, and editing software. The app keeps those sections apart as
**groups**, so you can tell where a value came from. This page explains every group you can see.

Which groups appear depends on the file. A phone photo might have six; a PNG screenshot might
have two.

## Always present

### File
Facts about the file on your disk rather than about the picture: name, size, the format the app
detected, and when the file was last **modified** on this computer. That date changes when a
file is copied or edited, so it's often *not* when the photo was taken; see
[Dates and times](Dates-and-Times.md).

## EXIF groups (photos)

EXIF is the main standard cameras use to record how a photo was taken. Inside the file it's
split into sections, which the app shows as these groups:

| Group | What's in it |
|---|---|
| **Image** | The basics about the main picture: camera **Make** and **Model**, **Orientation**, resolution (DPI), the **Software** that last saved it, a **Date/Time**, and copyright or artist if set. |
| **Exif** | How the photo was taken: exposure time, f-number, ISO, flash, focal length, lens, white balance, the original capture date, and more. See [Camera settings](Camera-Settings.md). |
| **GPS** | Where it was taken: latitude, longitude, altitude, sometimes speed and direction, plus a combined **Position** with a map link. See [Location data](Location-Data.md). |
| **Interoperability** | A small technical marker saying which EXIF rules the file follows (for example `R98`, the standard for sRGB files). Safe to ignore. |
| **Thumbnail** | Details of the small preview image stored inside the photo: its size in bytes and how it's compressed. |
| **SubIFD**, **SubIFD 2**, … | Found in camera RAW files. Each describes another image stored inside the file, such as a larger preview or the raw sensor data itself. |
| **IFD2**, **IFD3**, … | Rare extra images chained after the thumbnail. |

"IFD" stands for *Image File Directory*, the name the TIFF standard (which EXIF is built on)
gives each of these sections.

## Format groups

These describe the container itself. The app reads them directly from the file's structure, so
they're always accurate about the actual image or video.

| Group | Format | What's in it |
|---|---|---|
| **JPEG** | JPEG | The real pixel width and height, encoding (baseline or progressive), bits per sample, number of color components, and any embedded comment. |
| **JFIF** | JPEG | An older, simpler header some JPEGs carry: version and pixel density. |
| **PNG** | PNG | Width and height, bit depth, color type (RGB, with alpha, palette…), interlacing, pixel density and last-modified time. |
| **PNG Text** | PNG | Free-text notes stored in the PNG, such as `Software` or `Comment`. Each row's name is the note's keyword. |
| **WebP** | WebP | Canvas size, whether it's animated or has transparency, and lossy or lossless encoding. |
| **HEIF** | HEIC, HEIF, AVIF | How the image is built and stored: brand, display size, rotation, color settings, the list of items inside, and extra images like an HDR gain map. See [Apple photos and videos](Apple-Photos-and-Videos.md). |

## Video groups

| Group | What's in it |
|---|---|
| **QuickTime** | The whole movie: file brand, **Created** and **Modified** dates (in UTC), and **Duration**. Applies to MP4 as well as MOV, since both share the same structure. |
| **Video Track** | The picture: codec (H.264, HEVC, ProRes…), **Display Size**, **Rotation**, **Stored Size**, duration and frame rate. |
| **Audio Track** | The sound: codec (AAC, Apple Lossless…), channels (2 = stereo) and sample rate. |
| **Metadata Track**, **Timecode Track**, **Text Track** | Extra tracks some cameras and editors add (timed metadata, timecode, subtitles). Usually just a codec and duration. |
| **QuickTime Metadata** | Descriptive tags: on iPhones the **Make**, **Model**, **Software**, **Creation Date**, location (**Position**) and the Live Photo **Content Identifier**; in other files things like **Title** or **Encoder**. |
| **QuickTime User Data** | An older place for the same kind of information, used by some cameras and tools. |

A file with two video tracks shows **Video Track** and **Video Track 2**.

## XMP

**XMP** is a newer, flexible metadata format from Adobe, used by Lightroom, Photoshop, phones and
many other tools. It can hold almost anything: ratings, keywords, titles, edit history, and copies
of EXIF values.

The app lists XMP properties with their **namespace prefix**, which tells you which vocabulary
the property belongs to:

| Prefix | Vocabulary | Example properties |
|---|---|---|
| `dc:` | Dublin Core: general description | `dc:title`, `dc:creator`, `dc:subject` (keywords), `dc:rights` |
| `xmp:` | Basic XMP | `xmp:Rating`, `xmp:CreatorTool`, `xmp:CreateDate`, `xmp:ModifyDate` |
| `photoshop:` | Photoshop | `photoshop:DateCreated`, `photoshop:City`, `photoshop:Country` |
| `exif:`, `tiff:` | Copies of EXIF and TIFF values | `exif:PixelXDimension`, `tiff:Orientation` |
| `xmpMM:` | Media management: file identity and history | `xmpMM:DocumentID`, `xmpMM:History` |
| `crs:` | Camera Raw / Lightroom edit settings | `crs:Exposure2012`, `crs:WhiteBalance` |
| `aux:` | Extra camera and lens details | `aux:Lens`, `aux:SerialNumber` |

Lists such as keywords are joined with commas. Structured values are shown as
`field: value; field: value`.

XMP values are shown exactly as written (dates in `2024-05-01T12:34:56` form, numbers raw), so
`tiff:Orientation` shows `1` where the Image group shows `Horizontal (normal)`.

## When groups disagree

The same fact can appear in several groups, and they don't always match. That's normal:

- **Sizes**: `Exif Image Width` is what the camera recorded; the **JPEG**, **PNG** or **HEIF**
  group shows the actual pixels. After cropping or resizing in an app that doesn't update EXIF,
  they differ. Trust the format group. See [Orientation and size](Orientation-and-Size.md).
- **Dates**: EXIF dates, XMP dates and the file's Modified date each record a different moment.
  See [Dates and times](Dates-and-Times.md).
- **Software**: the Image group's **Software** is whatever saved the file last, which may be an
  editor rather than the camera.
