using System.Globalization;
using System.Text;
using ExifViewer.Core.Binary;
using ExifViewer.Core.Tiff;
using ExifViewer.Core.Xmp;

namespace ExifViewer.Core.Containers;

/// <summary>
/// Reads HEIF still images (HEIC, AVIF): the 'meta' box's items, where they're stored, and the properties
/// of the primary image, then hands the Exif and XMP items to their readers. Image data isn't decoded.
/// </summary>
internal static class HeifReader
{
    private const string Directory = "HEIF";

    private sealed record Item(uint Id, string Type, string? ContentType);
    private sealed record Extent(long Offset, long Length);
    private sealed record Location(int Method, long BaseOffset, List<Extent> Extents);

    public static void Read(ReadOnlyMemory<byte> data, ReportBuilder report)
    {
        var r = new EndianReader(data, bigEndian: true);
        try
        {
            IsoBox? meta = null;
            foreach (var box in IsoMedia.Children(r, 0, r.Length))
            {
                if (box.Type == "ftyp")
                    IsoMedia.ReadBrands(r, box, report, Directory);
                else if (box.Type == "meta")
                    meta = box;
            }
            if (meta is null)
                report.Warn("HEIF: no 'meta' box.");
            else
                ReadMeta(r, meta.Value, report);
        }
        catch (MalformedDataException e)
        {
            report.Warn("HEIF: " + e.Message);
        }
    }

    private static void ReadMeta(EndianReader r, IsoBox meta, ReportBuilder report)
    {
        uint primary = 0;
        var items = new Dictionary<uint, Item>();
        var locations = new Dictionary<uint, Location>();
        var properties = new List<IsoBox>();
        var associations = new Dictionary<uint, List<int>>();
        IsoBox? idat = null;

        foreach (var box in IsoMedia.Children(r, meta, skip: 4))
        {
            switch (box.Type)
            {
                case "pitm":
                    primary = r.U8(box.BodyOffset) == 0 ? r.U16(box.BodyOffset + 4) : r.U32(box.BodyOffset + 4);
                    break;
                case "iinf":
                    ReadItemInfo(r, box, items);
                    break;
                case "iloc":
                    ReadLocations(r, box, locations);
                    break;
                case "idat":
                    idat = box;
                    break;
                case "iprp":
                    ReadProperties(r, box, properties, associations);
                    break;
            }
        }

        if (items.TryGetValue(primary, out var primaryItem))
            report.Add(Directory, "Primary Image", ItemTypeName(primaryItem.Type));
        if (associations.TryGetValue(primary, out var indexes))
        {
            foreach (int index in indexes)
                if (index >= 1 && index <= properties.Count)
                    DescribeProperty(r, properties[index - 1], report);
        }
        report.Add(Directory, "Items", string.Join(", ", items.Values
            .GroupBy(i => i.Type)
            .OrderByDescending(g => g.Count())
            .Select(g => $"{ItemTypeName(g.Key)} × {g.Count()}")));

        var auxiliary = properties.Where(p => p.Type == "auxC").Select(p => AuxiliaryName(r, p)).Distinct().ToList();
        if (auxiliary.Count > 0)
            report.Add(Directory, "Auxiliary Images", string.Join(", ", auxiliary));

        foreach (var item in items.Values)
        {
            if (item.Type != "Exif" && !(item.Type == "mime" && item.ContentType == "application/rdf+xml"))
                continue;
            if (!locations.TryGetValue(item.Id, out var location))
            {
                report.Warn($"HEIF: item {item.Id} ({item.Type}) has no location.");
                continue;
            }
            var bytes = Extract(r, location, idat);
            if (item.Type == "mime")
            {
                XmpReader.Read(bytes, report);
            }
            else if (bytes.Length >= 4)
            {
                // The Exif item starts with the offset from the end of this field to the TIFF header.
                long tiffAt = 4 + (long)new EndianReader(bytes, bigEndian: true).U32(0);
                if (tiffAt < bytes.Length)
                    TiffReader.ReadExifBlock(bytes.AsMemory((int)tiffAt), report);
            }
        }
    }

