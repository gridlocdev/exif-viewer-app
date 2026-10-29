using System.Globalization;
using System.Text;

namespace ExifViewer.Core.Tiff;

/// <summary>
/// Turns decoded IFD values into display text. Known tags get units and enum names; everything else
/// falls back to a generic rendering of its type. Formatting is culture-invariant.
/// </summary>
internal static class TagFormatter
{
    private const int MaxListedValues = 8;
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    /// <param name="ifd">The other entries in the same IFD, for tags whose meaning depends on a sibling (GPS refs).</param>
    public static string Format(TagSet set, ushort tag, TiffValue value, IReadOnlyDictionary<ushort, TiffValue> ifd)
    {
        string? special = set switch
        {
            TagSet.Gps => FormatGps(tag, value, ifd),
            TagSet.Interop => tag == 0x0002 ? Version(value) : null,
            _ => FormatTiff(tag, value, ifd),
        };
        return special ?? Generic(value);
    }

    private static string? FormatTiff(ushort tag, TiffValue v, IReadOnlyDictionary<ushort, TiffValue> ifd)
    {
        long? i = v.FirstInteger;
        double? d = v.FirstNumber;
        return tag switch
        {
            0x0132 => DateTime(v, ifd, subSecTag: 0x9290, offsetTag: 0x9010),
            0x9003 => DateTime(v, ifd, subSecTag: 0x9291, offsetTag: 0x9011),
            0x9004 => DateTime(v, ifd, subSecTag: 0x9292, offsetTag: 0x9012),
            0x0103 => Lookup(i, Compression),
            0x0106 => Lookup(i, Photometric),
            0x0112 => Lookup(i, Orientations),
            0x011C => Lookup(i, new() { [1] = "Chunky", [2] = "Planar" }),
            0x0128 or 0xA210 => Lookup(i, new() { [1] = "None", [2] = "inches", [3] = "cm" }),
            0x0213 => Lookup(i, new() { [1] = "Centered", [2] = "Co-sited" }),
            0x829A => ExposureTime(d),
            0x829D => FNumber(d),
            0x8822 => Lookup(i, ExposurePrograms),
            0x8830 => Lookup(i, SensitivityTypes),
            0x9000 or 0xA000 => Version(v),
            0x9101 => ComponentsConfiguration(v),
            0x9201 => d is double tv ? ExposureTime(Math.Pow(2, -tv)) : null,
            0x9202 or 0x9205 => d is double av ? FNumber(Math.Pow(2, av / 2)) : null,
            0x9204 => d is double ev ? ev.ToString("+0.##;-0.##;0", Inv) + " EV" : null,
            0x9206 => d is double m ? m.ToString("0.##", Inv) + " m" : null,
            0x9207 => Lookup(i, MeteringModes),
            0x9208 => Lookup(i, LightSources),
            0x9209 => i is long f ? Flash(f) : null,
            0x920A => d is double fl ? fl.ToString("0.#", Inv) + " mm" : null,
            0x9286 => EncodedText(v),
            0x9C9B or 0x9C9C or 0x9C9D or 0x9C9E or 0x9C9F => Encoding.Unicode.GetString(v.Bytes).TrimEnd('\0'),
            0xA001 => Lookup(i, new() { [1] = "sRGB", [2] = "Adobe RGB", [0xFFFF] = "Uncalibrated" }),
            0xA217 => Lookup(i, SensingMethods),
            0xA300 => v.Bytes is [3, ..] ? "Digital Camera" : null,
            0xA301 => v.Bytes is [1, ..] ? "Directly photographed" : null,
            0xA401 => Lookup(i, new() { [0] = "Normal", [1] = "Custom" }),
            0xA402 => Lookup(i, new() { [0] = "Auto", [1] = "Manual", [2] = "Auto bracket" }),
            0xA403 => Lookup(i, new() { [0] = "Auto", [1] = "Manual" }),
            0xA405 => i is long mm ? $"{mm} mm" : null,
            0xA406 => Lookup(i, new() { [0] = "Standard", [1] = "Landscape", [2] = "Portrait", [3] = "Night" }),
            0xA407 => Lookup(i, new() { [0] = "None", [1] = "Low gain up", [2] = "High gain up", [3] = "Low gain down", [4] = "High gain down" }),
            0xA408 or 0xA409 => Lookup(i, new() { [0] = "Normal", [1] = "Low", [2] = "High" }),
            0xA40A => Lookup(i, new() { [0] = "Normal", [1] = "Soft", [2] = "Hard" }),
            0xA40C => Lookup(i, new() { [0] = "Unknown", [1] = "Macro", [2] = "Close", [3] = "Distant" }),
            0xA432 => LensInfo(v),
            0x002E or 0x83BB or 0x8649 or 0x8773 or 0x927C or 0xC4A5 => ByteSummary(v),
            _ => null,
        };
    }

