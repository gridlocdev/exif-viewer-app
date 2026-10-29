using System.Text;

namespace ExifViewer.Core.Tiff;

internal enum TiffType : ushort
{
    Byte = 1, Ascii = 2, Short = 3, Long = 4, Rational = 5, SByte = 6, Undefined = 7,
    SShort = 8, SLong = 9, SRational = 10, Float = 11, Double = 12, Ifd = 13,
}

/// <summary>One IFD entry's value, decoded into whichever array suits its type.</summary>
internal sealed class TiffValue
{
    public required TiffType Type { get; init; }
    public required int Count { get; init; }
    public required bool BigEndian { get; init; }

    /// <summary>Raw bytes for Byte, SByte, Undefined and Ascii values.</summary>
    public byte[] Bytes { get; init; } = [];
    /// <summary>Integer values (also filled for Byte and SByte).</summary>
    public long[] Integers { get; init; } = [];
    public Rational[] Rationals { get; init; } = [];
    public double[] Reals { get; init; } = [];

    public int ByteLength => Count * SizeOf(Type);

    /// <summary>Ascii text up to the first NUL, trimmed.</summary>
    public string Text
    {
        get
        {
            int end = Array.IndexOf(Bytes, (byte)0);
            return Encoding.UTF8.GetString(Bytes, 0, end < 0 ? Bytes.Length : end).Trim();
        }
    }

    public long? FirstInteger => Integers.Length > 0 ? Integers[0] : null;

    public double? FirstNumber =>
        Integers.Length > 0 ? Integers[0]
        : Rationals.Length > 0 ? Rationals[0].ToDouble()
        : Reals.Length > 0 ? Reals[0]
        : null;

    /// <summary>Bytes per value, or 0 for a type this reader doesn't know.</summary>
    public static int SizeOf(TiffType type) => type switch
    {
        TiffType.Byte or TiffType.Ascii or TiffType.SByte or TiffType.Undefined => 1,
        TiffType.Short or TiffType.SShort => 2,
        TiffType.Long or TiffType.SLong or TiffType.Float or TiffType.Ifd => 4,
        TiffType.Rational or TiffType.SRational or TiffType.Double => 8,
        _ => 0,
    };
}
