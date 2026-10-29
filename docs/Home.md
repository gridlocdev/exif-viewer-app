# EXIF Viewer documentation

EXIF Viewer shows the information that cameras, phones and editing software store inside photos
and videos: when and where a picture was taken, which camera and settings made it, how big it
is, which way up it goes, and much more. It runs on macOS, Windows and Linux and never changes
your files.

These pages explain two things: **how to use the app**, and **what the data means**. You don't
need to know anything about photography or file formats to follow them.

## Start here

- New to the app? Read [Installing](Installing.md), then [Using the app](Using-the-App.md).
- Wondering whether your file will open? See [Supported formats](Supported-Formats.md).
- Looking at a list of tags and wondering what it all means? Start with
  [Metadata groups](Metadata-Groups.md), which explains every heading you can see in the app.

## Understanding your data

| Page | Answers questions like |
|---|---|
| [Camera settings](Camera-Settings.md) | What do shutter speed, f-number and ISO mean? Did the flash fire? |
| [Dates and times](Dates-and-Times.md) | Why does a photo have three different dates? Which one is right? |
| [Location data](Location-Data.md) | Where was this taken? How accurate is it? Who can see it? |
| [Orientation and size](Orientation-and-Size.md) | Why is the stored size sideways? What does "Rotate 90 CW" mean? |
| [Color and encoding](Color-and-Encoding.md) | What's "Uncalibrated" color space? What's HEVC? |
| [Apple photos and videos](Apple-Photos-and-Videos.md) | What's inside an iPhone HEIC or MOV? How do Live Photos fit together? |

## Reference

- [Tag IDs](Tag-IDs.md): the faint `0x…` numbers next to tags, and what "Unknown" tags are.
- [Glossary](Glossary.md): short definitions of EXIF, XMP, IFD, HEIF, APEX and other terms.
- [Warnings and troubleshooting](Troubleshooting.md): what messages in the status bar mean, and
  what to do when something doesn't show up.

## For developers

- [Developer guide](Developer-Guide.md): how the code is organized, how to add a tag or a format,
  testing, and releases.