    private static void ReadItemInfo(EndianReader r, IsoBox iinf, Dictionary<uint, Item> items)
    {
        long pos = iinf.BodyOffset;
        byte version = r.U8(pos);
        pos += 4 + (version == 0 ? 2 : 4);
        foreach (var infe in IsoMedia.Children(r, pos, iinf.End))
        {
            if (infe.Type != "infe")
                continue;
            byte v = r.U8(infe.BodyOffset);
            if (v < 2)
                continue;
            long p = infe.BodyOffset + 4;
            uint id = v == 2 ? r.U16(p) : r.U32(p);
            p += (v == 2 ? 2 : 4) + 2;
            string type = Encoding.Latin1.GetString(r.Span(p, 4));
            p += 4;
            IsoMedia.CString(r, ref p, infe.End);
            string? contentType = type == "mime" ? IsoMedia.CString(r, ref p, infe.End) : null;
            items[id] = new Item(id, type, contentType);
        }
    }

    private static void ReadLocations(EndianReader r, IsoBox iloc, Dictionary<uint, Location> locations)
    {
        long pos = iloc.BodyOffset;
        byte version = r.U8(pos);
        pos += 4;
        byte sizes = r.U8(pos), more = r.U8(pos + 1);
        pos += 2;
        int offsetSize = sizes >> 4, lengthSize = sizes & 0xF, baseSize = more >> 4;
        int indexSize = version is 1 or 2 ? more & 0xF : 0;
        uint count = version < 2 ? r.U16(pos) : r.U32(pos);
        pos += version < 2 ? 2 : 4;

        long Sized(int size)
        {
            long value = size switch
            {
                0 => 0,
                4 => r.U32(pos),
                8 => (long)Math.Min(r.U64(pos), long.MaxValue),
                _ => throw new MalformedDataException($"iloc field size {size} isn't 0, 4 or 8."),
            };
            pos += size;
            return value;
        }

        for (uint i = 0; i < count; i++)
        {
            uint id = version < 2 ? r.U16(pos) : r.U32(pos);
            pos += version < 2 ? 2 : 4;
            int method = 0;
            if (version is 1 or 2)
            {
                method = r.U16(pos) & 0xF;
                pos += 2;
            }
            pos += 2; // data reference index
            long baseOffset = Sized(baseSize);
            ushort extentCount = r.U16(pos);
            pos += 2;
            var extents = new List<Extent>(extentCount);
            for (int e = 0; e < extentCount; e++)
            {
                Sized(indexSize);
                long offset = Sized(offsetSize);
                extents.Add(new Extent(offset, Sized(lengthSize)));
            }
            locations[id] = new Location(method, baseOffset, extents);
        }
    }

    private static void ReadProperties(EndianReader r, IsoBox iprp, List<IsoBox> properties, Dictionary<uint, List<int>> associations)
    {
        foreach (var child in IsoMedia.Children(r, iprp))
        {
            if (child.Type == "ipco")
            {
                properties.AddRange(IsoMedia.Children(r, child));
            }
            else if (child.Type == "ipma")
            {
                long p = child.BodyOffset;
                byte version = r.U8(p);
                bool wideIndex = (r.U32(p) & 1) != 0;
                p += 4;
                uint entries = r.U32(p);
                p += 4;
                for (uint e = 0; e < entries; e++)
                {
                    uint id = version < 1 ? r.U16(p) : r.U32(p);
                    p += version < 1 ? 2 : 4;
                    int count = r.U8(p++);
                    var list = associations.TryGetValue(id, out var existing) ? existing : associations[id] = [];
                    for (int a = 0; a < count; a++)
                    {
                        list.Add(wideIndex ? r.U16(p) & 0x7FFF : r.U8(p) & 0x7F);
                        p += wideIndex ? 2 : 1;
                    }
                }
            }
        }
    }

    private static void DescribeProperty(EndianReader r, IsoBox property, ReportBuilder report)
    {
        long body = property.BodyOffset;
        switch (property.Type)
        {
            case "ispe":
                report.Add(Directory, "Image Width", r.U32(body + 4).ToString(CultureInfo.InvariantCulture));
                report.Add(Directory, "Image Height", r.U32(body + 8).ToString(CultureInfo.InvariantCulture));
                break;
            case "irot":
                report.Add(Directory, "Rotation", (r.U8(body) & 3) is var steps and > 0 ? $"{steps * 90}° counter-clockwise" : "None");
                break;
            case "imir":
                report.Add(Directory, "Mirror", (r.U8(body) & 1) == 0 ? "Left-right" : "Top-bottom");
                break;
            case "pixi":
                int channels = r.U8(body + 4);
                report.Add(Directory, "Bits Per Channel", string.Join(", ", Enumerable.Range(0, channels).Select(c => r.U8(body + 5 + c))));
                break;
            case "colr":
                DescribeColor(r, property, report);
                break;
        }
    }

