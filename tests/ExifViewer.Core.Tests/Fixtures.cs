using System.Buffers.Binary;
using System.Text;

namespace ExifViewer.Core.Tests;

/// <summary>One IFD under construction. Values are written in the file's byte order by <see cref="TiffBuilder"/>.</summary>
internal sealed class Ifd
{
    internal abstract record Entry(ushort Tag);
    internal sealed record ValueEntry(ushort Tag, ushort Type, uint Count, Func<bool, byte[]> Encode) : Entry(Tag);
    internal sealed record PointerEntry(ushort Tag, Ifd Child) : Entry(Tag);
    internal sealed record BlobEntry(ushort OffsetTag, ushort LengthTag, byte[] Data) : Entry(OffsetTag);

    public List<Entry> Entries { get; } = [];
    public Ifd? Next { get; set; }

    public Ifd Ascii(ushort tag, string text)
    {
        var bytes = Encoding.ASCII.GetBytes(text + "\0");
        Entries.Add(new ValueEntry(tag, 2, (uint)bytes.Length, _ => bytes));
        return this;
    }

    public Ifd Bytes(ushort tag, ushort type, params byte[] bytes)
    {
        Entries.Add(new ValueEntry(tag, type, (uint)bytes.Length, _ => bytes));
        return this;
    }

    public Ifd Short(ushort tag, params ushort[] values)
    {
        Entries.Add(new ValueEntry(tag, 3, (uint)values.Length, be => Encode(values, 2, (s, v) => Write16(s, v, be))));
        return this;
    }

    public Ifd Long(ushort tag, params uint[] values)
    {
        Entries.Add(new ValueEntry(tag, 4, (uint)values.Length, be => Encode(values, 4, (s, v) => Write32(s, v, be))));
        return this;
    }

    public Ifd Rational(ushort tag, params (uint N, uint D)[] values)
    {
        Entries.Add(new ValueEntry(tag, 5, (uint)values.Length, be => Encode(values, 8, (s, v) =>
        {
            Write32(s, v.N, be);
            Write32(s[4..], v.D, be);
        })));
        return this;
    }

    public Ifd SRational(ushort tag, params (int N, int D)[] values)
    {
        Entries.Add(new ValueEntry(tag, 10, (uint)values.Length, be => Encode(values, 8, (s, v) =>
        {
            Write32(s, unchecked((uint)v.N), be);
            Write32(s[4..], unchecked((uint)v.D), be);
        })));
        return this;
    }

    public Ifd Pointer(ushort tag, Ifd child)
    {
        Entries.Add(new PointerEntry(tag, child));
        return this;
    }

    /// <summary>JPEGInterchangeFormat/Length pointing at <paramref name="jpeg"/>.</summary>
    public Ifd Thumbnail(byte[] jpeg)
    {
        Entries.Add(new BlobEntry(0x0201, 0x0202, jpeg));
        return this;
    }

    private delegate void Writer<T>(Span<byte> span, T value);

    private static byte[] Encode<T>(T[] values, int size, Writer<T> write)
    {
        var bytes = new byte[values.Length * size];
        for (int i = 0; i < values.Length; i++)
            write(bytes.AsSpan(i * size), values[i]);
        return bytes;
    }

    internal static void Write16(Span<byte> s, ushort v, bool be)
    {
        if (be) BinaryPrimitives.WriteUInt16BigEndian(s, v);
        else BinaryPrimitives.WriteUInt16LittleEndian(s, v);
    }

    internal static void Write32(Span<byte> s, uint v, bool be)
    {
        if (be) BinaryPrimitives.WriteUInt32BigEndian(s, v);
        else BinaryPrimitives.WriteUInt32LittleEndian(s, v);
    }
}

/// <summary>Lays out a TIFF structure: header, then each IFD followed by its out-of-line values and child IFDs.</summary>
internal static class TiffBuilder
{
    public static byte[] Build(Ifd ifd0, bool bigEndian = false)
    {
        var buffer = new List<byte>(bigEndian ? "MM\0*"u8.ToArray() : "II*\0"u8.ToArray());
        buffer.AddRange(new byte[4]);
        uint first = Write(buffer, ifd0, bigEndian);
        var bytes = buffer.ToArray();
        Ifd.Write32(bytes.AsSpan(4), first, bigEndian);
        return bytes;
    }

