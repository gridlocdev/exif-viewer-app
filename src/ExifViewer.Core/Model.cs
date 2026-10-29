using System.Globalization;

namespace ExifViewer.Core;

/// <summary>Everything read from one file: tags grouped by where they came from, plus an embedded preview.</summary>
public sealed class MetadataReport
{
    public required string FileName { get; init; }
    public required string Format { get; init; }
    public required IReadOnlyList<MetadataDirectory> Directories { get; init; }
    public IReadOnlyList<string> Warnings { get; init; } = [];

    /// <summary>The largest displayable JPEG embedded in the file (EXIF thumbnail, RAW preview), if any.</summary>
    public ReadOnlyMemory<byte>? Preview { get; init; }

    /// <summary>The EXIF orientation of the main image (1–8), if the file has one.</summary>
    public int? Orientation { get; init; }

    public MetadataDirectory? Find(string directory) =>
        Directories.FirstOrDefault(d => d.Name == directory);

    public MetadataTag? Find(string directory, string tag) =>
        Find(directory)?.Tags.FirstOrDefault(t => t.Name == tag);
}

public sealed class MetadataDirectory(string name)
{
    public string Name { get; } = name;
    public List<MetadataTag> Tags { get; } = [];
}

/// <param name="Id">The tag number inside its directory (EXIF/TIFF/GPS), when it has one.</param>
public sealed record MetadataTag(string Name, string Value, int? Id = null)
{
    public string IdHex => Id is int id ? $"0x{id:X4}" : "";

    /// <summary>Set on "Position" tags, so the UI can offer to show the place on a map.</summary>
    public GeoPosition? Position { get; init; }

    public bool HasPosition => Position is not null;

    public static MetadataTag ForPosition(GeoPosition position) => new("Position", position.ToString()) { Position = position };
}

/// <summary>A point in decimal degrees: latitude north-positive, longitude east-positive.</summary>
public sealed record GeoPosition(double Latitude, double Longitude)
{
    /// <summary>The position if both values are real coordinates, else null.</summary>
    public static GeoPosition? Create(double latitude, double longitude) =>
        double.IsFinite(latitude) && double.IsFinite(longitude) && Math.Abs(latitude) <= 90 && Math.Abs(longitude) <= 180
            ? new GeoPosition(latitude, longitude)
            : null;

    /// <summary>"37.774900, -122.419400".</summary>
    public override string ToString() =>
        $"{Latitude.ToString("0.000000", CultureInfo.InvariantCulture)}, {Longitude.ToString("0.000000", CultureInfo.InvariantCulture)}";
}

public readonly record struct Rational(long Numerator, long Denominator)
{
    public bool IsValid => Denominator != 0;
    public double ToDouble() => IsValid ? (double)Numerator / Denominator : double.NaN;

    public override string ToString() =>
        !IsValid ? $"{Numerator}/0"
        : Denominator == 1 ? Numerator.ToString(CultureInfo.InvariantCulture)
        : ToDouble().ToString("0.####", CultureInfo.InvariantCulture);
}
