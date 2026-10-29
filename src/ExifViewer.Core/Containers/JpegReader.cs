using System.Text;
using ExifViewer.Core.Binary;
using ExifViewer.Core.Tiff;
using ExifViewer.Core.Xmp;

namespace ExifViewer.Core.Containers;

/// <summary>Reads JPEG marker segments up to the start of scan: JFIF, EXIF (APP1), XMP (APP1), comments, and the frame header.</summary>
internal static class JpegReader
{
    private const byte StartOfScan = 0xDA, EndOfImage = 0xD9;
    private static readonly byte[] XmpSignature = "http://ns.adobe.com/xap/1.0/\0"u8.ToArray();

    public static bool HasHeader(ReadOnlySpan<byte> s) => s is [0xFF, 0xD8, 0xFF, ..];

    public static void Read(ReadOnlyMemory<byte> data, ReportBuilder report)
    {
        try
        {
            foreach (var (marker, payload) in Segments(data))
                ReadSegment(marker, payload, report);
        }
        catch (MalformedDataException e)
        {
            report.Warn("JPEG: " + e.Message);
        }
    }

    /// <summary>True for baseline, extended or progressive JPEGs: the kinds an ordinary image decoder can show.</summary>
    public static bool IsDisplayable(ReadOnlySpan<byte> data)
    {
        if (!HasHeader(data))
            return false;
        try
        {
            foreach (var (marker, _) in Segments(data.ToArray()))
                if (IsFrameHeader(marker))
                    return marker is 0xC0 or 0xC1 or 0xC2;
        }
        catch (MalformedDataException)
        {
        }
        return false;
    }

    /// <summary>Marker segments in file order, ending with the start-of-scan segment.</summary>
    private static IEnumerable<(byte Marker, ReadOnlyMemory<byte> Payload)> Segments(ReadOnlyMemory<byte> data)
    {
        var r = new EndianReader(data, bigEndian: true);
        long pos = 2;
        while (r.Has(pos, 2))
        {
            if (r.U8(pos) != 0xFF)
                throw new MalformedDataException($"Expected a marker at offset {pos}.");
            byte marker = r.U8(pos + 1);
            if (marker == 0xFF)
            {
                pos++;
                continue;
            }
            if (marker is 0x01 or >= 0xD0 and <= 0xD7)
            {
                pos += 2;
                continue;
            }
            if (marker == EndOfImage)
                yield break;

            int length = r.U16(pos + 2);
            if (length < 2)
                throw new MalformedDataException($"Segment 0x{marker:X2} at offset {pos} has length {length}.");
            yield return (marker, r.Slice(pos + 4, length - 2));
            if (marker == StartOfScan)
                yield break;
            pos += 2 + length;
        }
    }

    private static bool IsFrameHeader(byte marker) => marker is >= 0xC0 and <= 0xCF and not (0xC4 or 0xC8 or 0xCC);

    private static void ReadSegment(byte marker, ReadOnlyMemory<byte> payload, ReportBuilder report)
    {
        var s = payload.Span;
        switch (marker)
        {
            case 0xE0 when s.StartsWith("JFIF\0"u8) && s.Length >= 12:
                report.Add("JFIF", "Version", $"{s[5]}.{s[6]:00}");
                report.Add("JFIF", "Resolution Unit", s[7] switch { 1 => "inches", 2 => "cm", _ => "None" });
                report.Add("JFIF", "X Resolution", (s[8] << 8 | s[9]).ToString(System.Globalization.CultureInfo.InvariantCulture));
                report.Add("JFIF", "Y Resolution", (s[10] << 8 | s[11]).ToString(System.Globalization.CultureInfo.InvariantCulture));
                break;
            case 0xE1 when s.StartsWith("Exif\0\0"u8):
                TiffReader.Read(payload[6..], report);
                break;
            case 0xE1 when s.StartsWith(XmpSignature):
                XmpReader.Read(s[XmpSignature.Length..], report);
                break;
            case 0xFE:
                report.Add("JPEG", "Comment", Encoding.UTF8.GetString(s).TrimEnd('\0'));
                break;
            case var m when IsFrameHeader(m) && s.Length >= 6:
                report.Add("JPEG", "Encoding", Encodings.TryGetValue(m, out var name) ? name : $"SOF{m - 0xC0}");
                report.Add("JPEG", "Image Width", (s[3] << 8 | s[4]).ToString(System.Globalization.CultureInfo.InvariantCulture));
                report.Add("JPEG", "Image Height", (s[1] << 8 | s[2]).ToString(System.Globalization.CultureInfo.InvariantCulture));
                report.Add("JPEG", "Bits Per Sample", s[0].ToString(System.Globalization.CultureInfo.InvariantCulture));
                report.Add("JPEG", "Color Components", s[5].ToString(System.Globalization.CultureInfo.InvariantCulture));
                break;
        }
    }

    private static readonly Dictionary<byte, string> Encodings = new()
    {
        [0xC0] = "Baseline DCT, Huffman",
        [0xC1] = "Extended sequential DCT, Huffman",
        [0xC2] = "Progressive DCT, Huffman",
        [0xC3] = "Lossless, Huffman",
        [0xC5] = "Differential sequential DCT, Huffman",
        [0xC6] = "Differential progressive DCT, Huffman",
        [0xC7] = "Differential lossless, Huffman",
        [0xC9] = "Extended sequential DCT, arithmetic",
        [0xCA] = "Progressive DCT, arithmetic",
        [0xCB] = "Lossless, arithmetic",
        [0xCD] = "Differential sequential DCT, arithmetic",
        [0xCE] = "Differential progressive DCT, arithmetic",
        [0xCF] = "Differential lossless, arithmetic",
    };
}