    private static uint Write(List<byte> buffer, Ifd ifd, bool be)
    {
        var entries = new List<(ushort Tag, ushort Type, uint Count, byte[] Value)>();
        int count = ifd.Entries.Sum(e => e is Ifd.BlobEntry ? 2 : 1);
        uint offset = (uint)buffer.Count;
        buffer.AddRange(new byte[2 + count * 12 + 4]);

        byte[] Long(uint v)
        {
            var b = new byte[4];
            Ifd.Write32(b, v, be);
            return b;
        }

        foreach (var entry in ifd.Entries)
        {
            switch (entry)
            {
                case Ifd.ValueEntry v:
                    entries.Add((v.Tag, v.Type, v.Count, v.Encode(be)));
                    break;
                case Ifd.PointerEntry p:
                    entries.Add((p.Tag, 4, 1, Long(Write(buffer, p.Child, be))));
                    break;
                case Ifd.BlobEntry blob:
                    uint at = (uint)buffer.Count;
                    buffer.AddRange(blob.Data);
                    entries.Add((blob.OffsetTag, 4, 1, Long(at)));
                    entries.Add((blob.LengthTag, 4, 1, Long((uint)blob.Data.Length)));
                    break;
            }
        }

        var header = new byte[2 + count * 12 + 4];
        Ifd.Write16(header, (ushort)count, be);
        for (int i = 0; i < entries.Count; i++)
        {
            var (tag, type, n, value) = entries[i];
            var e = header.AsSpan(2 + i * 12, 12);
            Ifd.Write16(e, tag, be);
            Ifd.Write16(e[2..], type, be);
            Ifd.Write32(e[4..], n, be);
            if (value.Length <= 4)
            {
                value.CopyTo(e[8..]);
            }
            else
            {
                Ifd.Write32(e[8..], (uint)buffer.Count, be);
                buffer.AddRange(value);
                if (value.Length % 2 == 1)
                    buffer.Add(0);
            }
        }
        if (ifd.Next is not null)
            Ifd.Write32(header.AsSpan(2 + count * 12), Write(buffer, ifd.Next, be), be);

        for (int i = 0; i < header.Length; i++)
            buffer[(int)offset + i] = header[i];
        return offset;
    }
}

/// <summary>Minimal JPEG, PNG and WebP files around metadata blocks. The image data is never valid; parsers stop before it.</summary>
internal static class Containers
{
    /// <summary>SOI, the given APPn segments, a baseline frame header, an empty scan, EOI.</summary>
    public static byte[] Jpeg(int width, int height, params (byte Marker, byte[] Payload)[] segments)
    {
        var bytes = new List<byte> { 0xFF, 0xD8 };
        foreach (var (marker, payload) in segments)
            Segment(bytes, marker, payload);
        Segment(bytes, 0xC0, [8, (byte)(height >> 8), (byte)height, (byte)(width >> 8), (byte)width, 3, 1, 0x22, 0, 2, 0x11, 1, 3, 0x11, 1]);
        Segment(bytes, 0xDA, [3, 1, 0, 2, 0x11, 3, 0x11, 0, 0x3F, 0]);
        bytes.AddRange([0xFF, 0xD9]);
        return bytes.ToArray();
    }

    public static (byte, byte[]) Exif(byte[] tiff) => (0xE1, [.. "Exif\0\0"u8, .. tiff]);

    public static (byte, byte[]) Xmp(string xml) => (0xE1, [.. "http://ns.adobe.com/xap/1.0/\0"u8, .. Encoding.UTF8.GetBytes(xml)]);

    private static void Segment(List<byte> bytes, byte marker, byte[] payload)
    {
        int length = payload.Length + 2;
        bytes.AddRange([0xFF, marker, (byte)(length >> 8), (byte)length]);
        bytes.AddRange(payload);
    }

    public static byte[] Png(params (string Type, byte[] Data)[] chunks)
    {
        var bytes = new List<byte>([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);
        foreach (var (type, data) in chunks.Append(("IEND", [])))
        {
            var length = new byte[4];
            BinaryPrimitives.WriteUInt32BigEndian(length, (uint)data.Length);
            bytes.AddRange(length);
            bytes.AddRange(Encoding.ASCII.GetBytes(type));
            bytes.AddRange(data);
            bytes.AddRange(new byte[4]); // CRC, not checked
        }
        return bytes.ToArray();
    }

    public static byte[] PngHeader(uint width, uint height)
    {
        var data = new byte[13];
        BinaryPrimitives.WriteUInt32BigEndian(data, width);
        BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(4), height);
        data[8] = 8;
        data[9] = 6;
        return data;
    }

    public static byte[] WebP(params (string FourCc, byte[] Data)[] chunks)
    {
        var body = new List<byte>("WEBP"u8.ToArray());
        foreach (var (fourCc, data) in chunks)
        {
            body.AddRange(Encoding.ASCII.GetBytes(fourCc));
            var size = new byte[4];
            BinaryPrimitives.WriteUInt32LittleEndian(size, (uint)data.Length);
            body.AddRange(size);
            body.AddRange(data);
            if (data.Length % 2 == 1)
                body.Add(0);
        }
        var riffSize = new byte[4];
        BinaryPrimitives.WriteUInt32LittleEndian(riffSize, (uint)body.Count);
        return [.. "RIFF"u8, .. riffSize, .. body];
    }

    /// <summary>VP8X header for a canvas of the given size with the EXIF flag set.</summary>
    public static byte[] Vp8X(int width, int height)
    {
        int w = width - 1, h = height - 1;
        return [0x08, 0, 0, 0, (byte)w, (byte)(w >> 8), (byte)(w >> 16), (byte)h, (byte)(h >> 8), (byte)(h >> 16)];
    }
}