    private static void DescribeColor(EndianReader r, IsoBox colr, ReportBuilder report)
    {
        long body = colr.BodyOffset;
        string kind = Encoding.Latin1.GetString(r.Span(body, 4));
        if (kind == "nclx")
        {
            report.Add(Directory, "Color Primaries", Lookup(r.U16(body + 4), Primaries));
            report.Add(Directory, "Transfer Characteristics", Lookup(r.U16(body + 6), Transfers));
            report.Add(Directory, "Matrix Coefficients", Lookup(r.U16(body + 8), Matrices));
            report.Add(Directory, "Full Range", (r.U8(body + 10) & 0x80) != 0 ? "Yes" : "No");
        }
        else if (kind is "prof" or "rICC")
        {
            report.Add(Directory, "ICC Profile", $"({colr.End - body - 4:N0} bytes)");
        }
    }

    private static string AuxiliaryName(EndianReader r, IsoBox auxC)
    {
        long p = auxC.BodyOffset + 4;
        string urn = IsoMedia.CString(r, ref p, auxC.End);
        return urn switch
        {
            "urn:com:apple:photo:2020:aux:hdrgainmap" => "HDR gain map",
            "urn:com:apple:photo:2018:aux:portraiteffectsmatte" => "Portrait effects matte",
            "urn:mpeg:hevc:2015:auxid:1" or "urn:mpeg:mpegB:cicp:systems:auxiliary:alpha" => "Alpha",
            "urn:mpeg:hevc:2015:auxid:2" or "urn:mpeg:mpegB:cicp:systems:auxiliary:depth" => "Depth map",
            _ when urn.StartsWith("urn:com:apple:photo:", StringComparison.Ordinal) && urn.Contains("semanticskinmatte", StringComparison.Ordinal) => "Skin matte",
            _ when urn.StartsWith("urn:com:apple:photo:", StringComparison.Ordinal) && urn.Contains("semantichairmatte", StringComparison.Ordinal) => "Hair matte",
            _ when urn.StartsWith("urn:com:apple:photo:", StringComparison.Ordinal) && urn.Contains("semanticteethmatte", StringComparison.Ordinal) => "Teeth matte",
            _ => urn,
        };
    }

    /// <summary>The item's bytes: its extents joined, from the file (method 0) or the 'idat' box (method 1).</summary>
    private static byte[] Extract(EndianReader r, Location location, IsoBox? idat)
    {
        long origin = location.Method switch
        {
            0 => 0,
            1 when idat is { } box => box.BodyOffset,
            _ => throw new MalformedDataException($"Item stored with construction method {location.Method}, which isn't supported."),
        };
        using var output = new MemoryStream();
        foreach (var extent in location.Extents)
        {
            long start = origin + location.BaseOffset + extent.Offset;
            long length = extent.Length == 0 ? r.Length - start : extent.Length;
            output.Write(r.Span(start, length));
        }
        return output.ToArray();
    }

    private static string ItemTypeName(string type) => type switch
    {
        "hvc1" => "HEVC",
        "av01" => "AV1",
        "jpeg" => "JPEG",
        "grid" => "Grid of tiles",
        "iden" => "Derived (identity)",
        "iovl" => "Overlay",
        "tmap" => "Tone-mapped",
        "unci" => "Uncompressed",
        "Exif" => "Exif",
        "mime" => "MIME (XMP)",
        _ => type.Trim(),
    };

    private static string Lookup(int value, Dictionary<int, string> names) =>
        names.TryGetValue(value, out var name) ? name : $"Unknown ({value})";

    private static readonly Dictionary<int, string> Primaries = new()
    {
        [1] = "BT.709", [2] = "Unspecified", [5] = "BT.601 (PAL)", [6] = "BT.601 (NTSC)", [9] = "BT.2020", [12] = "Display P3",
    };

    private static readonly Dictionary<int, string> Transfers = new()
    {
        [1] = "BT.709", [2] = "Unspecified", [6] = "BT.601", [8] = "Linear", [13] = "sRGB", [14] = "BT.2020 (10-bit)",
        [16] = "PQ (HDR10)", [18] = "HLG",
    };

    private static readonly Dictionary<int, string> Matrices = new()
    {
        [0] = "Identity (RGB)", [1] = "BT.709", [2] = "Unspecified", [5] = "BT.601 (PAL)", [6] = "BT.601 (NTSC)",
        [9] = "BT.2020 non-constant", [10] = "BT.2020 constant",
    };
}