    private static string? FormatGps(ushort tag, TiffValue v, IReadOnlyDictionary<ushort, TiffValue> ifd)
    {
        double? d = v.FirstNumber;
        string? Ref(ushort refTag) => ifd.TryGetValue(refTag, out var r) ? r.Text : null;
        return tag switch
        {
            0x00 => string.Join('.', v.Integers),
            0x01 or 0x13 => Lookup(v.Text, new() { ["N"] = "North", ["S"] = "South" }),
            0x03 or 0x15 => Lookup(v.Text, new() { ["E"] = "East", ["W"] = "West" }),
            0x02 or 0x04 or 0x14 or 0x16 => Coordinate(v) is double c ? Dms(c) + (Ref((ushort)(tag - 1)) is { Length: > 0 } r ? " " + r : "") : null,
            0x05 => v.Bytes is [var a, ..] ? (a == 1 ? "Below sea level" : "Above sea level") : null,
            0x06 => d is double alt ? alt.ToString("0.#", Inv) + " m" + (ifd.TryGetValue(0x05, out var ar) && ar.Bytes is [1, ..] ? " below sea level" : "") : null,
            0x07 => TimeStamp(v),
            0x0C => Lookup(v.Text, SpeedUnits),
            0x0D => d is double s ? s.ToString("0.##", Inv) + (Ref(0x0C) is string u && SpeedUnits.TryGetValue(u, out var unit) ? " " + unit : "") : null,
            0x0E or 0x10 or 0x17 => Lookup(v.Text, new() { ["T"] = "True north", ["M"] = "Magnetic north" }),
            0x0F or 0x11 or 0x18 => d is double deg ? deg.ToString("0.##", Inv) + "°" : null,
            0x1B or 0x1C => EncodedText(v),
            0x1D => v.Text.Replace(':', '-'),
            0x1E => Lookup(v.FirstInteger, new() { [0] = "No correction", [1] = "Differential corrected" }),
            _ => null,
        };
    }

    /// <summary>"37.774900, -122.419400" from the GPS IFD's latitude and longitude, or null if either is missing.</summary>
    public static string? Position(IReadOnlyDictionary<ushort, TiffValue> gps)
    {
        double? Signed(ushort tag, ushort refTag, string negative)
        {
            if (!gps.TryGetValue(tag, out var v) || Coordinate(v) is not double c)
                return null;
            return gps.TryGetValue(refTag, out var r) && r.Text == negative ? -c : c;
        }
        return Signed(0x02, 0x01, "S") is double lat && Signed(0x04, 0x03, "W") is double lon
            ? $"{lat.ToString("0.000000", Inv)}, {lon.ToString("0.000000", Inv)}"
            : null;
    }

    private static string Generic(TiffValue v) => v.Type switch
    {
        TiffType.Ascii => v.Text,
        TiffType.Undefined when IsPrintable(v.Bytes) => Encoding.ASCII.GetString(v.Bytes).TrimEnd('\0').Trim(),
        TiffType.Undefined => v.Count <= 16 ? Convert.ToHexString(v.Bytes) : ByteSummary(v),
        TiffType.Byte or TiffType.SByte when v.Count > 16 => ByteSummary(v),
        TiffType.Rational or TiffType.SRational => List(v.Rationals.Select(r => r.ToString()), v.Count),
        TiffType.Float or TiffType.Double => List(v.Reals.Select(r => r.ToString("0.####", Inv)), v.Count),
        _ => List(v.Integers.Select(n => n.ToString(Inv)), v.Count),
    };

    private static string List(IEnumerable<string> values, int count) =>
        count <= MaxListedValues
            ? string.Join(", ", values)
            : string.Join(", ", values.Take(MaxListedValues)) + $", … ({count} values)";

    private static string ByteSummary(TiffValue v) => $"({v.ByteLength:N0} bytes)";

    private static bool IsPrintable(byte[] bytes) =>
        bytes.Length > 0 && bytes.AsSpan().TrimEnd((byte)0) is { Length: > 0 } s && s.IndexOfAnyExceptInRange((byte)0x20, (byte)0x7E) < 0;

    private static string Lookup(long? key, Dictionary<long, string> names) =>
        key is not long k ? "" : names.TryGetValue(k, out var name) ? name : $"Unknown ({k})";

