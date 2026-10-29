# Camera settings

The **Exif** group records how the camera was set when the picture was taken. This page explains
the settings you'll see most often, in plain terms.

## The three that control brightness

A photo's brightness comes from three settings working together, often called the *exposure
triangle*.

| Tag | Shown as | What it means |
|---|---|---|
| **Exposure Time** | `1/250 s`, `2 s` | How long the sensor collected light (the *shutter speed*). Short times like `1/1000 s` freeze motion; long times like `1/15 s` let moving things blur and need a steady hand or a tripod. |
| **F-Number** | `f/1.8`, `f/8.0` | How wide the lens opening (*aperture*) was. **Smaller numbers mean a wider opening**: more light, and a shallower zone of focus (blurry background). `f/8` and above keeps more of the scene sharp. |
| **ISO** | `100`, `3200` | How much the sensor's signal was amplified. Low values (`50`–`400`) give the cleanest image; high values (`3200`+) brighten dark scenes but add grain. |

**Exposure Compensation** (`-0.67 EV`, `+1 EV`) shows whether the photographer, or the phone,
asked for a brighter (`+`) or darker (`-`) picture than the camera's meter suggested. Each
**EV** step doubles or halves the light, so `+1 EV` is twice as bright.

### Shutter Speed Value and Aperture Value

Some cameras also store **Shutter Speed Value** and **Aperture Value**. These record the same
settings in a different unit (*APEX*, a logarithmic scale). The app converts them, so they read
just like Exposure Time and F-Number. Small differences between the pairs are rounding. **Max
Aperture Value** is the widest opening the lens can manage, not the one used.

## How the camera decided

| Tag | Common values | Meaning |
|---|---|---|
| **Exposure Program** | Program AE, Aperture-priority AE, Shutter speed priority AE, Manual | Who chose the settings. *Program* means the camera chose both shutter and aperture; *Aperture-priority* means you chose the aperture; *Manual* means you chose everything. |
| **Exposure Mode** | Auto, Manual, Auto bracket | Similar, from the EXIF 2.2 standard. *Auto bracket* means the camera took a burst at different brightnesses. |
| **Metering Mode** | Multi-segment, Center-weighted average, Spot | How the camera measured the light: across the whole scene, mostly the middle, or a single small point. |
| **Scene Capture Type** | Standard, Landscape, Portrait, Night | A scene mode, if one was chosen. |
| **Sensitivity Type** | Recommended Exposure Index, Standard Output Sensitivity… | Which ISO standard the ISO value follows. You can ignore it. |

## Flash

**Flash** describes several things at once:

- **Fired** or **Did not fire**: whether light came out.
- The mode: **Auto** (camera decided), **Forced on**, **Forced off**.
- **Red-eye reduction** if pre-flashes were used.
- **Return detected / not detected**: whether a flash unit sensed its own light bouncing back.
  Most phones don't report it.
- **No flash function**: the device has no flash at all.

So `Did not fire, Auto` means the flash was on automatic and decided it wasn't needed.

## Lens and zoom

| Tag | Shown as | Meaning |
|---|---|---|
| **Focal Length** | `6.8 mm` | The lens's actual focal length. On phones and small cameras this number is tiny because the sensor is tiny. |
| **Focal Length In 35mm Film** | `24 mm` | The same view expressed for a full-frame (35mm film) camera, which makes cameras comparable. Roughly: `14–24 mm` ultra-wide, `24–35 mm` wide, `50 mm` normal, `70 mm`+ telephoto. |
| **Lens Info** | `24-70mm f/2.8-4` | The lens's range: shortest–longest focal length, and widest aperture at each end. A single number means a fixed (prime) lens. |
| **Lens Make**, **Lens Model** | `Apple`, `iPhone 16 Pro back triple camera 6.765mm f/1.78` | Which lens or camera module took the picture. Phones name the specific camera here (main, ultra-wide, telephoto). |
| **Digital Zoom Ratio** | `1`, `2.5` | How much the image was digitally enlarged (cropped). `1` or `0` means none. |
| **Subject Distance** | `2.5 m` | How far away the camera focused, if it recorded it. |

## Color and light

| Tag | Meaning |
|---|---|
| **White Balance** | **Auto** or **Manual**: whether the camera adjusted colors for the light source itself. |
| **Light Source** | What the light was set or detected as: Daylight, Cloudy, Tungsten, Fluorescent, Flash… *Unknown* is very common and just means it wasn't recorded. |
| **Brightness Value** | The scene's measured brightness on the APEX scale. Higher is brighter; negative values are dim indoor or night scenes. |
| **Contrast**, **Saturation**, **Sharpness** | In-camera processing: Normal, Low/Soft, High/Hard. |

## Identifying the camera

| Tag | Group | Meaning |
|---|---|---|
| **Make**, **Model** | Image | The camera or phone, e.g. `Apple` / `iPhone 16 Pro`. |
| **Software** | Image | Firmware or app that saved the file, e.g. `18.1` on an iPhone, or `Adobe Photoshop 26.0` after editing. |
| **Serial Number**, **Lens Serial Number**, **Owner Name** | Exif | Set by some cameras. Note they can identify you if you share the photo. |
| **Maker Note** | Exif | A private block of extra settings in the maker's own format. Shown as its size only; see [Supported formats](Supported-Formats.md#not-read-yet). |

See also: [Glossary](Glossary.md) for terms like APEX and EV.
