using ExifViewer.Core.Containers;

namespace ExifViewer.Core.Tests;

public class IsoMediaTests
{
    private static string? Value(MetadataReport report, string directory, string tag) => report.Find(directory, tag)?.Value;

    private static byte[] AppleTiff() => TiffBuilder.Build(new Ifd()
        .Ascii(0x010F, "Apple")
        .Ascii(0x0110, "iPhone 16 Pro")
        .Short(0x0112, 6)
        .Pointer(0x8769, new Ifd().Rational(0x829D, (178, 100)).Ascii(0x9003, "2024:05:01 12:34:56")), bigEndian: true);

    [Fact]
    public void Heic_ReadsPrimaryImagePropertiesExifAndXmp()
    {
        var report = MetadataReader.Read(IsoFixtures.Heic(AppleTiff()), "IMG_0001.HEIC");

        Assert.Empty(report.Warnings);
        Assert.Equal("HEIC (HEIF, HEVC)", report.Format);
        Assert.Equal("heic", Value(report, "HEIF", "Major Brand"));
        Assert.Equal("Grid of tiles", Value(report, "HEIF", "Primary Image"));
        Assert.Equal("4032", Value(report, "HEIF", "Image Width"));
        Assert.Equal("3024", Value(report, "HEIF", "Image Height"));
        Assert.Equal("90° counter-clockwise", Value(report, "HEIF", "Rotation"));
        Assert.Equal("Display P3", Value(report, "HEIF", "Color Primaries"));
        Assert.Equal("sRGB", Value(report, "HEIF", "Transfer Characteristics"));
        Assert.Equal("HDR gain map", Value(report, "HEIF", "Auxiliary Images"));
        Assert.Equal("Grid of tiles × 1, HEVC × 1, Exif × 1, MIME (XMP) × 1", Value(report, "HEIF", "Items"));
        Assert.Equal("Apple", Value(report, "Image", "Make"));
        Assert.Equal("f/1.8", Value(report, "Exif", "F-Number"));
        Assert.Equal("2024-05-01T12:34:56", Value(report, "XMP", "photoshop:DateCreated"));
        Assert.Null(report.Preview);
    }

    [Fact]
    public void Mov_ReadsHeaderTracksAppleKeysAndUserData()
    {
        var report = MetadataReader.Read(IsoFixtures.Mov(), "IMG_0002.MOV");

        Assert.Empty(report.Warnings);
        Assert.Equal("QuickTime MOV", report.Format);
        Assert.Equal("2024-05-01 12:00:00 UTC", Value(report, "QuickTime", "Created"));
        Assert.Equal("0:00:12.500", Value(report, "QuickTime", "Duration"));

        Assert.Equal("HEVC (H.265)", Value(report, "Video Track", "Codec"));
        Assert.Equal("1920", Value(report, "Video Track", "Width"));
        Assert.Equal("90° clockwise", Value(report, "Video Track", "Rotation"));
        Assert.Equal("30 fps", Value(report, "Video Track", "Frame Rate"));
        Assert.Equal("1920×1080", Value(report, "Video Track", "Coded Size"));
        Assert.Null(report.Find("Video Track", "Language"));

        Assert.Equal("AAC", Value(report, "Audio Track", "Codec"));
        Assert.Equal("2", Value(report, "Audio Track", "Channels"));
        Assert.Equal("44100 Hz", Value(report, "Audio Track", "Sample Rate"));
        Assert.Equal("0:00:12.500", Value(report, "Audio Track", "Duration"));
        Assert.Null(report.Find("Audio Track", "Rotation"));

        Assert.Equal("Apple", Value(report, "QuickTime Metadata", "Make"));
        Assert.Equal("+37.7749-122.4194+012.345/", Value(report, "QuickTime Metadata", "Location (ISO 6709)"));
        Assert.Equal("37.774900, -122.419400", Value(report, "QuickTime Metadata", "Position"));
        Assert.Equal("12.3 m", Value(report, "QuickTime Metadata", "Altitude"));
        Assert.Equal("6A1D7C42-0B5C-4E7A-9D0E-1F2A3B4C5D6E", Value(report, "QuickTime Metadata", "Content Identifier"));
        Assert.Equal("0.75", Value(report, "QuickTime Metadata", "Live Photo Vitality Score"));
        Assert.Equal("iPhone 16 Pro", Value(report, "QuickTime User Data", "Model"));
    }

    [Fact]
    public void MovFromDisk_SkipsMediaDataButReportsTheRealSize()
    {
        string path = Path.Combine(Path.GetTempPath(), $"exifviewer-{Guid.NewGuid():N}.mov");
        var bytes = IsoFixtures.Mov(mediaBytes: 5_000_000);
        File.WriteAllBytes(path, bytes);
        try
        {
            using (var stream = File.OpenRead(path))
                Assert.True(IsoMedia.LoadWithoutMediaData(stream).Length < 10_000);

            var report = MetadataReader.Read(path);

            Assert.Empty(report.Warnings);
            Assert.Equal(MetadataReader.FormatSize(bytes.Length), Value(report, "File", "File Size"));
            Assert.Equal("HEVC (H.265)", Value(report, "Video Track", "Codec"));
            Assert.Equal("Apple", Value(report, "QuickTime Metadata", "Make"));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void TruncatedHeicAndMov_WarnWithoutThrowing()
    {
        foreach (var file in new[] { IsoFixtures.Heic(AppleTiff()), IsoFixtures.Mov() })
        {
            for (int length = 0; length < file.Length; length += 13)
                MetadataReader.Read(file.AsMemory(0, length), "cut.mov");
            Assert.NotEmpty(MetadataReader.Read(file.AsMemory(0, file.Length - 20), "cut").Warnings);
        }
    }

    [Theory]
    [InlineData(12.5, "0:00:12.500")]
    [InlineData(3725.042, "1:02:05.042")]
    public void Duration_IsHoursMinutesSecondsMillis(double seconds, string expected) =>
        Assert.Equal(expected, QuickTimeReader.Duration(seconds));
}
