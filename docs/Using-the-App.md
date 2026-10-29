# Using the app

## Opening a file

There are three ways to open a photo or video:

- Click **Open…** (or press **Ctrl+O** on Windows and Linux, **Cmd+O** on macOS) and pick a file.
  The file picker has **Images** and **Videos** filters; choose **All files** to open something
  that isn't listed.
- **Drag a file** from your file manager onto the window.
- From a terminal, pass the file when starting the app, for example `ExifViewer photo.jpg`.

Opening a file never changes it. EXIF Viewer only reads.

## The window

```
┌────────────────────────────────────────────────────────────────┐
│ [Open…]  photo.jpg                               [Filter tags] │
├──────────────────────┬─────────────────────────────────────────┤
│                      │ ⌄ File  4                               │
│                      │   File Name      photo.jpg              │
│       preview        │   File Size      3.9 MB (…)             │
│                      │ ⌄ Image  8                              │
│                      │   Make           Apple          0x010F  │
│                      │   …                                     │
├──────────────────────┴─────────────────────────────────────────┤
│ JPEG · 31 tags in 5 groups                                     │
└────────────────────────────────────────────────────────────────┘
```

### The preview (left)

The app shows the picture so you know which file you're looking at. It picks the best source it
can:

1. **The file itself**, for JPEG, PNG and WebP.
2. **A preview stored inside the file**, for most camera RAW files (and some TIFFs). Cameras keep
   a ready-made JPEG inside the RAW so it can be shown quickly.
3. **On macOS only**, a thumbnail from the system's Quick Look. This covers HEIC photos,
   videos, TIFFs and RAW files without a stored preview. While it's being made you'll see
   "Rendering preview…" for a second or two.

If none of these work you'll see **"No preview for …"**. That's expected for HEIC photos and
videos on Windows and Linux: showing them would mean bundling a video decoder, and the app
keeps its dependencies small. All the information is still listed on the right.

The preview is turned the right way up using the file's orientation (see
[Orientation and size](Orientation-and-Size.md)). Drag the divider to give the preview or the
list more room.

### The tag list (right)

Everything the app found, in **groups**. Each group has a header with its name and how many tags
it holds; click the header to fold it away or open it again. Folded groups stay folded when you filter or
open another file. [Metadata groups](Metadata-Groups.md)
explains what each group is.

Each row has three parts:

- **Name** (left, dimmed): what the value is, like *Exposure Time*.
- **Value**: the information itself, made readable. For example the file stores the exposure as
  the fraction `1/250`, and the app shows `1/250 s`. You can select and copy any value.
- **Tag ID** (right, faint): the number the file actually uses for this tag. Most people can
  ignore it; see [Tag IDs](Tag-IDs.md) for when it helps.

### Filtering

Type in **Filter tags** to show only the rows whose name or value contains that text, ignoring
upper and lower case. If the text matches a **group name** (say `gps`), that whole group stays
visible. Clear the box to see everything again. **Cmd+F** (macOS) or **Ctrl+F** jumps to the box.

Some handy filters: `date`, `gps`, `lens`, `iso`, `size`, `apple`.

### Show on map

Rows called **Position** (in the GPS group for photos, or the QuickTime Metadata group for
videos) have a **Show on map** link underneath. It opens the location in **Apple Maps** on
macOS, or **OpenStreetMap** in your web browser on Windows and Linux. Clicking it sends the
coordinates to that map service; nothing is sent unless you click. See
[Location data](Location-Data.md).

### The status bar (bottom)

Shows the file's format and how many tags were found, for example
`HEIC (HEIF, HEVC) · 31 tags in 5 groups`. If part of the file couldn't be read, a short warning
appears here too. Warnings don't mean the rest is wrong; see
[Warnings and troubleshooting](Troubleshooting.md).

## The menu bar (macOS)

On macOS the app has a standard menu bar:

| Menu | Items |
|---|---|
| **EXIF Viewer** | About EXIF Viewer (shows the version), Hide, Quit |
| **File** | Open… (Cmd+O), Close Window (Cmd+W) |
| **Edit** | Cut, Copy, Paste, Select All, Find (Cmd+F, jumps to the filter box) |
| **View** | Expand All Groups, Collapse All Groups, Enter Full Screen (Ctrl+Cmd+F) |
| **Window** | Minimize (Cmd+M), Zoom |
| **Help** | EXIF Viewer Help (this documentation), Report an Issue |

On Windows and Linux the toolbar and keyboard shortcuts do the same jobs.

## Light and dark

The app follows your system's light or dark appearance automatically.

---
Next: [Supported formats](Supported-Formats.md)
