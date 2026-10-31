using System.Buffers.Binary;

namespace DAORework.Core.Formats;

/// <summary>Locates the COFF file header behind the DOS stub, the part of the PE layout every reader here starts from.</summary>
internal static class PortableExecutableHeader
{
    /// <summary>The COFF file header's size: machine, sections, timestamp, symbols, optional-header size, characteristics.</summary>
    public const int CoffHeaderLength = 20;

    private const ushort DosMagic = 0x5A4D; // "MZ"
    private const uint PeSignature = 0x00004550; // "PE\0\0"

    public static bool TryFindCoffHeader(ReadOnlySpan<byte> header, out int coffOffset, out string reason)
    {
        coffOffset = 0;
        if (header.Length < 0x40 || BinaryPrimitives.ReadUInt16LittleEndian(header) != DosMagic)
        {
            reason = "The file does not start with a DOS/PE header.";
            return false;
        }

        var peOffset = BinaryPrimitives.ReadInt32LittleEndian(header[0x3C..]);
        if (peOffset < 0x40 || peOffset > header.Length - 4 - CoffHeaderLength)
        {
            reason = $"The PE header offset {peOffset} is outside the bytes read.";
            return false;
        }

        if (BinaryPrimitives.ReadUInt32LittleEndian(header[peOffset..]) != PeSignature)
        {
            reason = "The PE signature was not found where the DOS header points.";
            return false;
        }

        coffOffset = peOffset + 4;
        reason = string.Empty;
        return true;
    }
}
