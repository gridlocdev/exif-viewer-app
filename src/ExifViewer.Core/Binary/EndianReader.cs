using System.Buffers.Binary;

namespace ExifViewer.Core.Binary;

/// <summary>
/// Bounds-checked reads over a byte buffer in one byte order. A read past the end throws
/// <see cref="MalformedDataException"/>, which readers catch at the level where they can carry on.
/// </summary>
internal readonly struct EndianReader
{
    private readonly ReadOnlyMemory<byte> data;

    public EndianReader(ReadOnlyMemory<byte> data, bool bigEndian)
    {
        this.data = data;
        BigEndian = bigEndian;
    }

    public bool BigEndian { get; }
    public int Length => data.Length;

    public bool Has(long offset, long count) => offset >= 0 && count >= 0 && offset + count <= data.Length;

    public ReadOnlySpan<byte> Span(long offset, long count) => Slice(offset, count).Span;

    public ReadOnlyMemory<byte> Slice(long offset, long count)
    {
        if (!Has(offset, count))
            throw new MalformedDataException($"Read of {count} bytes at offset {offset} runs past the end ({data.Length} bytes).");
        return data.Slice((int)offset, (int)count);
    }

    public byte U8(long offset) => Span(offset, 1)[0];

    public ushort U16(long offset)
    {
        var s = Span(offset, 2);
        return BigEndian ? BinaryPrimitives.ReadUInt16BigEndian(s) : BinaryPrimitives.ReadUInt16LittleEndian(s);
    }

    public uint U32(long offset)
    {
        var s = Span(offset, 4);
        return BigEndian ? BinaryPrimitives.ReadUInt32BigEndian(s) : BinaryPrimitives.ReadUInt32LittleEndian(s);
    }

    public ulong U64(long offset)
    {
        var s = Span(offset, 8);
        return BigEndian ? BinaryPrimitives.ReadUInt64BigEndian(s) : BinaryPrimitives.ReadUInt64LittleEndian(s);
    }

    public short S16(long offset) => unchecked((short)U16(offset));
    public int S32(long offset) => unchecked((int)U32(offset));

    public float F32(long offset)
    {
        var s = Span(offset, 4);
        return BigEndian ? BinaryPrimitives.ReadSingleBigEndian(s) : BinaryPrimitives.ReadSingleLittleEndian(s);
    }

    public double F64(long offset)
    {
        var s = Span(offset, 8);
        return BigEndian ? BinaryPrimitives.ReadDoubleBigEndian(s) : BinaryPrimitives.ReadDoubleLittleEndian(s);
    }

    /// <summary>True when the bytes at <paramref name="offset"/> equal <paramref name="expected"/>.</summary>
    public bool Matches(long offset, ReadOnlySpan<byte> expected) =>
        Has(offset, expected.Length) && Span(offset, expected.Length).SequenceEqual(expected);
}

/// <summary>The file's structure doesn't match its format (truncated, bad offset, bad length).</summary>
public sealed class MalformedDataException(string message) : Exception(message);
