using System.Globalization;
using ExifViewer.Core.Binary;
using ExifViewer.Core.Containers;
using ExifViewer.Core.Tiff;

namespace ExifViewer.Core;

/// <summary>Entry point: detects the container from its first bytes and runs the matching reader.</summary>
public static class MetadataReader
{
    public static MetadataReport Read(string path)
    {
        var info = new FileInfo(path);
        return Read(File.ReadAllBytes(path), info.Name, info.LastWriteTime);
    }

    public static MetadataReport Read(ReadOnlyMemory<byte> data, string fileName, DateTime? modified = null)
    {
        var report = new ReportBuilder();
        var s = data.Span;
        string format = DescribeFormat(s, fileName);

        report.Add("File", "File Name", fileName);
        report.Add("File", "File Size", FormatSize(data.Length));
        report.Add("File", "Format", format);
        if (modified is DateTime m)
            report.Add("File", "Modified", m.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));

        try
        {
            if (JpegReader.HasHeader(s))
                JpegReader.Read(data, report);
            else if (PngReader.HasHeader(s))
                PngReader.Read(data, report);
            else if (WebPReader.HasHeader(s))
                WebPReader.Read(data, report);
            else if (TiffReader.HasHeader(s))
                TiffReader.Read(data, report);
            else if (IsIsoMedia(s))
                report.Warn("HEIF, AVIF, CR3 and MP4/MOV files aren't read yet.");
            else
                report.Warn("Not a format ExifViewer can read.");
        }
        catch (MalformedDataException e)
        {
            report.Warn(e.Message);
        }

        return report.Build(fileName, format);
    }

    private static string DescribeFormat(ReadOnlySpan<byte> s, string fileName)
    {
        if (JpegReader.HasHeader(s)) return "JPEG";
        if (PngReader.HasHeader(s)) return "PNG";
        if (WebPReader.HasHeader(s)) return "WebP";
        if (TiffReader.HasHeader(s))
        {
            return Path.GetExtension(fileName).ToLowerInvariant() switch
            {
                ".dng" => "DNG (Adobe Digital Negative)",
                ".cr2" => "CR2 (Canon RAW)",
                ".nef" or ".nrw" => "NEF (Nikon RAW)",
                ".arw" or ".srf" or ".sr2" => "ARW (Sony RAW)",
                ".orf" => "ORF (Olympus RAW)",
                ".rw2" => "RW2 (Panasonic RAW)",
                ".pef" => "PEF (Pentax RAW)",
                _ => "TIFF",
            };
        }
        if (IsIsoMedia(s)) return "ISO media (" + System.Text.Encoding.ASCII.GetString(s[8..12]).Trim() + ")";
        return "Unknown";
    }

    /// <summary>ISO base media files (HEIC, AVIF, CR3, MP4, MOV) start with an "ftyp" box.</summary>
    private static bool IsIsoMedia(ReadOnlySpan<byte> s) => s.Length >= 12 && s[4..8].SequenceEqual("ftyp"u8);

    internal static string FormatSize(long bytes)
    {
        string[] units = ["bytes", "KB", "MB", "GB"];
        double size = bytes;
        int unit = 0;
        while (size >= 1024 && unit < units.Length - 1)
        {
            size /= 1024;
            unit++;
        }
        string exact = bytes.ToString("N0", CultureInfo.InvariantCulture) + " bytes";
        return unit == 0 ? exact : $"{size.ToString("0.#", CultureInfo.InvariantCulture)} {units[unit]} ({exact})";
    }
}
