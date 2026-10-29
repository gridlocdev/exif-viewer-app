using System.Text;
using ExifViewer.Core.Tiff;

namespace ExifViewer.Core.Tests;

public class ReaderTests
{
    private static Ifd CameraIfd() => new Ifd()
        .Ascii(0x010F, "Canon")
        .Ascii(0x0110, "Canon EOS R5")
        .Short(0x0112, 6)
        .Pointer(0x8769, new Ifd()
            .Rational(0x829A, (1, 250))
            .Rational(0x829D, (28, 10))
            .Short(0x8827, 400)
            .Bytes(0x9000, 7, "0232"u8.ToArray())
            .Ascii(0x9003, "2024:05:01 12:34:56")
            .SRational(0x9204, (-2, 3))
            .Short(0x9209, 0x19)
            .Rational(0x920A, (50, 1))
            .Rational(0xA432, (24, 1), (70, 1), (28, 10), (28, 10)));

    private static Ifd GpsIfd() => new Ifd()
        .Bytes(0x00, 1, 2, 3, 0, 0)
        .Ascii(0x01, "N")
        .Rational(0x02, (37, 1), (46, 1), (2964, 100))
        .Ascii(0x03, "W")
        .Rational(0x04, (122, 1), (25, 1), (984, 100))
        .Bytes(0x05, 1, 0)
        .Rational(0x06, (125, 10))
        .Rational(0x07, (14, 1), (5, 1), (30, 1));

    private static string? Value(MetadataReport report, string directory, string tag) => report.Find(directory, tag)?.Value;

    [Fact]
    public void JpegExif_FormatsCameraAndExposureTags()
    {
        var jpeg = Containers.Jpeg(6000, 4000, Containers.Exif(TiffBuilder.Build(CameraIfd())));

        var report = MetadataReader.Read(jpeg, "photo.jpg");

        Assert.Empty(report.Warnings);
        Assert.Equal("JPEG", report.Format);
        Assert.Equal("Canon", Value(report, "Image", "Make"));
        Assert.Equal("Canon EOS R5", Value(report, "Image", "Model"));
        Assert.Equal("Rotate 90 CW", Value(report, "Image", "Orientation"));
        Assert.Equal(6, report.Orientation);
        Assert.Equal("1/250 s", Value(report, "Exif", "Exposure Time"));
        Assert.Equal("f/2.8", Value(report, "Exif", "F-Number"));
        Assert.Equal("400", Value(report, "Exif", "ISO"));
        Assert.Equal("2.32", Value(report, "Exif", "Exif Version"));
        Assert.Equal("2024:05:01 12:34:56", Value(report, "Exif", "Date/Time Original"));
        Assert.Equal("-0.67 EV", Value(report, "Exif", "Exposure Compensation"));
        Assert.Equal("Fired, Auto", Value(report, "Exif", "Flash"));
        Assert.Equal("50 mm", Value(report, "Exif", "Focal Length"));
        Assert.Equal("24-70mm f/2.8", Value(report, "Exif", "Lens Info"));
        Assert.Equal("6000", Value(report, "JPEG", "Image Width"));
        Assert.Equal("4000", Value(report, "JPEG", "Image Height"));
    }

    [Fact]
    public void Gps_FormatsCoordinatesWithRefsAndAddsPosition()
    {
        var tiff = TiffBuilder.Build(new Ifd().Ascii(0x010F, "Apple").Pointer(0x8825, GpsIfd()));

        var report = MetadataReader.Read(Containers.Jpeg(10, 10, Containers.Exif(tiff)), "gps.jpg");

        Assert.Equal("2.3.0.0", Value(report, "GPS", "GPS Version ID"));
        Assert.Equal("North", Value(report, "GPS", "Latitude Ref"));
        Assert.Equal("37° 46′ 29.64″ N", Value(report, "GPS", "Latitude"));
        Assert.Equal("122° 25′ 9.84″ W", Value(report, "GPS", "Longitude"));
        Assert.Equal("12.5 m", Value(report, "GPS", "Altitude"));
        Assert.Equal("14:05:30 UTC", Value(report, "GPS", "Time Stamp"));
        Assert.Equal("37.774900, -122.419400", Value(report, "GPS", "Position"));
    }

