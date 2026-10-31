using System.Buffers.Binary;

namespace DAORework.Application.Tests.Fixes;

/// <summary>
/// A made-up 32-bit PE image, big enough for the 4 GB patch to read its headers; nothing of the game's. With
/// <c>steamProtected</c> its section table holds <c>.text</c> and <c>.bind</c>, the section Steam's protection adds.
/// </summary>
internal static class TestExecutable
{
    public static byte[] Build(bool largeAddressAware = false, uint securitySize = 0, bool steamProtected = false)
    {
        var bytes = new byte[512];
        bytes[0] = (byte)'M';
        bytes[1] = (byte)'Z';
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(0x3C), 0x80);
        "PE\0\0"u8.CopyTo(bytes.AsSpan(0x80));
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(0x84), 0x014C);
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(0x84 + 16), 224);
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(0x84 + 18), largeAddressAware ? (ushort)0x0122 : (ushort)0x0102);
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(0x98), 0x010B);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(0x98 + 92), 16);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(0x98 + 132), securitySize);
        if (steamProtected)
        {
            BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(0x84 + 2), 2);
            ".text"u8.CopyTo(bytes.AsSpan(0x98 + 224));
            ".bind"u8.CopyTo(bytes.AsSpan(0x98 + 224 + 40));
        }

        return bytes;
    }
}
