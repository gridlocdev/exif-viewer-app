using ExifViewer.Core.Binary;
using ExifViewer.Core.Xmp;

namespace ExifViewer.Core.Tiff;

/// <summary>
/// Walks a TIFF structure: the IFD chain (main image, thumbnail), the Exif, GPS and Interop IFDs it
/// points to, and RAW SubIFDs. Used for EXIF blocks inside JPEG/PNG/WebP and for TIFF-based files
/// (TIFF, DNG, CR2, NEF, ARW, ORF, RW2, PEF), where the whole file is one TIFF structure.
/// </summary>
internal sealed class TiffReader
{
    private const ushort ExifPointer = 0x8769, GpsPointer = 0x8825, InteropPointer = 0xA005, SubIfds = 0x014A;
    private const int MaxIfdChain = 8;

    private readonly EndianReader r;
    private readonly ReportBuilder report;
    private readonly HashSet<long> visited = [];

    private TiffReader(EndianReader r, ReportBuilder report)
    {
        this.r = r;
        this.report = report;
    }

    /// <summary>"II*\0", "MM\0*", and the Olympus ("IIRO", "IIRS") and Panasonic ("IIU\0") variants.</summary>
    public static bool HasHeader(ReadOnlySpan<byte> s)
    {
        if (s.Length < 4 || s[0] != s[1] || (s[0] != 'I' && s[0] != 'M'))
            return false;
        ushort magic = s[0] == 'I' ? (ushort)(s[2] | s[3] << 8) : (ushort)(s[2] << 8 | s[3]);
        return magic is 42 or 0x4F52 or 0x5352 or 0x55;
    }

    /// <summary>Reads an EXIF block, skipping the "Exif\0\0" prefix some containers keep in front of it.</summary>
    public static void ReadExifBlock(ReadOnlyMemory<byte> block, ReportBuilder report)
    {
        if (block.Span.StartsWith("Exif\0\0"u8))
            block = block[6..];
        Read(block, report);
    }

    public static void Read(ReadOnlyMemory<byte> tiff, ReportBuilder report)
    {
        if (!HasHeader(tiff.Span))
        {
            report.Warn("EXIF block doesn't start with a TIFF header.");
            return;
        }
        var r = new EndianReader(tiff, bigEndian: tiff.Span[0] == 'M');
        try
        {
            new TiffReader(r, report).ReadChain(r.U32(4));
        }
        catch (MalformedDataException e)
        {
            report.Warn("EXIF: " + e.Message);
        }
    }

    private void ReadChain(long offset)
    {
        for (int index = 0; offset != 0 && index < MaxIfdChain; index++)
        {
            string name = index switch { 0 => "Image", 1 => "Thumbnail", _ => $"IFD{index}" };
            offset = ReadIfd(offset, name, TagSet.Tiff);
        }
    }

    /// <summary>Reads one IFD into a directory named <paramref name="name"/> and returns the next IFD's offset (0 for none).</summary>
    private long ReadIfd(long offset, string name, TagSet set)
    {
        if (!visited.Add(offset))
        {
            report.Warn($"{name}: IFD at offset {offset} was already read; stopped following a loop.");
            return 0;
        }

        ushort count;
        try
        {
            count = r.U16(offset);
        }
        catch (MalformedDataException e)
        {
            report.Warn($"{name}: {e.Message}");
            return 0;
        }

        var entries = new List<(ushort Tag, TiffValue Value)>(count);
        for (int i = 0; i < count; i++)
        {
            long at = offset + 2 + i * 12L;
            try
            {
                if (ReadEntry(at) is { } entry)
                    entries.Add(entry);
            }
            catch (MalformedDataException e)
            {
                report.Warn($"{name}: entry {i}: {e.Message}");
                if (!r.Has(at, 12))
                    break;
            }
        }
        long nextAt = offset + 2 + count * 12L;
        long next = r.Has(nextAt, 4) ? r.U32(nextAt) : 0;

        var byTag = new Dictionary<ushort, TiffValue>();
        foreach (var (tag, value) in entries)
            byTag.TryAdd(tag, value);

        var directory = report.GetDirectory(name);
        foreach (var (tag, value) in entries)
        {
            if (set == TagSet.Tiff && FollowPointer(tag, value, name))
                continue;
            if (set == TagSet.Tiff && tag == 0x02BC)
            {
                XmpReader.Read(value.Bytes, report);
                continue;
            }
            directory.Tags.Add(new MetadataTag(TagNames.Get(set, tag), TagFormatter.Format(set, tag, value, byTag), tag));
        }

        if (set == TagSet.Gps && TagFormatter.Position(byTag) is string position)
            directory.Tags.Add(new MetadataTag("Position", position));
        if (name == "Image" && byTag.TryGetValue(0x0112, out var orientation))
            report.Orientation = (int?)orientation.FirstInteger;
        if (set == TagSet.Tiff)
            OfferPreviews(byTag);
        return next;
    }

