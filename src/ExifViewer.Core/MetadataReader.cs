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
        using var stream = info.OpenRead();
        var head = new byte[Math.Min(4096, info.Length)];
        stream.ReadExactly(head);
        stream.Position = 0;
        // Movies can be gigabytes, almost all of it media data; read just their structure.
        if (IsoMedia.HasHeader(head) && !IsoMedia.IsHeif(head))
            return Read(IsoMedia.LoadWithoutMediaData(stream), info.Name, info.LastWriteTime, info.Length);

        if (info.Length > Array.MaxLength)
            throw new IOException($"The file is {FormatSize(info.Length)}; only videos larger than 2 GB can be read.");
        var data = new byte[info.Length];
        stream.ReadExactly(data);
        return Read(data, info.Name, info.LastWriteTime);
    }

    /// <param name="fileSize">The file's real size, when <paramref name="data"/> leaves parts of it out.</param>
    public static MetadataReport Read(ReadOnlyMemory<byte> data, string fileName, DateTime? modified = null, long? fileSize = null)
    {
        var report = new ReportBuilder();
        var s = data.Span;
        string format = DescribeFormat(s, fileName);

        report.Add("File", "File Name", fileName);
        report.Add("File", "File Size", FormatSize(fileSize ?? data.Length));
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
            else if (IsoMedia.HasHeader(s) && IsoMedia.IsHeif(s))
                HeifReader.Read(data, report);
            else if (IsoMedia.HasHeader(s))
                QuickTimeReader.Read(data, report);
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
        if (IsoMedia.HasHeader(s)) return IsoMedia.DescribeFormat(s, fileName);
        return "Unknown";
    }

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
