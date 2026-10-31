using System.Buffers.Binary;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Formats;

namespace DAORework.Core.Fixes;

/// <summary>
/// The 4 GB patch (ADR-0013): sets IMAGE_FILE_LARGE_ADDRESS_AWARE in a 32-bit executable's COFF characteristics and
/// recomputes the optional header's checksum when it was set, as NTCore's 4GB Patch does. No other byte changes.
/// </summary>
public static class LargeAddressAwarePatch
{
    private const ushort LargeAddressAwareFlag = 0x0020;
    private const int CharacteristicsOffset = 18;
    private const int OptionalHeaderSizeOffset = 16;
    private const ushort Pe32Magic = 0x010B;

    /// <summary>NumberOfRvaAndSizes in the PE32 optional header: how many data directories follow it.</summary>
    private const int DirectoryCountOffset = 92;

    /// <summary>The size of data directory 4, the Authenticode security directory (directories start at 96, 8 bytes each).</summary>
    private const int SecurityDirectorySizeOffset = 96 + (4 * 8) + 4;

    /// <summary>The PE32 optional header up to and including the security directory.</summary>
    private const int OptionalHeaderBytesNeeded = SecurityDirectorySizeOffset + 4;

    public static Result<LargeAddressAwarePatchResult> Apply(ReadOnlySpan<byte> image)
    {
        if (!PortableExecutableHeader.TryFindCoffHeader(image, out var coff, out var reason))
        {
            return Invalid(reason);
        }

        var optional = coff + PortableExecutableHeader.CoffHeaderLength;
        var problem = Check(image, coff, optional);
        if (problem is not null)
        {
            return Invalid(problem);
        }

        var patched = image.ToArray();
        var characteristics = BinaryPrimitives.ReadUInt16LittleEndian(image[(coff + CharacteristicsOffset)..]);
        var alreadySet = (characteristics & LargeAddressAwareFlag) != 0;
        if (!alreadySet)
        {
            BinaryPrimitives.WriteUInt16LittleEndian(patched.AsSpan(coff + CharacteristicsOffset), (ushort)(characteristics | LargeAddressAwareFlag));
            var checksumField = PortableExecutableChecksum.FieldOffset(coff);
            if (PortableExecutableChecksum.Read(image, checksumField) != 0)
            {
                BinaryPrimitives.WriteUInt32LittleEndian(patched.AsSpan(checksumField), PortableExecutableChecksum.Compute(patched, checksumField));
            }
        }

        return Result.Success(new LargeAddressAwarePatchResult(patched, alreadySet, IsSigned(image, optional)));
    }

    private static string? Check(ReadOnlySpan<byte> image, int coff, int optional)
    {
        var machine = BinaryPrimitives.ReadUInt16LittleEndian(image[coff..]);
        if (machine != PortableExecutableMachine.X86)
        {
            return $"It is a {PortableExecutableMachine.Describe(machine)} executable; the 4 GB patch is for the 32-bit game.";
        }

        var optionalSize = BinaryPrimitives.ReadUInt16LittleEndian(image[(coff + OptionalHeaderSizeOffset)..]);
        if (optionalSize < OptionalHeaderBytesNeeded || image.Length < optional + OptionalHeaderBytesNeeded)
        {
            return "Its optional header is missing or too short.";
        }

        return BinaryPrimitives.ReadUInt16LittleEndian(image[optional..]) == Pe32Magic ? null : "It is not a PE32 executable.";
    }

    /// <summary>Whether the image carries an Authenticode signature (a non-empty security directory), which the patch invalidates.</summary>
    private static bool IsSigned(ReadOnlySpan<byte> image, int optional) =>
        BinaryPrimitives.ReadUInt32LittleEndian(image[(optional + DirectoryCountOffset)..]) > 4
        && BinaryPrimitives.ReadUInt32LittleEndian(image[(optional + SecurityDirectorySizeOffset)..]) != 0;

    private static Result<LargeAddressAwarePatchResult> Invalid(string reason) =>
        Result.Failure<LargeAddressAwarePatchResult>(new Error(DiagnosticCodes.FixSourceInvalid, $"The game executable cannot be patched: {reason}"));
}