    private bool FollowPointer(ushort tag, TiffValue value, string from)
    {
        switch (tag)
        {
            case ExifPointer when value.FirstInteger is long exif:
                ReadIfd(exif, "Exif", TagSet.Tiff);
                return true;
            case GpsPointer when value.FirstInteger is long gps:
                ReadIfd(gps, "GPS", TagSet.Gps);
                return true;
            case InteropPointer when from == "Exif" && value.FirstInteger is long interop:
                ReadIfd(interop, "Interoperability", TagSet.Interop);
                return true;
            case SubIfds:
                foreach (long sub in value.Integers)
                    ReadIfd(sub, report.UniqueName("SubIFD"), TagSet.Tiff);
                return true;
            default:
                return false;
        }
    }

    /// <summary>Embedded JPEGs: the EXIF thumbnail, single-strip JPEG previews (CR2, DNG), and Panasonic's JpgFromRaw.</summary>
    private void OfferPreviews(Dictionary<ushort, TiffValue> ifd)
    {
        if (ifd.TryGetValue(0x0201, out var start) && ifd.TryGetValue(0x0202, out var length))
            OfferRange(start.FirstInteger, length.FirstInteger);

        if (ifd.TryGetValue(0x0103, out var compression) && compression.FirstInteger is 6 or 7
            && ifd.TryGetValue(0x0111, out var strips) && strips.Count == 1
            && ifd.TryGetValue(0x0117, out var stripBytes) && stripBytes.Count == 1)
            OfferRange(strips.FirstInteger, stripBytes.FirstInteger);

        if (ifd.TryGetValue(0x002E, out var jpgFromRaw))
            report.OfferPreview(jpgFromRaw.Bytes);
    }

    private void OfferRange(long? offset, long? length)
    {
        if (offset is long o && length is long n && n > 0 && r.Has(o, n))
            report.OfferPreview(r.Slice(o, n));
    }

    private (ushort Tag, TiffValue Value)? ReadEntry(long at)
    {
        ushort tag = r.U16(at);
        var type = (TiffType)r.U16(at + 2);
        uint count = r.U32(at + 4);
        int size = TiffValue.SizeOf(type);
        if (size == 0)
        {
            report.Warn($"Tag 0x{tag:X4} has unknown type {(ushort)type}; skipped.");
            return null;
        }

        long total = (long)count * size;
        long dataAt = total <= 4 ? at + 8 : r.U32(at + 8);
        var bytes = r.Span(dataAt, total);
        int n = (int)count;

        TiffValue Make(byte[]? raw = null, long[]? ints = null, Rational[]? rationals = null, double[]? reals = null) => new()
        {
            Type = type,
            Count = n,
            BigEndian = r.BigEndian,
            Bytes = raw ?? [],
            Integers = ints ?? [],
            Rationals = rationals ?? [],
            Reals = reals ?? [],
        };

        return (tag, type switch
        {
            TiffType.Ascii or TiffType.Undefined => Make(raw: bytes.ToArray()),
            TiffType.Byte => Make(raw: bytes.ToArray(), ints: Array.ConvertAll(bytes.ToArray(), b => (long)b)),
            TiffType.SByte => Make(raw: bytes.ToArray(), ints: Array.ConvertAll(bytes.ToArray(), b => (long)(sbyte)b)),
            TiffType.Short => Make(ints: Ints(n, i => r.U16(dataAt + i * 2))),
            TiffType.SShort => Make(ints: Ints(n, i => r.S16(dataAt + i * 2))),
            TiffType.Long or TiffType.Ifd => Make(ints: Ints(n, i => r.U32(dataAt + i * 4))),
            TiffType.SLong => Make(ints: Ints(n, i => r.S32(dataAt + i * 4))),
            TiffType.Rational => Make(rationals: Values(n, i => new Rational(r.U32(dataAt + i * 8), r.U32(dataAt + i * 8 + 4)))),
            TiffType.SRational => Make(rationals: Values(n, i => new Rational(r.S32(dataAt + i * 8), r.S32(dataAt + i * 8 + 4)))),
            TiffType.Float => Make(reals: Values(n, i => (double)r.F32(dataAt + i * 4))),
            _ => Make(reals: Values(n, i => r.F64(dataAt + i * 8))),
        });
    }

    private static T[] Values<T>(int count, Func<long, T> read)
    {
        var values = new T[count];
        for (int i = 0; i < count; i++)
            values[i] = read(i);
        return values;
    }

    private static long[] Ints(int count, Func<long, long> read) => Values(count, read);
}