    [Fact]
    public void BigEndianTiff_IsReadAsTiffFile()
    {
        var tiff = TiffBuilder.Build(new Ifd().Ascii(0x010F, "Nikon").Long(0x0100, 8256).Short(0x0103, 1), bigEndian: true);

        var report = MetadataReader.Read(tiff, "scan.tif");

        Assert.Empty(report.Warnings);
        Assert.Equal("TIFF", report.Format);
        Assert.Equal("Nikon", Value(report, "Image", "Make"));
        Assert.Equal("8256", Value(report, "Image", "Image Width"));
        Assert.Equal("Uncompressed", Value(report, "Image", "Compression"));
    }

    [Fact]
    public void RawExtension_NamesTheFormat()
    {
        var report = MetadataReader.Read(TiffBuilder.Build(new Ifd().Ascii(0x010F, "Nikon")), "DSC_0001.NEF");

        Assert.Equal("NEF (Nikon RAW)", report.Format);
    }

    [Fact]
    public void Thumbnail_BecomesThePreview()
    {
        var thumbnail = Containers.Jpeg(160, 120);
        var ifd0 = new Ifd().Ascii(0x010F, "Sony");
        ifd0.Next = new Ifd().Short(0x0103, 6).Thumbnail(thumbnail);

        var report = MetadataReader.Read(Containers.Jpeg(10, 10, Containers.Exif(TiffBuilder.Build(ifd0))), "a.jpg");

        Assert.NotNull(report.Preview);
        Assert.Equal(thumbnail, report.Preview.Value.ToArray());
        Assert.Equal($"{thumbnail.Length}", Value(report, "Thumbnail", "Thumbnail Length"));
    }

    [Fact]
    public void SubIfdStripJpeg_BecomesThePreview()
    {
        var preview = Containers.Jpeg(1616, 1080);
        // A single-strip JPEG in a SubIFD, the way NEF and DNG files carry their larger previews.
        // The preview goes after the TIFF structure, whose size doesn't depend on the offset value.
        byte[] Structure(uint previewAt) => TiffBuilder.Build(new Ifd()
            .Ascii(0x010F, "Nikon")
            .Pointer(0x014A, new Ifd().Short(0x0103, 6).Long(0x0111, previewAt).Long(0x0117, (uint)preview.Length)));
        byte[] file = [.. Structure((uint)Structure(0).Length), .. preview];

        var report = MetadataReader.Read(file, "DSC_0002.NEF");

        Assert.Empty(report.Warnings);
        Assert.NotNull(report.Find("SubIFD"));
        Assert.Equal(preview, report.Preview?.ToArray());
    }

    [Fact]
    public void IfdLoop_StopsWithAWarning()
    {
        // IFD0 at offset 8 holds one entry and names itself as the next IFD.
        byte[] tiff =
        [
            .. "II*\0"u8, 8, 0, 0, 0,
            1, 0,
            0x0F, 0x01, 2, 0, 4, 0, 0, 0, (byte)'A', (byte)'b', (byte)'c', 0,
            8, 0, 0, 0,
        ];

        var report = MetadataReader.Read(tiff, "loop.tif");

        Assert.Equal("Abc", Value(report, "Image", "Make"));
        Assert.Contains(report.Warnings, w => w.Contains("already read", StringComparison.Ordinal));
    }

    [Fact]
    public void TruncatedFile_KeepsWhatItReadAndWarns()
    {
        var jpeg = Containers.Jpeg(10, 10, Containers.Exif(TiffBuilder.Build(CameraIfd())));

        for (int length = 0; length < jpeg.Length; length += 7)
        {
            var report = MetadataReader.Read(jpeg.AsMemory(0, length), "cut.jpg");
            Assert.NotNull(report.Find("File"));
        }
        var half = MetadataReader.Read(jpeg.AsMemory(0, jpeg.Length / 2), "cut.jpg");
        Assert.NotEmpty(half.Warnings);
    }

