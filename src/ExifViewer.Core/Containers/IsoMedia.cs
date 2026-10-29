using System.Buffers.Binary;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using ExifViewer.Core.Binary;

namespace ExifViewer.Core.Containers;

/// <summary>One box of an ISO base media file. The body runs from <see cref="BodyOffset"/> to <see cref="End"/>.</summary>
internal readonly record struct IsoBox(string Type, long Offset, long BodyOffset, long End);

/// <summary>
/// Shared pieces for ISO base media files (HEIF/HEIC/AVIF, MP4, QuickTime MOV): detection, box walking,
/// brands, and loading a large movie without its media data.
/// </summary>
internal static partial class IsoMedia
{
    private static readonly HashSet<string> FirstBoxTypes = ["ftyp", "moov", "mdat", "wide", "free", "skip", "pnot"];
    private static readonly HashSet<string> HeifBrands = ["mif1", "msf1", "heic", "heix", "heim", "heis", "hevc", "hevx", "avif", "avis"];

    /// <summary>Starts with a box whose type can begin an ISO media or QuickTime file.</summary>
    public static bool HasHeader(ReadOnlySpan<byte> s) =>
        s.Length >= 8 && FirstBoxTypes.Contains(Encoding.Latin1.GetString(s[4..8]))
        && BinaryPrimitives.ReadUInt32BigEndian(s) is 0 or 1 or >= 8;

    /// <summary>Major brand followed by the compatible brands, from a leading 'ftyp' box.</summary>
    public static List<string> Brands(ReadOnlySpan<byte> s)
    {
        if (s.Length < 16 || !s[4..8].SequenceEqual("ftyp"u8))
            return [];
        int end = (int)Math.Min(BinaryPrimitives.ReadUInt32BigEndian(s), (uint)s.Length);
        var brands = new List<string> { Encoding.Latin1.GetString(s[8..12]) };
        for (int at = 16; at + 4 <= end; at += 4)
            brands.Add(Encoding.Latin1.GetString(s.Slice(at, 4)));
        return brands;
    }

    /// <summary>HEIF still images (HEIC, AVIF) as opposed to movies, judged by the ftyp brands.</summary>
    public static bool IsHeif(ReadOnlySpan<byte> s) => Brands(s).Any(HeifBrands.Contains);

    public static string DescribeFormat(ReadOnlySpan<byte> s, string fileName)
    {
        var brands = Brands(s);
        if (brands.Count == 0)
            return "QuickTime MOV";
        if (brands.Any(b => b is "heic" or "heix" or "heim" or "heis"))
            return "HEIC (HEIF, HEVC)";
        if (brands.Any(b => b is "avif" or "avis"))
            return "AVIF";
        if (IsHeif(s))
            return "HEIF";
        return brands[0] switch
        {
            "qt  " => "QuickTime MOV",
            "M4V " or "M4VH" or "M4VP" => "M4V (MP4 video)",
            "M4A " or "M4B " => "M4A (MP4 audio)",
            "crx " => "CR3 (Canon RAW)",
            "3gp4" or "3gp5" or "3gp6" or "3g2a" => "3GP",
            _ when Path.GetExtension(fileName).Equals(".mov", StringComparison.OrdinalIgnoreCase) => "QuickTime MOV",
            _ => "MP4",
        };
    }

    /// <summary>The boxes between <paramref name="start"/> and <paramref name="end"/>, in order.</summary>
    public static IEnumerable<IsoBox> Children(EndianReader r, long start, long end)
    {
        long pos = start;
        while (pos + 8 <= end)
        {
            long size = r.U32(pos);
            string type = Encoding.Latin1.GetString(r.Span(pos + 4, 4));
            long header = 8;
            if (size == 1)
            {
                size = (long)Math.Min(r.U64(pos + 8), long.MaxValue);
                header = 16;
            }
            else if (size == 0)
            {
                size = end - pos;
            }
            if (size < header || size > end - pos)
                throw new MalformedDataException($"Box '{type}' at offset {pos} has size {size}, past its parent's end.");
            if (type == "uuid")
                header += 16;
            yield return new IsoBox(type, pos, pos + header, pos + size);
            pos += size;
        }
    }

    public static IEnumerable<IsoBox> Children(EndianReader r, IsoBox parent, int skip = 0) =>
        Children(r, parent.BodyOffset + skip, parent.End);

