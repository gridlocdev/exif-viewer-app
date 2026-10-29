# Warnings and troubleshooting

## Common questions

**The preview says "No preview for …"**
On Windows and Linux, HEIC photos, AVIF, videos and some TIFF/RAW files can't be previewed:
decoding them would mean bundling a video decoder. On macOS the app asks Quick Look, so this
only happens if Quick Look can't render the file either. The metadata on the right is complete
either way. See [Using the app](Using-the-App.md#the-preview-left).

**The photo looks sideways in another app but upright here (or the reverse).**
That's the Orientation tag. EXIF Viewer applies it; some apps and websites don't. See
[Orientation and size](Orientation-and-Size.md).

**The dates don't match each other.**
They record different moments, and classic EXIF dates have no time zone. See
[Dates and times](Dates-and-Times.md).

**There's no GPS group.**
The camera didn't save a location, or an app removed it. See [Location data](Location-Data.md).

**"Show on map" doesn't open anything.**
It opens your default web browser (Apple Maps on macOS). Make sure one is set as default.

**A tag is called "Unknown 0x…".**
The app found a tag it doesn't have a name for yet. Its value is still shown. See
[Tag IDs](Tag-IDs.md).

**The values differ from another app.**
Usually only in presentation: this app shows `1/250 s` where another shows `0.004`, or rounds
differently. Names differ too (exiftool says `DateTimeOriginal`). Compare by [tag ID](Tag-IDs.md).

**A huge file won't open.**
Videos of any size open quickly. Other files over 2 GB (giant TIFFs, for instance) can't be read,
and the status bar says so.

**macOS says the app can't be opened / Windows SmartScreen warns.**
See [Installing](Installing.md#first-launch-warnings).

## Status bar warnings

If part of a file can't be read, the app keeps everything it could read and lists what went wrong in
the status bar. Most warnings mean the file is **damaged or cut short** (an incomplete download or
copy), or was written by software that bent the rules.

| Warning contains | Meaning |
|---|---|
| `runs past the end` | The file ends before the data it promises. Usually a truncated file; values read before that point are fine. |
| `has size …, past its parent's end` | Same, for HEIC, MOV and MP4: a box claims to be bigger than the space it's in. |
| `was already read; stopped following a loop` | The EXIF data points back at itself. The app stops instead of looping forever. |
| `has unknown type` | A tag uses a data type the standards don't define. That one tag is skipped. |
| `doesn't start with a TIFF header` | The EXIF block is damaged or isn't really EXIF. |
| `Expected a marker` | The JPEG's structure is broken partway through. |
| `XMP:` | The XMP text isn't valid XML, or contains a document type declaration, which the app refuses for safety. Other groups are unaffected. |
| `no 'meta' box`, `has no location` | A HEIC is missing part of its structure. |
| `construction method` | A HEIC stores an item in a way the app doesn't support yet. |
| `inflates past 16 MB` | A compressed PNG text chunk is suspiciously large; it's skipped to protect memory. |
| `Not a format ExifViewer can read` | The file isn't a supported image or video. The File group is still shown. |
| `Couldn't open …` | The file couldn't be read at all: it's missing, locked, or you lack permission. |

## Safety

EXIF Viewer only **reads** files and never changes them. It makes no network connections except
when you click **Show on map**. On macOS, previews come from the system's Quick Look, which runs
locally.
