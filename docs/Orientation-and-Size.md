# Orientation and size

## Why photos are stored sideways

When you hold a phone upright, the camera sensor is still landscape-shaped. Rather than
rearranging every pixel, the camera saves the picture as it came off the sensor and adds a note
saying "turn this 90° when showing it". That note is the **Orientation** tag.

Good software reads it and shows the picture upright. Software that ignores it shows your photo
on its side, which is the usual cause of "my photo is sideways on this website".

EXIF Viewer reads the orientation and turns the **preview** the right way up.

## Orientation values

The **Orientation** tag in the Image group has eight possible values. The app shows the name; the
number is what's stored in the file.

| Value | Shown as | What the viewer does to show it upright |
|---|---|---|
| 1 | Horizontal (normal) | Nothing. |
| 2 | Mirror horizontal | Flip left↔right. |
| 3 | Rotate 180 | Turn upside down. |
| 4 | Mirror vertical | Flip top↔bottom. |
| 5 | Mirror horizontal and rotate 270 CW | Flip left↔right, then turn 90° counter-clockwise. |
| 6 | Rotate 90 CW | Turn 90° clockwise. (Phone held upright, the most common non-1 value.) |
| 7 | Mirror horizontal and rotate 90 CW | Flip left↔right, then turn 90° clockwise. |
| 8 | Rotate 270 CW | Turn 90° counter-clockwise. (Phone held upright the other way.) |

"CW" means clockwise. The mirrored values (2, 4, 5, 7) are rare and mostly come from front
cameras or editing apps.

## HEIC and video rotation

HEIC photos and videos describe rotation in their own structure, and the app shows it in plain
words:

- **HEIF** group, **Rotation**: for example `90° counter-clockwise`. HEIC files *also* carry an EXIF
  Orientation, usually set to match.
- **Video Track** group, **Rotation**: for example `90° clockwise`, typical of a portrait iPhone
  video.

`None` or no Rotation row means the picture is stored the way it's shown.

## Display size and stored size

Because of rotation, a picture has two sizes:

- **Display Size**: the size you actually see, upright. A portrait iPhone video is `1080×1920`.
- **Stored Size**: the size as saved in the file, before rotation: `1920×1080`. This row only
  appears when it differs from Display Size.

Sizes are always **width × height**, in pixels.

## Which size tag to trust

Photos can list their size in several places:

| Where | Tag | Trust it? |
|---|---|---|
| **JPEG**, **PNG**, **WebP**, **HEIF** group | Image Width / Height, Canvas Width / Height, Display Size | **Yes.** Read from the image data itself. |
| **Exif** group | Exif Image Width / Height | Usually. It's what the camera recorded, and editors don't always update it after cropping. |
| **Image** group | Image Width / Height | In TIFF and RAW files, yes. JPEGs often leave it out. |
| **Thumbnail**, **SubIFD** | Image Width / Height | These describe the *embedded* preview images, not the main photo. |
| **XMP** | `exif:PixelXDimension`, `tiff:ImageWidth` | Copies; may be stale after editing. |

## Resolution (DPI)

**X Resolution** / **Y Resolution** with **Resolution Unit** (`72` and `inches`) say how many
pixels per inch to use when *printing*. They don't change the picture's quality or pixel count.
`72` is just the common default.

See also: [Apple photos and videos](Apple-Photos-and-Videos.md), [Color and encoding](Color-and-Encoding.md).
