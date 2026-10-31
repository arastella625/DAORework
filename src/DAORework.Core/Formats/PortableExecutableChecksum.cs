using System.Buffers.Binary;

namespace DAORework.Core.Formats;

/// <summary>
/// The PE image checksum Windows' <c>CheckSumMappedFile</c> documents: the file summed as little-endian 16-bit words
/// with the carry folded back in, the checksum field itself counted as zero, then the file's length added.
/// </summary>
public static class PortableExecutableChecksum
{
    /// <summary>Where the checksum sits in the optional header, the same in PE32 and PE32+.</summary>
    public const int OptionalHeaderOffset = 64;

    /// <summary>The checksum field's offset in a file whose COFF file header starts at <paramref name="coffOffset"/>.</summary>
    public static int FieldOffset(int coffOffset) => coffOffset + PortableExecutableHeader.CoffHeaderLength + OptionalHeaderOffset;

    public static uint Compute(ReadOnlySpan<byte> image, int fieldOffset)
    {
        ulong sum = 0;
        for (var i = 0; i < image.Length; i += 2)
        {
            sum += (ushort)(ByteAt(image, i, fieldOffset) | (ByteAt(image, i + 1, fieldOffset) << 8));
            sum = (sum & 0xFFFF) + (sum >> 16);
        }

        sum = (sum & 0xFFFF) + (sum >> 16);
        return (uint)(sum + (ulong)image.Length);
    }

    public static uint Read(ReadOnlySpan<byte> image, int fieldOffset) => BinaryPrimitives.ReadUInt32LittleEndian(image[fieldOffset..]);

    private static int ByteAt(ReadOnlySpan<byte> image, int index, int fieldOffset) =>
        index >= image.Length || (index >= fieldOffset && index < fieldOffset + 4) ? 0 : image[index];
}
