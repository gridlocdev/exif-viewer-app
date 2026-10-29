# Color and encoding

These tags describe how the picture's colors and pixels are stored. You rarely need them, but
they explain a few values that look alarming and aren't.

## Color space

**Color Space** in the Exif group says which range of colors the image uses:

| Value | Meaning |
|---|---|
| **sRGB** | The standard range for the web and most screens. The safe default. |
| **Adobe RGB** | A wider range used by some cameras for printing. |
| **Uncalibrated** | "Not sRGB, see the attached color profile." **This is normal**, not an error. iPhones shooting in Display P3, and Photoshop exports in wide-gamut spaces, write it. |

When a file has an **ICC Profile** (in the HEIF group, or as a tag in Image/Exif), that profile
defines the exact colors. The app shows its size, such as `(536 bytes)`, but not its contents.

**Interoperability Index** says the same thing in shorthand: `R98` means sRGB, `R03` Adobe RGB.

## HEIC and AVIF color

The **HEIF** group can describe color with four values (called *nclx* in the file):

| Tag | Common values | Meaning |
|---|---|---|
| **Color Primaries** | Display P3, BT.709 (same as sRGB), BT.2020 | Which red, green and blue the image uses: how wide its color range is. iPhones use Display P3. |
| **Transfer Characteristics** | sRGB, PQ (HDR10), HLG | How brightness values map to light. PQ and HLG mean HDR. |
| **Matrix Coefficients** | BT.601, BT.709, Identity (RGB) | How brightness and color are separated for compression. |
| **Full Range** | Yes / No | Whether values use the full 0–255 range or the narrower "video" range. |

## How the image is compressed

| Tag | Group | Meaning |
|---|---|---|
| **Encoding** | JPEG | *Baseline DCT* is the standard JPEG; *Progressive DCT* loads blurry-to-sharp on the web. Both look the same once loaded. |
| **Compression** | Image, Thumbnail, SubIFD | For TIFF and RAW: *Uncompressed*, *LZW*, *Adobe Deflate*, or *JPEG (old-style)*, which is what EXIF thumbnails always say. |
| **Bits Per Sample** / **Bit Depth** / **Bits Per Channel** | Image, PNG, HEIF | How many bits per color. `8` gives 256 levels per color; `10`, `12` or `16` give smoother gradients, used for HDR and RAW. |
| **Color Components** | JPEG | `3` for color (luminance plus two color channels), `1` for grayscale. |
| **Components Configuration** | Exif | The order of those channels, almost always `Y, Cb, Cr, -`. |
| **YCbCr Positioning**, **YCbCr Sub-Sampling** | Image | Fine detail about how color is sampled. Safe to ignore. |
| **Photometric Interpretation** | Image | How pixel values should be read: *RGB*, *YCbCr*, *BlackIsZero* (grayscale), *Color Filter Array* (raw sensor data). |
| **Color Type** | PNG | *RGB*, *RGB with alpha* (has transparency), *Palette* (limited colors), *Grayscale*. |

## Video codecs

The **Codec** row in a Video or Audio Track names the compression format:

| Codec | Notes |
|---|---|
| **HEVC (H.265)** | Default on iPhones since 2017 ("High Efficiency"). About half the size of H.264 at the same quality. |
| **H.264 (AVC)** | The most widely compatible video format. iPhones use it with "Most Compatible" selected. |
| **Dolby Vision (HEVC)** | HEVC with HDR information; iPhone HDR video. |
| **Apple ProRes …** | High-quality editing formats (422, 422 HQ, 4444, RAW). Very large files. |
| **AV1**, **VP9** | Newer web formats. |
| **AAC** | The usual audio format in MOV and MP4. |
| **Apple Lossless**, **Linear PCM** | Uncompressed or lossless audio. |
| **Timed metadata** | Not picture or sound: data that changes during the video, such as iPhone motion or focus data. |

See also: [Apple photos and videos](Apple-Photos-and-Videos.md), [Glossary](Glossary.md).
