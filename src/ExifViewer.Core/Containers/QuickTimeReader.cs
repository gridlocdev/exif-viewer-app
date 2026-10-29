using System.Globalization;
using System.Text;
using ExifViewer.Core.Binary;

namespace ExifViewer.Core.Containers;

/// <summary>
/// Reads QuickTime MOV and MP4 structure: the movie header, each track's codec and timing, Apple's
/// 'mdta' metadata keys (make, model, location, Live Photo identifier), iTunes-style tags and
/// QuickTime user data. Media samples aren't read.
/// </summary>
internal static class QuickTimeReader
{
    private const string MovieDirectory = "QuickTime";
    private const string MetadataDirectory = "QuickTime Metadata";
    private const string UserDataDirectory = "QuickTime User Data";
    private const string ApplePrefix = "com.apple.quicktime.";
    private static readonly DateTime Epoch1904 = new(1904, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public static void Read(ReadOnlyMemory<byte> data, ReportBuilder report)
    {
        var r = new EndianReader(data, bigEndian: true);
        try
        {
            foreach (var box in IsoMedia.Children(r, 0, r.Length))
            {
                if (box.Type == "ftyp")
                    IsoMedia.ReadBrands(r, box, report, MovieDirectory);
                else if (box.Type == "moov")
                    ReadMovie(r, box, report);
            }
        }
        catch (MalformedDataException e)
        {
            report.Warn("QuickTime: " + e.Message);
        }
    }

    private static void ReadMovie(EndianReader r, IsoBox moov, ReportBuilder report)
    {
        foreach (var box in IsoMedia.Children(r, moov))
        {
            switch (box.Type)
            {
                case "mvhd":
                    var header = ReadTimes(r, box);
                    AddDate(report, MovieDirectory, "Created", header.Created);
                    AddDate(report, MovieDirectory, "Modified", header.Modified);
                    if (header.Timescale > 0)
                        report.Add(MovieDirectory, "Duration", Duration((double)header.Duration / header.Timescale));
                    break;
                case "trak":
                    ReadTrack(r, box, report);
                    break;
                case "meta":
                    ReadMeta(r, box, report);
                    break;
                case "udta":
                    ReadUserData(r, box, report);
                    break;
            }
        }
    }

    private readonly record struct Times(ulong Created, ulong Modified, uint Timescale, ulong Duration, long End);

    /// <summary>The creation/modification/timescale/duration fields shared by 'mvhd' and 'mdhd' (versions 0 and 1).</summary>
    private static Times ReadTimes(EndianReader r, IsoBox box)
    {
        long p = box.BodyOffset;
        return r.U8(p) == 1
            ? new Times(r.U64(p + 4), r.U64(p + 12), r.U32(p + 20), r.U64(p + 24), p + 32)
            : new Times(r.U32(p + 4), r.U32(p + 8), r.U32(p + 12), r.U32(p + 16), p + 20);
    }

    private static void ReadTrack(EndianReader r, IsoBox trak, ReportBuilder report)
    {
        var mdia = IsoMedia.Find(r, trak, "mdia");
        string handler = "";
        if (mdia is { } media && IsoMedia.Find(r, media, "hdlr") is { } hdlr)
            handler = Encoding.Latin1.GetString(r.Span(hdlr.BodyOffset + 8, 4));
        string directory = report.UniqueName(handler switch
        {
            "vide" => "Video Track",
            "soun" => "Audio Track",
            "meta" => "Metadata Track",
            "tmcd" => "Timecode Track",
            "text" or "sbtl" or "subt" => "Text Track",
            _ => "Track",
        });

        string? format = null;
        IsoBox? entry = null;
        if (mdia is { } m && IsoMedia.Find(r, m, "minf", "stbl", "stsd") is { } stsd && r.U32(stsd.BodyOffset + 4) > 0)
        {
            entry = IsoMedia.Children(r, stsd, skip: 8).Cast<IsoBox?>().FirstOrDefault();
            format = entry?.Type;
        }
        if (format is not null)
            report.Add(directory, "Codec", CodecName(format));

        if (IsoMedia.Find(r, trak, "tkhd") is { } tkhd && handler == "vide")
            ReadTrackHeader(r, tkhd, directory, report);

        if (mdia is not { } mediaBox || IsoMedia.Find(r, mediaBox, "mdhd") is not { } mdhd)
            return;
        var times = ReadTimes(r, mdhd);
        double seconds = times.Timescale > 0 ? (double)times.Duration / times.Timescale : 0;
        if (seconds > 0)
            report.Add(directory, "Duration", Duration(seconds));
        if (Language(r.U16(times.End)) is string language)
            report.Add(directory, "Language", language);

        if (handler == "vide" && entry is { } video && video.End - video.Offset >= 36)
        {
            report.Add(directory, "Coded Size", $"{r.U16(video.Offset + 32)}×{r.U16(video.Offset + 34)}");
            if (seconds > 0 && IsoMedia.Find(r, mediaBox, "minf", "stbl", "stts") is { } stts)
                report.Add(directory, "Frame Rate", (SampleCount(r, stts) / seconds).ToString("0.##", Inv) + " fps");
        }
        else if (handler == "soun" && entry is { } audio && audio.End - audio.Offset >= 36)
        {
            report.Add(directory, "Channels", r.U16(audio.Offset + 24).ToString(Inv));
            report.Add(directory, "Sample Rate", (r.U32(audio.Offset + 32) >> 16).ToString(Inv) + " Hz");
        }
    }

    /// <summary>Display size and rotation from the track header's matrix (iPhone portrait video is rotated 90°).</summary>
    private static void ReadTrackHeader(EndianReader r, IsoBox tkhd, string directory, ReportBuilder report)
    {
        long p = tkhd.BodyOffset;
        long matrix = p + (r.U8(p) == 1 ? 4 + 32 : 4 + 20) + 16;
        double a = r.S32(matrix) / 65536.0, b = r.S32(matrix + 4) / 65536.0;
        double width = r.U32(matrix + 36) / 65536.0, height = r.U32(matrix + 40) / 65536.0;
        report.Add(directory, "Width", width.ToString("0.##", Inv));
        report.Add(directory, "Height", height.ToString("0.##", Inv));
        int rotation = ((int)Math.Round(Math.Atan2(b, a) * 180 / Math.PI) + 360) % 360;
        if (rotation != 0)
            report.Add(directory, "Rotation", $"{rotation}° clockwise");
    }

    private static long SampleCount(EndianReader r, IsoBox stts)
    {
        uint entries = r.U32(stts.BodyOffset + 4);
        long total = 0;
        for (uint i = 0; i < entries; i++)
            total += r.U32(stts.BodyOffset + 8 + i * 8L);
        return total;
    }

    /// <summary>
    /// 'meta' with 'keys' + 'ilst' (QuickTime 'mdta', used by Apple devices) or 'ilst' alone (iTunes-style).
    /// QuickTime's moov/meta is a plain box; MP4's udta/meta is a full box with 4 bytes of version and flags.
    /// </summary>
    private static void ReadMeta(EndianReader r, IsoBox meta, ReportBuilder report)
    {
        int skip = r.U32(meta.BodyOffset) == 0 ? 4 : 0;
        var keys = new List<string>();
        IsoBox? ilst = null;
        foreach (var box in IsoMedia.Children(r, meta, skip))
        {
            if (box.Type == "keys")
            {
                long p = box.BodyOffset + 8;
                uint count = r.U32(box.BodyOffset + 4);
                for (uint i = 0; i < count && p + 8 <= box.End; i++)
                {
                    uint size = r.U32(p);
                    if (size < 8)
                        break;
                    keys.Add(Encoding.UTF8.GetString(r.Span(p + 8, size - 8)));
                    p += size;
                }
            }
            else if (box.Type == "ilst")
            {
                ilst = box;
            }
        }
        if (ilst is null)
            return;

        foreach (var item in IsoMedia.Children(r, ilst.Value))
        {
            uint index = r.U32(item.Offset + 4);
            string key = keys.Count > 0 && index >= 1 && index <= keys.Count ? keys[(int)index - 1] : item.Type;
            foreach (var data in IsoMedia.Children(r, item))
            {
                if (data.Type != "data" || data.End - data.BodyOffset < 8)
                    continue;
                uint kind = r.U32(data.BodyOffset) & 0xFFFFFF;
                var value = r.Slice(data.BodyOffset + 8, data.End - data.BodyOffset - 8);
                if (kind == 13)
                    report.OfferPreview(value);
                string text = DataValue(kind, value.Span);
                report.Add(MetadataDirectory, KeyName(key), text);
                if (key is ApplePrefix + "location.ISO6709" or "©xyz")
                    IsoMedia.AddIso6709(report, MetadataDirectory, text);
            }
        }
    }

    /// <summary>QuickTime user data: '©'-prefixed text atoms (16-bit length, 16-bit language, text) and nested 'meta'.</summary>
    private static void ReadUserData(EndianReader r, IsoBox udta, ReportBuilder report)
    {
        foreach (var box in IsoMedia.Children(r, udta))
        {
            if (box.Type == "meta")
            {
                ReadMeta(r, box, report);
                continue;
            }
            if (box.Type[0] != '©' || box.End - box.BodyOffset < 4)
                continue;
            int length = r.U16(box.BodyOffset);
            if (length > box.End - box.BodyOffset - 4)
                continue;
            string text = Encoding.UTF8.GetString(r.Span(box.BodyOffset + 4, length)).TrimEnd('\0');
            report.Add(UserDataDirectory, KeyName(box.Type), text);
            if (box.Type == "©xyz")
                IsoMedia.AddIso6709(report, UserDataDirectory, text);
        }
    }

    /// <summary>A 'data' box value by its well-known type (QuickTime File Format, "Well-known types").</summary>
    private static string DataValue(uint kind, ReadOnlySpan<byte> v) => kind switch
    {
        1 => Encoding.UTF8.GetString(v).TrimEnd('\0'),
        2 => Encoding.BigEndianUnicode.GetString(v).TrimEnd('\0'),
        13 => $"(JPEG image, {v.Length:N0} bytes)",
        14 => $"(PNG image, {v.Length:N0} bytes)",
        21 or 22 when v.Length is 1 or 2 or 4 or 8 => Integer(v, signed: kind == 21).ToString(Inv),
        23 when v.Length == 4 => new EndianReader(v.ToArray(), true).F32(0).ToString("0.######", Inv),
        24 when v.Length == 8 => new EndianReader(v.ToArray(), true).F64(0).ToString("0.######", Inv),
        _ when v.Length > 0 && v.IndexOfAnyExceptInRange((byte)0x20, (byte)0x7E) < 0 => Encoding.ASCII.GetString(v),
        _ => $"({v.Length:N0} bytes)",
    };

    private static long Integer(ReadOnlySpan<byte> v, bool signed)
    {
        ulong value = 0;
        foreach (byte b in v)
            value = value << 8 | b;
        if (!signed || v.Length == 8)
            return unchecked((long)value);
        int shift = 64 - v.Length * 8;
        return unchecked((long)(value << shift)) >> shift;
    }

    private static string KeyName(string key)
    {
        string shortKey = key.StartsWith(ApplePrefix, StringComparison.Ordinal) ? key[ApplePrefix.Length..] : key;
        return Names.TryGetValue(shortKey, out var name) ? name : shortKey;
    }

    private static readonly Dictionary<string, string> Names = new()
    {
        ["make"] = "Make",
        ["model"] = "Model",
        ["software"] = "Software",
        ["creationdate"] = "Creation Date",
        ["location.ISO6709"] = "Location (ISO 6709)",
        ["location.accuracy.horizontal"] = "Location Accuracy (m)",
        ["location.name"] = "Location Name",
        ["content.identifier"] = "Content Identifier",
        ["live-photo.auto"] = "Live Photo Auto",
        ["live-photo.vitality-score"] = "Live Photo Vitality Score",
        ["live-photo.vitality-scoring-version"] = "Live Photo Vitality Scoring Version",
        ["full-frame-rate-playback-intent"] = "Full Frame Rate Playback Intent",
        ["camera.lens_model"] = "Lens Model",
        ["camera.focal_length.35mm_equivalent"] = "Focal Length (35mm)",
        ["title"] = "Title",
        ["description"] = "Description",
        ["author"] = "Author",
        ["comment"] = "Comment",
        ["keywords"] = "Keywords",
        ["encoder"] = "Encoder",
        ["©nam"] = "Title",
        ["©ART"] = "Artist",
        ["aART"] = "Album Artist",
        ["©alb"] = "Album",
        ["©day"] = "Date",
        ["©too"] = "Encoder",
        ["©cmt"] = "Comment",
        ["©gen"] = "Genre",
        ["©wrt"] = "Composer",
        ["©mak"] = "Make",
        ["©mod"] = "Model",
        ["©swr"] = "Software",
        ["©xyz"] = "Location (ISO 6709)",
        ["desc"] = "Description",
        ["cprt"] = "Copyright",
        ["covr"] = "Cover Art",
    };

    private static string CodecName(string format) => format switch
    {
        "avc1" or "avc3" => "H.264 (AVC)",
        "hvc1" or "hev1" => "HEVC (H.265)",
        "dvh1" or "dvhe" => "Dolby Vision (HEVC)",
        "av01" => "AV1",
        "vp09" => "VP9",
        "apch" => "Apple ProRes 422 HQ",
        "apcn" => "Apple ProRes 422",
        "apcs" => "Apple ProRes 422 LT",
        "apco" => "Apple ProRes 422 Proxy",
        "ap4h" => "Apple ProRes 4444",
        "ap4x" => "Apple ProRes 4444 XQ",
        "aprn" or "aprh" => "Apple ProRes RAW",
        "jpeg" or "mjpa" => "Motion JPEG",
        "mp4a" => "AAC",
        "alac" => "Apple Lossless",
        "lpcm" => "Linear PCM",
        "sowt" => "PCM (little-endian)",
        "twos" => "PCM (big-endian)",
        "ac-3" => "Dolby Digital",
        "ec-3" => "Dolby Digital Plus",
        "Opus" => "Opus",
        "fLaC" => "FLAC",
        "mebx" => "Timed metadata",
        "tmcd" => "Timecode",
        _ => format.Trim(),
    };

    /// <summary>ISO 639-2 code packed as three 5-bit letters; values under 0x400 are old Macintosh codes.</summary>
    private static string? Language(ushort packed)
    {
        if (packed < 0x400 || packed == 0x7FFF)
            return null;
        var code = new string([(char)((packed >> 10 & 0x1F) + 0x60), (char)((packed >> 5 & 0x1F) + 0x60), (char)((packed & 0x1F) + 0x60)]);
        return code == "und" ? null : code;
    }

    private static void AddDate(ReportBuilder report, string directory, string name, ulong secondsSince1904)
    {
        // Zero means unset; anything past year 9999 is garbage.
        if (secondsSince1904 == 0 || secondsSince1904 > 255_000_000_000)
            return;
        report.Add(directory, name, Epoch1904.AddSeconds(secondsSince1904).ToString("yyyy-MM-dd HH:mm:ss", Inv) + " UTC");
    }

    internal static string Duration(double seconds)
    {
        var t = TimeSpan.FromSeconds(seconds);
        return $"{(int)t.TotalHours}:{t.Minutes:00}:{t.Seconds:00}.{t.Milliseconds:000}";
    }
}
