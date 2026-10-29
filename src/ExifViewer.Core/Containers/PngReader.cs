using System.Globalization;
using System.IO.Compression;
using System.Text;
using ExifViewer.Core.Binary;
using ExifViewer.Core.Tiff;
using ExifViewer.Core.Xmp;

namespace ExifViewer.Core.Containers;

/// <summary>Reads PNG chunks: the header, eXIf, text chunks (with XMP in iTXt), modification time and pixel density.</summary>
internal static class PngReader
{
    private const string XmpKeyword = "XML:com.adobe.xmp";
    private const int MaxInflatedBytes = 16 * 1024 * 1024;

    public static bool HasHeader(ReadOnlySpan<byte> s) => s.StartsWith((ReadOnlySpan<byte>)[0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);

    public static void Read(ReadOnlyMemory<byte> data, ReportBuilder report)
    {
        var r = new EndianReader(data, bigEndian: true);
        long pos = 8;
        try
        {
            while (r.Has(pos, 8))
            {
                uint length = r.U32(pos);
                string type = Encoding.ASCII.GetString(r.Span(pos + 4, 4));
                var chunk = r.Slice(pos + 8, length);
                if (type == "IEND")
                    return;
                ReadChunk(type, chunk, report);
                pos += 12 + length;
            }
        }
        catch (MalformedDataException e)
        {
            report.Warn("PNG: " + e.Message);
        }
    }

    private static void ReadChunk(string type, ReadOnlyMemory<byte> chunk, ReportBuilder report)
    {
        var r = new EndianReader(chunk, bigEndian: true);
        var s = chunk.Span;
        switch (type)
        {
            case "IHDR" when s.Length >= 13:
                report.Add("PNG", "Image Width", r.U32(0).ToString(CultureInfo.InvariantCulture));
                report.Add("PNG", "Image Height", r.U32(4).ToString(CultureInfo.InvariantCulture));
                report.Add("PNG", "Bit Depth", s[8].ToString(CultureInfo.InvariantCulture));
                report.Add("PNG", "Color Type", s[9] switch
                {
                    0 => "Grayscale", 2 => "RGB", 3 => "Palette", 4 => "Grayscale with alpha", 6 => "RGB with alpha",
                    _ => $"Unknown ({s[9]})",
                });
                report.Add("PNG", "Interlace", s[12] == 1 ? "Adam7" : "None");
                break;
            case "eXIf":
                TiffReader.ReadExifBlock(chunk, report);
                break;
            case "tEXt":
                if (SplitKeyword(s) is { } text)
                    AddText(text.Keyword, Encoding.Latin1.GetString(text.Body), report);
                break;
            case "zTXt":
                if (SplitKeyword(s) is { Body.Length: > 1 } ztext)
                    AddText(ztext.Keyword, Encoding.Latin1.GetString(Inflate(ztext.Body.AsSpan(1))), report);
                break;
            case "iTXt":
                ReadInternationalText(s, report);
                break;
            case "tIME" when s.Length >= 7:
                report.Add("PNG", "Last Modified",
                    $"{r.U16(0):0000}:{s[2]:00}:{s[3]:00} {s[4]:00}:{s[5]:00}:{s[6]:00}");
                break;
            case "pHYs" when s.Length >= 9:
                uint x = r.U32(0), y = r.U32(4);
                if (s[8] == 1)
                {
                    report.Add("PNG", "X Resolution", $"{Math.Round(x * 0.0254).ToString(CultureInfo.InvariantCulture)} dpi");
                    report.Add("PNG", "Y Resolution", $"{Math.Round(y * 0.0254).ToString(CultureInfo.InvariantCulture)} dpi");
                }
                else
                {
                    report.Add("PNG", "Pixel Aspect Ratio", $"{x}:{y}");
                }
                break;
        }
    }

    /// <summary>iTXt: keyword, compression flag and method, language tag, translated keyword, then UTF-8 text.</summary>
    private static void ReadInternationalText(ReadOnlySpan<byte> s, ReportBuilder report)
    {
        if (SplitKeyword(s) is not { Body.Length: >= 2 } split)
            return;
        string keyword = split.Keyword;
        ReadOnlySpan<byte> rest = split.Body;
        bool compressed = rest[0] == 1;
        rest = rest[2..];
        for (int skip = 0; skip < 2; skip++)
        {
            int nul = rest.IndexOf((byte)0);
            if (nul < 0)
                return;
            rest = rest[(nul + 1)..];
        }
        var text = compressed ? Inflate(rest) : rest.ToArray();
        if (keyword == XmpKeyword)
            XmpReader.Read(text, report);
        else
            AddText(keyword, Encoding.UTF8.GetString(text), report);
    }

    private static void AddText(string keyword, string text, ReportBuilder report)
    {
        if (keyword == XmpKeyword)
            XmpReader.Read(Encoding.UTF8.GetBytes(text), report);
        else
            report.Add("PNG Text", keyword, text);
    }

    private static (string Keyword, byte[] Body)? SplitKeyword(ReadOnlySpan<byte> s)
    {
        int nul = s.IndexOf((byte)0);
        return nul <= 0 ? null : (Encoding.Latin1.GetString(s[..nul]), s[(nul + 1)..].ToArray());
    }

    private static byte[] Inflate(ReadOnlySpan<byte> compressed)
    {
        try
        {
            using var input = new ZLibStream(new MemoryStream(compressed.ToArray()), CompressionMode.Decompress);
            using var output = new MemoryStream();
            var buffer = new byte[81920];
            int read;
            while ((read = input.Read(buffer)) > 0)
            {
                if (output.Length + read > MaxInflatedBytes)
                    throw new MalformedDataException($"Compressed text inflates past {MaxInflatedBytes / (1024 * 1024)} MB.");
                output.Write(buffer, 0, read);
            }
            return output.ToArray();
        }
        catch (InvalidDataException e)
        {
            throw new MalformedDataException("Compressed text chunk: " + e.Message);
        }
    }
}