    private static string Lookup(string key, Dictionary<string, string> names) =>
        names.TryGetValue(key, out var name) ? name : key;

    private static string? ExposureTime(double? seconds) => seconds switch
    {
        null or double.NaN => null,
        <= 0 => "0 s",
        < 0.25 => $"1/{Math.Round(1 / seconds.Value).ToString(Inv)} s",
        _ => seconds.Value.ToString("0.##", Inv) + " s",
    };

    private static string? FNumber(double? f) => f is double v && !double.IsNaN(v) ? "f/" + v.ToString("0.0", Inv) : null;

    /// <summary>"0232" → "2.32".</summary>
    private static string? Version(TiffValue v)
    {
        string text = Encoding.ASCII.GetString(v.Bytes);
        return text.Length == 4 && text.All(char.IsAsciiDigit)
            ? $"{int.Parse(text[..2], Inv)}.{text[2..]}"
            : null;
    }

    private static string ComponentsConfiguration(TiffValue v) =>
        string.Join(", ", v.Bytes.Select(b => b switch { 1 => "Y", 2 => "Cb", 3 => "Cr", 4 => "R", 5 => "G", 6 => "B", _ => "-" }));

    /// <summary>Flash is a bit field: fired, return light, mode, function present, red-eye reduction.</summary>
    internal static string Flash(long value)
    {
        var parts = new List<string> { (value & 1) != 0 ? "Fired" : "Did not fire" };
        switch ((value >> 3) & 3)
        {
            case 1: parts.Add("Forced on"); break;
            case 2: parts.Add("Forced off"); break;
            case 3: parts.Add("Auto"); break;
        }
        switch ((value >> 1) & 3)
        {
            case 2: parts.Add("Return not detected"); break;
            case 3: parts.Add("Return detected"); break;
        }
        if ((value & 0x20) != 0) parts.Add("No flash function");
        if ((value & 0x40) != 0) parts.Add("Red-eye reduction");
        return string.Join(", ", parts);
    }

    /// <summary>"24-70mm f/2.8-4" from the four Lens Info rationals.</summary>
    private static string? LensInfo(TiffValue v)
    {
        if (v.Rationals.Length != 4)
            return null;
        string Range(Rational lo, Rational hi, string fmt) =>
            !lo.IsValid ? "?"
            : !hi.IsValid || lo == hi ? lo.ToDouble().ToString(fmt, Inv)
            : $"{lo.ToDouble().ToString(fmt, Inv)}-{hi.ToDouble().ToString(fmt, Inv)}";
        return $"{Range(v.Rationals[0], v.Rationals[1], "0.#")}mm f/{Range(v.Rationals[2], v.Rationals[3], "0.0")}";
    }

    /// <summary>Text with an 8-byte character-code prefix (User Comment, GPS Processing Method).</summary>
    private static string EncodedText(TiffValue v)
    {
        if (v.Bytes.Length < 8)
            return Generic(v);
        string code = Encoding.ASCII.GetString(v.Bytes, 0, 8).TrimEnd('\0');
        var body = v.Bytes.AsSpan(8);
        string text = code switch
        {
            "UNICODE" => (v.BigEndian ? Encoding.BigEndianUnicode : Encoding.Unicode).GetString(body),
            _ => Encoding.UTF8.GetString(body),
        };
        return text.TrimEnd('\0', ' ');
    }

    /// <summary>
    /// "2024:05:01 12:34:56" → "2024-05-01 12:34:56", with sub-seconds and the UTC offset appended when the
    /// same IFD has them. Blank or odd values are shown as written.
    /// </summary>
    private static string? DateTime(TiffValue v, IReadOnlyDictionary<ushort, TiffValue> ifd, ushort subSecTag, ushort offsetTag)
    {
        string text = v.Text;
        if (text.Length != 19 || text[4] != ':' || text[7] != ':' || text[10] != ' ')
            return null;
        var result = new StringBuilder(text);
        result[4] = '-';
        result[7] = '-';
        if (ifd.TryGetValue(subSecTag, out var subSec) && subSec.Text is { Length: > 0 } fraction && fraction.All(char.IsAsciiDigit))
            result.Append('.').Append(fraction);
        if (ifd.TryGetValue(offsetTag, out var offset) && offset.Text is { Length: > 0 } zone)
            result.Append(' ').Append(zone);
        return result.ToString();
    }

    private static double? Coordinate(TiffValue v)
    {
        if (v.Rationals.Length != 3 || v.Rationals.Any(r => !r.IsValid))
            return null;
        return v.Rationals[0].ToDouble() + v.Rationals[1].ToDouble() / 60 + v.Rationals[2].ToDouble() / 3600;
    }

