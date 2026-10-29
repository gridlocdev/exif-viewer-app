# Dates and times

A single photo can carry four or five different dates. They aren't contradicting each other;
each records a different moment. Knowing which is which saves a lot of confusion.

## Which date is which

| Tag | Group | Records | Use it for |
|---|---|---|---|
| **Date/Time Original** | Exif | The moment the shutter was pressed. | **"When was this taken?"** This is the one photo apps sort by. |
| **Date/Time Digitized** | Exif | When the image became digital. For digital cameras it's the same as Original; for a scanned print it's the scan date. | Scans |
| **Date/Time** | Image | When the file was last changed by software that updates EXIF, such as an editor. Often the same as Original for untouched photos. | Spotting edits |
| **Modified** | File | When the file on *this computer's disk* last changed. Copying, downloading or syncing can update it. | Rarely meaningful for when a photo was taken |
| **Created**, **Modified** | QuickTime | When a video was recorded and last changed, according to the video itself. | Videos |
| **Creation Date** | QuickTime Metadata | iPhone videos: the recording time, with time zone. | iPhone videos (most reliable) |
| **xmp:CreateDate**, **photoshop:DateCreated** … | XMP | Dates kept by editing software, usually copies of the above. | Edited files |

### How they're shown

EXIF stores dates as `2024:05:01 12:34:56`. The app shows them as `2024-05-01 12:34:56`, and adds
two extra pieces when the file has them:

- **Fractions of a second** from the matching *Sub-Sec Time* tag: `12:34:56.12`. Phones record
  these, which helps put burst shots in order.
- **The time zone offset** from the matching *Offset Time* tag: `+02:00` means two hours ahead of
  UTC.

Example: `2024-05-01 12:34:56.12 +02:00`.

If a date is blank or malformed in the file (some cameras write `0000:00:00 00:00:00` or spaces),
it's shown exactly as stored.

## Time zones: the big gotcha

**Classic EXIF dates have no time zone.** `2024-05-01 12:34:56` is the clock time on the camera,
wherever it was. If you flew from London to New York without changing the camera's clock, every
photo is five hours off and nothing in the file says so.

Newer cameras and all modern phones also write the **Offset Time** tags (EXIF 2.31 and later).
You'll see them in the Exif group as **Offset Time**, **Offset Time Original** and **Offset Time
Digitized**, and the app adds them onto the matching date where it can.

The **Date/Time** in the Image group gets its offset from **Offset Time** in the Exif group. The
app shows that offset as its own row rather than adding it to the date.

## Video dates are UTC

The **QuickTime** group's **Created** and **Modified** are stored as UTC (shown with ` UTC`), so
they won't match your local clock. Many non-Apple cameras get this wrong and write local time
anyway, so treat them with some suspicion.

iPhone videos also carry **Creation Date** in the QuickTime Metadata group, written as local time
*with* an offset, for example `2024-05-01T14:34:56+0200`. That one is the most trustworthy.

## GPS time

The **GPS** group has its own **Date Stamp** and **Time Stamp**, taken from satellites and always
in UTC. They're useful for checking a camera clock that was set wrong: compare GPS time with Date/Time
Original and the difference is the clock error (plus the time zone).

## Why is the file's Modified date wrong?

Because it describes the file, not the photo. Copying from a phone, downloading from the cloud,
or extracting a zip often resets it to "now". Use **Date/Time Original** instead.

See also: [Location data](Location-Data.md), [Apple photos and videos](Apple-Photos-and-Videos.md).
