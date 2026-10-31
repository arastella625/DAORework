using System.Buffers.Binary;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;

namespace DAORework.Core.Formats;

/// <summary>
/// Reads the one thing DAORework needs from a Windows executable's header: the large-address-aware flag,
/// which lets a 32-bit game use 4 GB instead of 2 GB. Pure byte arithmetic over the PE format's public layout.
/// </summary>
public static class PortableExecutable
{
    /// <summary>The bytes needed to reach the COFF characteristics for any sane header: the DOS stub rarely exceeds 1 KB.</summary>
    public const int HeaderBytesNeeded = 4096;

    private const ushort LargeAddressAwareFlag = 0x0020;

    /// <summary>Where the characteristics field sits in the COFF file header.</summary>
    private const int CharacteristicsOffset = 18;

    /// <summary>Whether the executable whose leading bytes are given carries IMAGE_FILE_LARGE_ADDRESS_AWARE.</summary>
    public static Result<bool> IsLargeAddressAware(ReadOnlySpan<byte> header)
    {
        if (!PortableExecutableHeader.TryFindCoffHeader(header, out var coffOffset, out var reason))
        {
            return Failure(reason);
        }

        var characteristics = BinaryPrimitives.ReadUInt16LittleEndian(header[(coffOffset + CharacteristicsOffset)..]);
        return Result.Success((characteristics & LargeAddressAwareFlag) != 0);
    }

    private static Result<bool> Failure(string message) =>
        Result.Failure<bool>(new Error(DiagnosticCodes.LargeAddressAwareUnreadable, message));
}