    private static string Dms(double degrees)
    {
        double deg = Math.Floor(degrees);
        double minutesTotal = (degrees - deg) * 60;
        double min = Math.Floor(minutesTotal);
        double sec = Math.Round((minutesTotal - min) * 60, 2);
        if (sec >= 60) { sec = 0; min++; }
        if (min >= 60) { min = 0; deg++; }
        return $"{deg.ToString(Inv)}° {min.ToString(Inv)}′ {sec.ToString("0.##", Inv)}″";
    }

    private static string? TimeStamp(TiffValue v)
    {
        if (v.Rationals.Length != 3 || v.Rationals.Any(r => !r.IsValid))
            return null;
        double h = v.Rationals[0].ToDouble(), m = v.Rationals[1].ToDouble(), s = v.Rationals[2].ToDouble();
        return $"{h.ToString("00", Inv)}:{m.ToString("00", Inv)}:{s.ToString("00.##", Inv)} UTC";
    }

    private static readonly Dictionary<long, string> Compression = new()
    {
        [1] = "Uncompressed", [2] = "CCITT 1D", [5] = "LZW", [6] = "JPEG (old-style)", [7] = "JPEG",
        [8] = "Adobe Deflate", [32773] = "PackBits", [34892] = "Lossy JPEG", [65535] = "Pentax PEF Compressed",
    };

    private static readonly Dictionary<long, string> Photometric = new()
    {
        [0] = "WhiteIsZero", [1] = "BlackIsZero", [2] = "RGB", [3] = "RGB Palette", [5] = "CMYK",
        [6] = "YCbCr", [8] = "CIELab", [32803] = "Color Filter Array", [34892] = "Linear Raw",
    };

    private static readonly Dictionary<long, string> Orientations = new()
    {
        [1] = "Horizontal (normal)", [2] = "Mirror horizontal", [3] = "Rotate 180", [4] = "Mirror vertical",
        [5] = "Mirror horizontal and rotate 270 CW", [6] = "Rotate 90 CW", [7] = "Mirror horizontal and rotate 90 CW",
        [8] = "Rotate 270 CW",
    };

    private static readonly Dictionary<long, string> ExposurePrograms = new()
    {
        [0] = "Not defined", [1] = "Manual", [2] = "Program AE", [3] = "Aperture-priority AE",
        [4] = "Shutter speed priority AE", [5] = "Creative (slow speed)", [6] = "Action (high speed)",
        [7] = "Portrait", [8] = "Landscape", [9] = "Bulb",
    };

    private static readonly Dictionary<long, string> SensitivityTypes = new()
    {
        [0] = "Unknown", [1] = "Standard Output Sensitivity", [2] = "Recommended Exposure Index", [3] = "ISO Speed",
        [4] = "Standard Output Sensitivity and Recommended Exposure Index", [5] = "Standard Output Sensitivity and ISO Speed",
        [6] = "Recommended Exposure Index and ISO Speed", [7] = "Standard Output Sensitivity, Recommended Exposure Index and ISO Speed",
    };

    private static readonly Dictionary<long, string> MeteringModes = new()
    {
        [0] = "Unknown", [1] = "Average", [2] = "Center-weighted average", [3] = "Spot", [4] = "Multi-spot",
        [5] = "Multi-segment", [6] = "Partial", [255] = "Other",
    };

    private static readonly Dictionary<long, string> LightSources = new()
    {
        [0] = "Unknown", [1] = "Daylight", [2] = "Fluorescent", [3] = "Tungsten (incandescent)", [4] = "Flash",
        [9] = "Fine weather", [10] = "Cloudy", [11] = "Shade", [12] = "Daylight fluorescent", [13] = "Day white fluorescent",
        [14] = "Cool white fluorescent", [15] = "White fluorescent", [16] = "Warm white fluorescent",
        [17] = "Standard light A", [18] = "Standard light B", [19] = "Standard light C", [20] = "D55", [21] = "D65",
        [22] = "D75", [23] = "D50", [24] = "ISO studio tungsten", [255] = "Other",
    };

    private static readonly Dictionary<long, string> SensingMethods = new()
    {
        [1] = "Not defined", [2] = "One-chip color area", [3] = "Two-chip color area", [4] = "Three-chip color area",
        [5] = "Color sequential area", [7] = "Trilinear", [8] = "Color sequential linear",
    };

    private static readonly Dictionary<string, string> SpeedUnits = new()
    {
        ["K"] = "km/h", ["M"] = "mph", ["N"] = "knots",
    };
}
