using System.Buffers.Binary;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;

namespace DAORework.Core.Formats;

/// <summary>
/// Reads which CPU a Windows executable or DLL is built for. The game is 32-bit, so a DLL it loads from its own folder
/// must be too: the classic mistake is copying DXVK's x64 <c>d3d9.dll</c> into <c>bin_ship</c>.
/// </summary>
public static class PortableExecutableMachine
{
    /// <summary>IMAGE_FILE_MACHINE_I386, the only machine type the 32-bit game can load.</summary>
    public const ushort X86 = 0x014C;

    /// <summary>IMAGE_FILE_MACHINE_AMD64.</summary>
    public const ushort X64 = 0x8664;

    public static Result<ushort> Read(ReadOnlySpan<byte> header)
    {
        if (!PortableExecutableHeader.TryFindCoffHeader(header, out var coffOffset, out var reason))
        {
            return Result.Failure<ushort>(new Error(DiagnosticCodes.KnownItemUnreadable, reason));
        }

        return Result.Success(BinaryPrimitives.ReadUInt16LittleEndian(header[coffOffset..]));
    }

    public static string Describe(ushort machine) => machine switch
    {
        X86 => "32-bit (x86)",
        X64 => "64-bit (x64)",
        _ => $"machine type 0x{machine:X4}",
    };
}
