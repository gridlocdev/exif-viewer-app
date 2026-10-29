using System.Buffers.Binary;
using System.Text;

namespace ExifViewer.Core.Tests;

/// <summary>Builds ISO base media boxes (HEIF, MP4, QuickTime) byte by byte.</summary>
internal static class Iso
{
    public static byte[] Box(string type, params byte[][] parts)
    {
        byte[] body = [.. parts.SelectMany(p => p)];
        return [.. U32((uint)(8 + body.Length)), .. Encoding.Latin1.GetBytes(type), .. body];
    }

    public static byte[] FullBox(string type, byte version, uint flags, params byte[][] parts) =>
        Box(type, [[version, (byte)(flags >> 16), (byte)(flags >> 8), (byte)flags], .. parts]);

    public static byte[] U16(ushort v)
    {
        var b = new byte[2];
        BinaryPrimitives.WriteUInt16BigEndian(b, v);
        return b;
    }

    public static byte[] U32(uint v)
    {
        var b = new byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(b, v);
        return b;
    }

    public static byte[] S32(int v) => U32(unchecked((uint)v));

    public static byte[] Text(string s) => Encoding.UTF8.GetBytes(s);

    public static byte[] CString(string s) => [.. Encoding.UTF8.GetBytes(s), 0];

    public static byte[] Zeros(int count) => new byte[count];

    /// <summary>An ilst item box: its type is the 1-based index into the 'keys' box.</summary>
    public static byte[] KeyItem(uint index, byte[] data) => Box(Encoding.Latin1.GetString(U32(index)), data);

    public static byte[] Data(uint type, byte[] value) => Box("data", U32(type), U32(0), value);
}

internal static class IsoFixtures
{
    public const string Xmp = """
        <x:xmpmeta xmlns:x="adobe:ns:meta/"><rdf:RDF xmlns:rdf="http://www.w3.org/1999/02/22-rdf-syntax-ns#">
        <rdf:Description xmlns:photoshop="http://ns.adobe.com/photoshop/1.0/" photoshop:DateCreated="2024-05-01T12:34:56"/>
        </rdf:RDF></x:xmpmeta>
        """;

    /// <summary>
    /// An iPhone-style HEIC: grid primary image with ispe/irot, an HDR gain map auxC, and Exif and XMP items
    /// stored in 'mdat'. The Exif item carries Apple's 6-byte "Exif\0\0" prefix.
    /// </summary>
    public static byte[] Heic(byte[] tiff)
    {
        byte[] exif = [.. Iso.U32(6), .. "Exif\0\0"u8, .. tiff];
        byte[] xmp = Iso.Text(Xmp);
        byte[] ftyp = Iso.Box("ftyp", Iso.Text("heic"), Iso.U32(0), Iso.Text("mif1"), Iso.Text("MiHB"), Iso.Text("heic"));

        byte[] Meta(uint exifAt, uint xmpAt) => Iso.FullBox("meta", 0, 0,
            Iso.FullBox("hdlr", 0, 0, Iso.U32(0), Iso.Text("pict"), Iso.Zeros(12), Iso.CString("")),
            Iso.FullBox("pitm", 0, 0, Iso.U16(1)),
            Iso.FullBox("iinf", 0, 0, Iso.U16(4),
                Iso.FullBox("infe", 2, 0, Iso.U16(1), Iso.U16(0), Iso.Text("grid"), Iso.CString("")),
                Iso.FullBox("infe", 2, 1, Iso.U16(2), Iso.U16(0), Iso.Text("hvc1"), Iso.CString("")),
                Iso.FullBox("infe", 2, 0, Iso.U16(3), Iso.U16(0), Iso.Text("Exif"), Iso.CString("")),
                Iso.FullBox("infe", 2, 0, Iso.U16(4), Iso.U16(0), Iso.Text("mime"), Iso.CString(""), Iso.CString("application/rdf+xml"))),
            Iso.FullBox("iloc", 0, 0, [0x44, 0x00], Iso.U16(2),
                Iso.U16(3), Iso.U16(0), Iso.U16(1), Iso.U32(exifAt), Iso.U32((uint)exif.Length),
                Iso.U16(4), Iso.U16(0), Iso.U16(1), Iso.U32(xmpAt), Iso.U32((uint)xmp.Length)),
            Iso.Box("iprp",
                Iso.Box("ipco",
                    Iso.FullBox("ispe", 0, 0, Iso.U32(4032), Iso.U32(3024)),
                    Iso.Box("irot", [1]),
                    Iso.FullBox("auxC", 0, 0, Iso.CString("urn:com:apple:photo:2020:aux:hdrgainmap")),
                    Iso.Box("colr", Iso.Text("nclx"), Iso.U16(12), Iso.U16(13), Iso.U16(6), [0x80])),
                Iso.FullBox("ipma", 0, 0, Iso.U32(1), Iso.U16(1), [3, 0x81, 0x02, 0x04])));

        uint exifAt = (uint)(ftyp.Length + Meta(0, 0).Length + 8);
        byte[] mdat = Iso.Box("mdat", exif, xmp);
        return [.. ftyp, .. Meta(exifAt, exifAt + (uint)exif.Length), .. mdat];
    }

