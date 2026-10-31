using System.Buffers.Binary;
using DAORework.Core.Diagnostics;
using DAORework.Core.Fixes;
using DAORework.Core.Formats;
using Xunit;

namespace DAORework.Core.Tests.Fixes;

public sealed class LargeAddressAwarePatchTests
{
    [Fact]
    public void Apply_SetsTheFlagAndRecomputesTheChecksumAndNothingElse()
    {
        var original = SyntheticImage.Build();

        var result = LargeAddressAwarePatch.Apply(original).Value;

        Assert.False(result.WasAlreadySet);
        Assert.True(PortableExecutable.IsLargeAddressAware(result.Image).Value);
        Assert.Equal(0x1EBCu, BinaryPrimitives.ReadUInt32LittleEndian(result.Image.AsSpan(SyntheticImage.ChecksumOffset)));
        Assert.All(ChangedBytes(original, result.Image), i => Assert.True(i == SyntheticImage.CharacteristicsOffset || (i >= SyntheticImage.ChecksumOffset && i < SyntheticImage.ChecksumOffset + 4), $"byte {i} changed"));
    }

    [Fact]
    public void Apply_ChecksumOfZero_StaysZero()
    {
        var original = SyntheticImage.Build();
        BinaryPrimitives.WriteUInt32LittleEndian(original.AsSpan(SyntheticImage.ChecksumOffset), 0);

        var patched = LargeAddressAwarePatch.Apply(original).Value.Image;

        Assert.Equal([SyntheticImage.CharacteristicsOffset], ChangedBytes(original, patched));
    }

    [Fact]
    public void Apply_FlagAlreadySet_ChangesNothing()
    {
        var original = SyntheticImage.Build(characteristics: 0x0122);

        var result = LargeAddressAwarePatch.Apply(original).Value;

        Assert.True(result.WasAlreadySet);
        Assert.Equal(original, result.Image);
    }

    [Theory]
    [InlineData(0u, false)]
    [InlineData(0x2000u, true)]
    public void Apply_ReportsAnAuthenticodeSignature(uint securitySize, bool expected)
    {
        Assert.Equal(expected, LargeAddressAwarePatch.Apply(SyntheticImage.Build(securitySize: securitySize)).Value.IsSigned);
    }

    [Fact]
    public void Apply_FewDataDirectories_IsNotSigned()
    {
        Assert.False(LargeAddressAwarePatch.Apply(SyntheticImage.Build(directoryCount: 4, securitySize: 0x2000)).Value.IsSigned);
    }

    [Theory]
    [InlineData(0x8664, 0x010B, 224, "64-bit")]
    [InlineData(0x014C, 0x020B, 240, "not a PE32")]
    [InlineData(0x014C, 0x010B, 0, "too short")]
    public void Apply_NotA32BitPe32Image_IsRefused(int machine, int magic, int optionalSize, string reason)
    {
        var result = LargeAddressAwarePatch.Apply(SyntheticImage.Build(machine: (ushort)machine, magic: (ushort)magic, optionalSize: (ushort)optionalSize));

        Assert.Equal(DiagnosticCodes.FixSourceInvalid, result.Error!.Code);
        Assert.Contains(reason, result.Error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Apply_NotAnExecutable_IsRefused()
    {
        Assert.Equal(DiagnosticCodes.FixSourceInvalid, LargeAddressAwarePatch.Apply("not an exe"u8).Error!.Code);
    }

    [Fact]
    public void Apply_ImageEndingInsideTheOptionalHeader_IsRefused()
    {
        var truncated = SyntheticImage.Build()[..(SyntheticImage.OptionalOffset + 100)];

        Assert.Equal(DiagnosticCodes.FixSourceInvalid, LargeAddressAwarePatch.Apply(truncated).Error!.Code);
    }

    private static List<int> ChangedBytes(byte[] before, byte[] after) =>
        Enumerable.Range(0, before.Length).Where(i => before[i] != after[i]).ToList();
}
