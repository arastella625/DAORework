using System.Buffers.Binary;

namespace DAORework.Core.Tests.Mods.Programs;

/// <summary>Made-up Windows program headers: just enough of a DOS stub and PE header to say a CPU and whether it's a DLL.</summary>
internal static class TestPe
{
    public const ushort X86 = 0x014C;
    public const ushort X64 = 0x8664;

    public static byte[] Build(ushort machine = X86, bool dll = true)
    {
        var header = new byte[256];
        "MZ"u8.CopyTo(header);
        BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(0x3C), 0x80);
        "PE\0\0"u8.CopyTo(header.AsSpan(0x80));
        BinaryPrimitives.WriteUInt16LittleEndian(header.AsSpan(0x84), machine);
        BinaryPrimitives.WriteUInt16LittleEndian(header.AsSpan(0x84 + 18), dll ? (ushort)0x2102 : (ushort)0x0102);
        return header;
    }
}