    public static readonly DateTime Created = new(2024, 5, 1, 12, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// An iPhone-style MOV: 12.5 s of portrait (rotated 90°) HEVC at 30 fps plus stereo AAC, Apple 'mdta' keys
    /// with a location, and a '©mod' user data atom. <paramref name="mediaBytes"/> sets the size of 'mdat'.
    /// </summary>
    public static byte[] Mov(int mediaBytes = 64)
    {
        uint created = (uint)(Created - new DateTime(1904, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
        byte[] Times(uint timescale, uint duration) => [.. Iso.U32(created), .. Iso.U32(created), .. Iso.U32(timescale), .. Iso.U32(duration)];
        byte[] Tkhd(int a, int b, int c, int d, uint width, uint height) => Iso.FullBox("tkhd", 0, 0xF,
            Iso.U32(created), Iso.U32(created), Iso.U32(1), Iso.U32(0), Iso.U32(7500), Iso.Zeros(16),
            Iso.S32(a), Iso.S32(b), Iso.U32(0), Iso.S32(c), Iso.S32(d), Iso.U32(0), Iso.U32(0), Iso.U32(0), Iso.U32(0x40000000),
            Iso.U32(width << 16), Iso.U32(height << 16));
        byte[] Hdlr(string handler) => Iso.FullBox("hdlr", 0, 0, Iso.Text("mhlr"), Iso.Text(handler), Iso.Zeros(12), Iso.CString(""));

        byte[] video = Iso.Box("trak",
            Tkhd(0, 0x10000, -0x10000, 0, 1920, 1080),
            Iso.Box("mdia",
                Iso.FullBox("mdhd", 0, 0, Times(600, 7500), Iso.U16(0x55C4), Iso.U16(0)),
                Hdlr("vide"),
                Iso.Box("minf", Iso.Box("stbl",
                    Iso.FullBox("stsd", 0, 0, Iso.U32(1),
                        Iso.Box("hvc1", Iso.Zeros(6), Iso.U16(1), Iso.Zeros(16), Iso.U16(1920), Iso.U16(1080), Iso.Zeros(50))),
                    Iso.FullBox("stts", 0, 0, Iso.U32(1), Iso.U32(375), Iso.U32(20))))));

        byte[] audio = Iso.Box("trak",
            Tkhd(0x10000, 0, 0, 0x10000, 0, 0),
            Iso.Box("mdia",
                Iso.FullBox("mdhd", 0, 0, Times(44100, 551250), Iso.U16(0x55C4), Iso.U16(0)),
                Hdlr("soun"),
                Iso.Box("minf", Iso.Box("stbl",
                    Iso.FullBox("stsd", 0, 0, Iso.U32(1),
                        Iso.Box("mp4a", Iso.Zeros(6), Iso.U16(1), Iso.Zeros(8), Iso.U16(2), Iso.U16(16), Iso.U16(0), Iso.U16(0), Iso.U32(44100u << 16)))))));

        byte[] Key(string name) => [.. Iso.U32((uint)(8 + Encoding.UTF8.GetByteCount(name))), .. Iso.Text("mdta"), .. Iso.Text(name)];
        byte[] meta = Iso.Box("meta",
            Hdlr("mdta"),
            Iso.FullBox("keys", 0, 0, Iso.U32(4),
                Key("com.apple.quicktime.make"),
                Key("com.apple.quicktime.location.ISO6709"),
                Key("com.apple.quicktime.content.identifier"),
                Key("com.apple.quicktime.live-photo.vitality-score")),
            Iso.Box("ilst",
                Iso.KeyItem(1, Iso.Data(1, Iso.Text("Apple"))),
                Iso.KeyItem(2, Iso.Data(1, Iso.Text("+37.7749-122.4194+012.345/"))),
                Iso.KeyItem(3, Iso.Data(1, Iso.Text("6A1D7C42-0B5C-4E7A-9D0E-1F2A3B4C5D6E"))),
                Iso.KeyItem(4, Iso.Data(23, [0x3F, 0x40, 0x00, 0x00]))));

        byte[] model = Iso.Text("iPhone 16 Pro");
        byte[] udta = Iso.Box("udta", Iso.Box("©mod", Iso.U16((ushort)model.Length), Iso.U16(0x55C4), model));

        return
        [
            .. Iso.Box("ftyp", Iso.Text("qt  "), Iso.U32(0), Iso.Text("qt  ")),
            .. Iso.Box("wide"),
            .. Iso.Box("mdat", Iso.Zeros(mediaBytes)),
            .. Iso.Box("moov", Iso.FullBox("mvhd", 0, 0, Times(600, 7500), Iso.Zeros(80)), video, audio, meta, udta),
        ];
    }
}
