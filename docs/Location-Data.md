# Location data

Phones and many cameras save where a photo or video was taken. The app shows this in the **GPS**
group for photos, and in **QuickTime Metadata** for videos.

## Reading the GPS group

| Tag | Example | Meaning |
|---|---|---|
| **Latitude** | `37° 46′ 29.64″ N` | Distance north (N) or south (S) of the equator, in degrees, minutes and seconds. |
| **Longitude** | `122° 25′ 9.84″ W` | Distance east (E) or west (W) of the prime meridian through Greenwich. |
| **Latitude Ref**, **Longitude Ref** | `North`, `West` | Which side of the equator or meridian. Already included in the two rows above; stored separately in the file. |
| **Position** | `37.774900, -122.419400` | The same place as one pair of decimal numbers (the form map apps use). South and west are negative. Has a **Show on map** link. |
| **Altitude** | `12.5 m` or `3.2 m below sea level` | Height above (or below) sea level. Phone altitude is often off by tens of meters. |
| **Horizontal Positioning Error** | `35` | How far off the position might be, in meters. Larger means less certain. |
| **Speed**, **Speed Ref** | `4.2 km/h` | How fast the device was moving. |
| **Image Direction**, **Image Direction Ref** | `243.5°`, `True north` | Which way the camera was pointing: 0° is north, 90° east, 180° south, 270° west. |
| **Track** | `180°` | Which way the device was moving. |
| **Date Stamp**, **Time Stamp** | `2024-05-01`, `14:05:30 UTC` | When the position was fixed, in UTC from the satellites. |
| **Map Datum** | `WGS-84` | The model of the Earth used. Almost always WGS-84, the same one map apps use. |
| **Processing Method** | `GPS`, `NETWORK`, `fused` | How the position was found: satellites, or Wi-Fi/cell towers. |

### Degrees, minutes and seconds

`37° 46′ 29.64″` means 37 degrees, 46 minutes (′) and 29.64 seconds (″). One degree is 60 minutes,
one minute is 60 seconds, so this is `37 + 46/60 + 29.64/3600 = 37.7749` in decimal, which is the
number in **Position**.

A second of latitude is about 30 meters on the ground, and the hundredths in `29.64″` are about
30 cm. That's more precise than phone GPS really is.

## Videos

Video location is stored as a single text value, **Location (ISO 6709)**, for example
`+37.7749-122.4194+012.345/`. That's latitude, longitude and altitude with their signs. The app
splits it into **Position** (with **Show on map**) and **Altitude**.

iPhone videos may also have **Location Accuracy (m)**.

## Show on map

Click **Show on map** under a Position to open it in:

- **Apple Maps** on macOS (it opens the Maps app), or
- **OpenStreetMap** in your default web browser on Windows and Linux.

**Privacy**: the app itself never sends anything over the network. Clicking the link hands the
coordinates to that map service in the same way as typing them into its search box would. If
nothing happens when you click, check that you have a default web browser set.

## Location and privacy

Location data is sensitive. A photo shared with GPS intact can reveal where you live, work or
travel. Things to know:

- Many social networks and messaging apps **remove** location when you upload, but email, file
  sharing, cloud links and AirDrop usually **keep** it.
- On iPhone, the share sheet has **Options → Location** to leave it out.
- EXIF Viewer only reads files and can't remove location. To strip it, use your phone's share
  options, your photo app's "remove location" feature, or a tool such as `exiftool -gps:all=`.

## No location?

If there's no GPS group, the camera didn't record one: location was off, the camera had no GPS,
or an app removed it. Indoor photos sometimes have only a rough position, with a large
**Horizontal Positioning Error**.

See also: [Dates and times](Dates-and-Times.md).
