# Apple photos and videos

iPhones and iPads save photos as **HEIC** and videos as **MOV** by default (Settings → Camera →
Formats → *High Efficiency*). With *Most Compatible* selected they save JPEG and H.264 MOV instead.
**ProRAW** photos are DNG files, and screenshots are PNG. EXIF Viewer reads all of these.

## Inside an iPhone HEIC

A HEIC is a container holding several **items**. The HEIF group summarises them:

| Tag | Typical iPhone value | Meaning |
|---|---|---|
| **Major Brand** | `heic` | The file type. |
| **Primary Image** | Grid of tiles | The picture you see isn't stored as one image. It's cut into tiles (usually 512×512) that are put back together when displayed. |
| **Display Size** | `3024×4032` | The full picture, upright. |
| **Rotation** | `90° counter-clockwise` | How the stored picture is turned for display. |
| **Items** | `HEVC × 49, Grid of tiles × 1, Exif × 1, MIME (XMP) × 1` | Everything inside: the tiles, the grid that joins them, a thumbnail, EXIF, XMP and any extra images. |
| **Auxiliary Images** | `HDR gain map` | Extra images stored alongside the photo (see below). |
| **Color Primaries** | Display P3 | iPhones capture a wider color range than sRGB. See [Color and encoding](Color-and-Encoding.md). |

### Extra images

- **HDR gain map**: lets HDR-capable screens show the photo brighter, with more highlight detail.
  Normal screens ignore it.
- **Depth map**: how far away each part of the scene is. Portrait mode uses it to blur the background,
  and you can change that blur later.
- **Portrait effects matte**: an outline of the person, for portrait lighting effects.
- **Skin, Hair and Teeth mattes**: masks used by Photos' editing tools.

### EXIF in HEIC

The HEIC's EXIF works just as in a JPEG: Image, Exif and GPS groups with the camera, settings,
dates and location. On iPhones, **Lens Model** names which camera was used, for example
`iPhone 16 Pro back triple camera 6.765mm f/1.78`, and **Software** is the iOS version.

Apple also writes a **Maker Note** with extra details (burst ID, HDR flags, the Live Photo
identifier). The app shows its size but doesn't decode it yet.

## Inside an iPhone video

| Group | What you'll find |
|---|---|
| **QuickTime** | Brand `qt`, Created/Modified in UTC, Duration. |
| **Video Track** | Codec (usually HEVC), **Display Size** (`1080×1920` for portrait 1080p), **Rotation** (`90° clockwise` for portrait), **Stored Size**, Frame Rate (30, 60, or about 24 for Cinematic mode). |
| **Audio Track** | AAC, usually 1 or 2 channels at 44100 or 48000 Hz. |
| **Metadata Track** | Timed metadata such as motion, focus or Cinematic mode data. |
| **QuickTime Metadata** | **Make**, **Model**, **Software** (iOS version), **Creation Date** with time zone, **Location (ISO 6709)** with **Position** and **Altitude**, and for Live Photos the **Content Identifier**. |

Portrait videos are stored landscape and rotated on playback, which is why Stored Size and
Display Size are swapped. See [Orientation and size](Orientation-and-Size.md).

## Live Photos

A Live Photo is **two files**: a HEIC (or JPEG) still and a short MOV, usually with the same name
(`IMG_1234.HEIC` and `IMG_1234.MOV`). When you export from Photos with "Include Live Photo" or copy
from the phone, you get both.

They're linked by a shared **Content Identifier**, a long ID like
`6A1D7C42-0B5C-4E7A-9D0E-1F2A3B4C5D6E`:

- In the **MOV**, it's in QuickTime Metadata as **Content Identifier**. You'll also see Live
  Photo rows such as **Live Photo Vitality Score**, which is how lively Photos judged the
  motion to be.
- In the **HEIC**, it's inside Apple's maker note, which the app doesn't decode yet. For now,
  match the pair by file name.

## ProRAW

ProRAW photos are **DNG** files. They show the same EXIF as a HEIC, plus RAW details in **SubIFD**
groups and a full-size JPEG preview, which the app displays on every platform.

## Screenshots

Screenshots are PNG. They usually have only the **PNG** group, plus sometimes a little XMP, because
no camera was involved.

See also: [Location data](Location-Data.md), [Dates and times](Dates-and-Times.md).
