using System.Buffers.Binary;

namespace DAORework.Core.Tests.Fixes;

/// <summary>
/// A made-up 32-bit PE image: filler bytes <c>(i * 31 + 7) % 251</c>, then the headers the 4 GB patch reads. The
/// expected checksums in the tests come from the independent pefile library (2024.8.26) over these same bytes.
/// </summary>
internal static class SyntheticImage
{
    public const int CoffOffset = 0x84;
    public const int CharacteristicsOffset = CoffOffset + 18;
    public const int OptionalOffset = CoffOffset + 20;
    public const int ChecksumOffset = OptionalOffset + 64;

    public static byte[] Build(ushort characteristics = 0x0102, int length = 1024, ushort machine = 0x014C, ushort magic = 0x010B, ushort optionalSize = 224, uint directoryCount = 16, uint securitySize = 0)
    {
        var bytes = new byte[length];
        for (var i = 0; i < length; i++)
        {
            bytes[i] = (byte)(((i * 31) + 7) % 251);
        }

        bytes[0] = (byte)'M';
        bytes[1] = (byte)'Z';
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(0x3C), 0x80);
        "PE\0\0"u8.CopyTo(bytes.AsSpan(0x80));
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(CoffOffset), machine);
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(CoffOffset + 2), 0);
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(CoffOffset + 16), optionalSize);
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(CharacteristicsOffset), characteristics);
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(OptionalOffset), magic);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(OptionalOffset + 92), directoryCount);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(OptionalOffset + 132), securitySize);
        return bytes;
    }
}
