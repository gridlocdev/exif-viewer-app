# Tag IDs

Next to most rows, on the right, is a faint number like `0x0112`. That's the **tag ID**.

## What they are

Inside the file, EXIF tags don't have names. Each is stored as a number, a data type and a value.
`0x0112` is the number for Orientation, `0x829A` for Exposure Time, and so on. The names in the
app come from a lookup table built from the official TIFF, EXIF and DNG specifications.

The `0x` means the number is written in hexadecimal, the usual way these numbers appear in
specifications and other tools.

Rows that don't come from numbered tags have no ID: the File group, XMP, the container groups
(JPEG, PNG, HEIF…), video tracks, and values the app works out itself, such as **Position**.

## When they help

- **Unknown tags.** If the app shows `Unknown 0xC6FD`, it found a tag its table doesn't cover yet,
  often a camera maker's own addition. Search the web for the ID (for example "EXIF tag 0xC6FD")
  to find out what it is.
- **Comparing tools.** Different apps name the same tag differently: *Date/Time Original* here,
  `DateTimeOriginal` in exiftool, *Date taken* in Windows. The ID is the same everywhere.
- **Telling look-alikes apart.** *Image Width* (`0x0100`) and *Exif Image Width* (`0xA002`) sound
  alike but are different tags with different meanings (see
  [Orientation and size](Orientation-and-Size.md)).
- **Same tag, different group.** *X Resolution* (`0x011A`) appears in both Image and Thumbnail,
  describing different images.
- **Writing code.** Programming libraries look tags up by number.

## Some common IDs

| ID | Tag | Group |
|---|---|---|
| `0x010F` | Make | Image |
| `0x0110` | Model | Image |
| `0x0112` | Orientation | Image |
| `0x0132` | Date/Time | Image |
| `0x829A` | Exposure Time | Exif |
| `0x829D` | F-Number | Exif |
| `0x8827` | ISO | Exif |
| `0x9003` | Date/Time Original | Exif |
| `0x9209` | Flash | Exif |
| `0x920A` | Focal Length | Exif |
| `0xA434` | Lens Model | Exif |
| `0x0002` | Latitude | GPS |
| `0x0004` | Longitude | GPS |

GPS tags have their own small numbers starting at `0x0000`, separate from the main list, which is
why Latitude is `0x0002`.

## Specifications

- EXIF: published by CIPA as *DC-008* (Exchangeable image file format for digital still cameras).
- TIFF 6.0: the base format EXIF is built on.
- DNG: Adobe's Digital Negative specification, which adds RAW tags.
