# Glossary

**APEX**: *Additive System of Photographic Exposure*. A logarithmic way of writing shutter speed
and aperture, used by the *Shutter Speed Value*, *Aperture Value* and *Brightness Value* tags. The
app converts the first two to normal units. See [Camera settings](Camera-Settings.md).

**Aperture**: the opening in the lens that lets light in, written as an f-number (`f/1.8`). Smaller
number means a bigger opening.

**Atom / box**: the building blocks of MOV, MP4 and HEIC files. Each has a four-letter name like
`moov` or `meta`. The app reads them to find the metadata.

**Auxiliary image**: an extra image stored with a HEIC photo, such as a depth map or HDR gain map.

**Codec**: the method used to compress video or audio, such as HEVC or AAC.

**Container**: the file format that holds the image and its metadata (JPEG, PNG, HEIC, MOV…),
as opposed to the compression used inside it.

**DMS**: degrees, minutes, seconds: `37° 46′ 29.64″`. See [Location data](Location-Data.md).

**DNG**: Adobe's open RAW format, also used for Apple ProRAW.

**EV**: *exposure value*. One EV step doubles or halves the light. Used by Exposure Compensation.

**EXIF**: *Exchangeable Image File Format*. The standard most cameras use to store how and when a
photo was taken. Built on TIFF.

**Focal length**: how "zoomed in" a lens is, in millimetres. The 35mm equivalent makes different
cameras comparable.

**Gain map**: an extra image that tells HDR screens how much brighter to show each part of a photo.

**GPS**: satellite positioning. The GPS group holds where a photo was taken.

**HDR**: *High Dynamic Range*: more detail in bright and dark areas, and brighter highlights on
capable screens.

**HEIC / HEIF**: *High Efficiency Image File Format*. HEIF is the container; HEIC is HEIF with
HEVC-compressed images, the iPhone default. AVIF is HEIF with AV1 compression.

**HEVC**: *High Efficiency Video Coding*, also H.265. Used for iPhone video and HEIC photos.

**ICC profile**: a description of exactly what colors an image's numbers mean.

**IFD**: *Image File Directory*. A list of tags in TIFF and EXIF. The Image, Exif, GPS, Thumbnail
and SubIFD groups are each one IFD.

**IPTC**: an older metadata standard for captions, keywords and credits, from the news industry.
Not read yet; most of it is also in XMP.

**ISO**: the sensor's sensitivity (amplification). Higher is brighter but grainier.

**ISO 6709**: a compact text format for a location, like `+37.7749-122.4194+012.345/`, used in
videos.

**Live Photo**: an Apple photo plus a short video, as two files sharing a Content Identifier.

**Maker note**: a camera maker's private block of extra settings inside EXIF, in an undocumented
format. Shown as a size only.

**Metadata**: information *about* a file rather than its picture or sound: dates, settings, location.

**Orientation**: a tag that says which way up to show a picture. See
[Orientation and size](Orientation-and-Size.md).

**RAW**: a file holding the camera sensor's data before processing. CR2, NEF, ARW, DNG and others.

**Rational**: a fraction such as `1/250` or `28/10`. EXIF stores many values this way; the app shows
them as `1/250 s` or `f/2.8`.

**sRGB**: the standard color range for screens and the web.

**Tag**: one piece of metadata: a name (or ID) and a value.

**Tag ID**: the number a tag is stored under, such as `0x0112`. See [Tag IDs](Tag-IDs.md).

**Thumbnail**: a small preview image stored inside a photo.

**TIFF**: an image format whose tag structure EXIF and most RAW formats are built on.

**UTC**: Coordinated Universal Time, the time zone-free reference time. GPS times and video dates use it.

**XMP**: *Extensible Metadata Platform*. Adobe's flexible XML-based metadata, used by editors and
phones. See [Metadata groups](Metadata-Groups.md#xmp).
