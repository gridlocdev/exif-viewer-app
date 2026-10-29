using System.Globalization;
using System.Text;
using ExifViewer.Core.Binary;
using ExifViewer.Core.Tiff;
using ExifViewer.Core.Xmp;

namespace ExifViewer.Core.Containers;

/// <summary>Reads WebP RIFF chunks: canvas and bitstream headers, EXIF and XMP.</summary>
internal static class WebPReader
{
    public static bool HasHeader(ReadOnlySpan<byte> s) => s.Length >= 12 && s.StartsWith("RIFF"u8) && s[8..12].SequenceEqual("WEBP"u8);

    public static void Read(ReadOnlyMemory<byte> data, ReportBuilder report)
    {
        var r = new EndianReader(data, bigEndian: false);
        long pos = 12;
        try
        {
            while (r.Has(pos, 8))
            {
                string fourCc = Encoding.ASCII.GetString(r.Span(pos, 4));
                uint size = r.U32(pos + 4);
                ReadChunk(fourCc, r.Slice(pos + 8, size), report);
                pos += 8 + size + (size & 1);
            }
        }
        catch (MalformedDataException e)
        {
            report.Warn("WebP: " + e.Message);
        }
    }

    private static void ReadChunk(string fourCc, ReadOnlyMemory<byte> chunk, ReportBuilder report)
    {
        var r = new EndianReader(chunk, bigEndian: false);
        var s = chunk.Span;
        switch (fourCc)
        {
            case "VP8X" when s.Length >= 10:
                report.Add("WebP", "Canvas Width", (1 + U24(s[4..])).ToString(CultureInfo.InvariantCulture));
                report.Add("WebP", "Canvas Height", (1 + U24(s[7..])).ToString(CultureInfo.InvariantCulture));
                report.Add("WebP", "Animated", (s[0] & 0x02) != 0 ? "Yes" : "No");
                report.Add("WebP", "Alpha", (s[0] & 0x10) != 0 ? "Yes" : "No");
                break;
            case "VP8 " when s.Length >= 10 && s[3..6].SequenceEqual((ReadOnlySpan<byte>)[0x9D, 0x01, 0x2A]):
                report.Add("WebP", "Encoding", "Lossy (VP8)");
                report.Add("WebP", "Image Width", (r.U16(6) & 0x3FFF).ToString(CultureInfo.InvariantCulture));
                report.Add("WebP", "Image Height", (r.U16(8) & 0x3FFF).ToString(CultureInfo.InvariantCulture));
                break;
            case "VP8L" when s.Length >= 5 && s[0] == 0x2F:
                uint bits = r.U32(1);
                report.Add("WebP", "Encoding", "Lossless (VP8L)");
                report.Add("WebP", "Image Width", ((bits & 0x3FFF) + 1).ToString(CultureInfo.InvariantCulture));
                report.Add("WebP", "Image Height", (((bits >> 14) & 0x3FFF) + 1).ToString(CultureInfo.InvariantCulture));
                break;
            case "EXIF":
                TiffReader.ReadExifBlock(chunk, report);
                break;
            case "XMP ":
                XmpReader.Read(s, report);
                break;
        }
    }

    private static int U24(ReadOnlySpan<byte> s) => s[0] | s[1] << 8 | s[2] << 16;
}