    [Fact]
    public void Xmp_FlattensAttributesArraysAndText()
    {
        const string xml = """
            <x:xmpmeta xmlns:x="adobe:ns:meta/">
              <rdf:RDF xmlns:rdf="http://www.w3.org/1999/02/22-rdf-syntax-ns#">
                <rdf:Description xmlns:xmp="http://ns.adobe.com/xap/1.0/" xmlns:dc="http://purl.org/dc/elements/1.1/" xmp:Rating="4">
                  <dc:title><rdf:Alt><rdf:li xml:lang="x-default">Golden Gate</rdf:li></rdf:Alt></dc:title>
                  <dc:subject><rdf:Bag><rdf:li>bridge</rdf:li><rdf:li>fog</rdf:li></rdf:Bag></dc:subject>
                  <xmp:CreatorTool>Lightroom</xmp:CreatorTool>
                </rdf:Description>
              </rdf:RDF>
            </x:xmpmeta>
            """;

        var report = MetadataReader.Read(Containers.Jpeg(10, 10, Containers.Xmp(xml)), "x.jpg");

        Assert.Empty(report.Warnings);
        Assert.Equal("4", Value(report, "XMP", "xmp:Rating"));
        Assert.Equal("Golden Gate", Value(report, "XMP", "dc:title"));
        Assert.Equal("bridge, fog", Value(report, "XMP", "dc:subject"));
        Assert.Equal("Lightroom", Value(report, "XMP", "xmp:CreatorTool"));
    }

    [Fact]
    public void Xmp_WithDoctype_IsRejected()
    {
        const string xml = """<!DOCTYPE x [<!ENTITY e SYSTEM "file:///etc/passwd">]><x>&e;</x>""";

        var report = MetadataReader.Read(Containers.Jpeg(10, 10, Containers.Xmp(xml)), "x.jpg");

        Assert.Null(report.Find("XMP"));
        Assert.Contains(report.Warnings, w => w.StartsWith("XMP:", StringComparison.Ordinal));
    }

    [Fact]
    public void Png_ReadsHeaderTextAndExif()
    {
        var png = Containers.Png(
            ("IHDR", Containers.PngHeader(640, 480)),
            ("tEXt", [.. "Software\0"u8, .. "GIMP"u8]),
            ("eXIf", TiffBuilder.Build(new Ifd().Ascii(0x010F, "Google"))));

        var report = MetadataReader.Read(png, "a.png");

        Assert.Empty(report.Warnings);
        Assert.Equal("640", Value(report, "PNG", "Image Width"));
        Assert.Equal("RGB with alpha", Value(report, "PNG", "Color Type"));
        Assert.Equal("GIMP", Value(report, "PNG Text", "Software"));
        Assert.Equal("Google", Value(report, "Image", "Make"));
    }

    [Fact]
    public void WebP_ReadsCanvasAndExifWithOrWithoutPrefix()
    {
        var tiff = TiffBuilder.Build(new Ifd().Ascii(0x0110, "Pixel 9"));
        foreach (var exif in new[] { tiff, [.. "Exif\0\0"u8, .. tiff] })
        {
            var report = MetadataReader.Read(Containers.WebP(("VP8X", Containers.Vp8X(1920, 1080)), ("EXIF", exif)), "a.webp");

            Assert.Empty(report.Warnings);
            Assert.Equal("1920", Value(report, "WebP", "Canvas Width"));
            Assert.Equal("1080", Value(report, "WebP", "Canvas Height"));
            Assert.Equal("Pixel 9", Value(report, "Image", "Model"));
        }
    }

    [Fact]
    public void UnknownFile_WarnsButStillDescribesTheFile()
    {
        var report = MetadataReader.Read(Encoding.ASCII.GetBytes("just some text"), "notes.txt");

        Assert.Equal("Unknown", report.Format);
        Assert.Equal("14 bytes", Value(report, "File", "File Size"));
        Assert.Single(report.Warnings);
    }

    [Theory]
    [InlineData(0x00, "Did not fire")]
    [InlineData(0x10, "Did not fire, Forced off")]
    [InlineData(0x19, "Fired, Auto")]
    [InlineData(0x0F, "Fired, Forced on, Return detected")]
    [InlineData(0x59, "Fired, Auto, Red-eye reduction")]
    [InlineData(0x20, "Did not fire, No flash function")]
    public void Flash_DescribesEachBit(long value, string expected) =>
        Assert.Equal(expected, TagFormatter.Flash(value));

    [Theory]
    [InlineData(500L, "500 bytes")]
    [InlineData(1536L, "1.5 KB (1,536 bytes)")]
    [InlineData(25_000_000L, "23.8 MB (25,000,000 bytes)")]
    public void FileSize_IsHumanReadable(long bytes, string expected) =>
        Assert.Equal(expected, MetadataReader.FormatSize(bytes));
}