    /// <summary>The first child of <paramref name="parent"/> with this type, following a path ("mdia", "minf", "stbl").</summary>
    public static IsoBox? Find(EndianReader r, IsoBox parent, params string[] path)
    {
        IsoBox current = parent;
        foreach (string type in path)
        {
            IsoBox? next = null;
            foreach (var child in Children(r, current))
            {
                if (child.Type == type)
                {
                    next = child;
                    break;
                }
            }
            if (next is null)
                return null;
            current = next.Value;
        }
        return current;
    }

    public static void ReadBrands(EndianReader r, IsoBox ftyp, ReportBuilder report, string directory)
    {
        if (ftyp.End - ftyp.BodyOffset < 8)
            return;
        report.Add(directory, "Major Brand", Encoding.Latin1.GetString(r.Span(ftyp.BodyOffset, 4)).Trim());
        var compatible = new List<string>();
        for (long at = ftyp.BodyOffset + 8; at + 4 <= ftyp.End; at += 4)
            compatible.Add(Encoding.Latin1.GetString(r.Span(at, 4)).Trim());
        if (compatible.Count > 0)
            report.Add(directory, "Compatible Brands", string.Join(", ", compatible));
    }

    /// <summary>A NUL-terminated UTF-8 string starting at <paramref name="pos"/>; advances past the NUL.</summary>
    public static string CString(EndianReader r, ref long pos, long end)
    {
        long start = pos;
        while (pos < end && r.U8(pos) != 0)
            pos++;
        string text = Encoding.UTF8.GetString(r.Span(start, pos - start));
        if (pos < end)
            pos++;
        return text;
    }

    /// <summary>
    /// Copies the file's top-level boxes into memory, leaving out the payload of 'mdat' boxes (and of any
    /// box over <paramref name="maxBoxBytes"/>), so a multi-gigabyte video loads only its few-KB structure.
    /// Skipped boxes stay in the copy as empty boxes of the same type.
    /// </summary>
    public static byte[] LoadWithoutMediaData(Stream stream, long maxBoxBytes = 256L * 1024 * 1024)
    {
        using var output = new MemoryStream();
        long length = stream.Length, pos = 0;
        Span<byte> header = stackalloc byte[16];
        Span<byte> empty = stackalloc byte[8];
        while (pos + 8 <= length)
        {
            stream.Position = pos;
            stream.ReadExactly(header[..8]);
            long size = BinaryPrimitives.ReadUInt32BigEndian(header);
            int headerLength = 8;
            if (size == 1 && pos + 16 <= length)
            {
                stream.ReadExactly(header[8..16]);
                size = (long)Math.Min(BinaryPrimitives.ReadUInt64BigEndian(header[8..]), long.MaxValue);
                headerLength = 16;
            }
            else if (size == 0)
            {
                size = length - pos;
            }
            if (size < headerLength || size > length - pos)
                break;

            bool skip = header[4..8].SequenceEqual("mdat"u8) || size > maxBoxBytes;
            if (skip)
            {
                BinaryPrimitives.WriteUInt32BigEndian(empty, 8);
                header[4..8].CopyTo(empty[4..]);
                output.Write(empty);
            }
            else
            {
                stream.Position = pos;
                var box = new byte[size];
                stream.ReadExactly(box);
                output.Write(box);
            }
            pos += size;
        }
        return output.ToArray();
    }

    /// <summary>"+37.7749-122.4194+012.345/" (ISO 6709, decimal degrees) → position and altitude tags.</summary>
    public static void AddIso6709(ReportBuilder report, string directory, string text)
    {
        var m = Iso6709Regex().Match(text.Trim());
        if (!m.Success)
            return;
        double lat = double.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
        double lon = double.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture);
        if (Math.Abs(lat) > 90 || Math.Abs(lon) > 180)
            return;
        report.Add(directory, "Position", $"{lat.ToString("0.000000", CultureInfo.InvariantCulture)}, {lon.ToString("0.000000", CultureInfo.InvariantCulture)}");
        if (m.Groups[3].Success)
            report.Add(directory, "Altitude", double.Parse(m.Groups[3].Value, CultureInfo.InvariantCulture).ToString("0.#", CultureInfo.InvariantCulture) + " m");
    }

    [GeneratedRegex(@"^([+-]\d+(?:\.\d+)?)([+-]\d+(?:\.\d+)?)([+-]\d+(?:\.\d+)?)?")]
    private static partial Regex Iso6709Regex();
}
