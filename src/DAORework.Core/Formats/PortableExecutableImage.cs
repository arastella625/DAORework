using System.Buffers.Binary;

namespace DAORework.Core.Formats;

/// <summary>
/// What a Windows program's header says it is: the CPU it is built for, and whether it is a DLL. A mod that runs inside
/// the game may only place DLLs (ADR-0033 §2), whatever their extension, so a renamed <c>.exe</c> is told apart here.
/// </summary>
public sealed record PortableExecutableImage(ushort Machine, bool IsDll)
{
    /// <summary>IMAGE_FILE_DLL in the COFF header's characteristics.</summary>
    private const ushort DllCharacteristic = 0x2000;

    /// <summary>Offset of the characteristics within the COFF header.</summary>
    private const int CharacteristicsOffset = 18;

    /// <summary>Reads a file's first bytes; false when they are not a whole DOS and PE header.</summary>
    public static bool TryRead(ReadOnlySpan<byte> header, out PortableExecutableImage? image)
    {
        image = null;
        if (!PortableExecutableHeader.TryFindCoffHeader(header, out var coff, out _))
        {
            return false;
        }

        var machine = BinaryPrimitives.ReadUInt16LittleEndian(header[coff..]);
        var characteristics = BinaryPrimitives.ReadUInt16LittleEndian(header[(coff + CharacteristicsOffset)..]);
        image = new PortableExecutableImage(machine, (characteristics & DllCharacteristic) != 0);
        return true;
    }
}
