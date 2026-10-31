using System.Buffers.Binary;
using DAORework.Core.Diagnostics;
using DAORework.Core.Formats;
using Xunit;

namespace DAORework.Core.Tests.Formats;

public sealed class PortableExecutableTests
{
    private const int PeOffset = 0x80;

    [Theory]
    [InlineData(0x0102, false)]
    [InlineData(0x0122, true)]
    public void IsLargeAddressAware_ReadsTheCharacteristicsFlag(int characteristics, bool expected)
    {
        var result = PortableExecutable.IsLargeAddressAware(Header((ushort)characteristics));

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value);
    }

    [Fact]
    public void IsLargeAddressAware_NotAnExecutable_Fails()
    {
        var result = PortableExecutable.IsLargeAddressAware("just some text, not an exe"u8);

        Assert.Equal(DiagnosticCodes.LargeAddressAwareUnreadable, result.Error!.Code);
    }

    [Fact]
    public void IsLargeAddressAware_PeOffsetBeyondTheBytesRead_Fails()
    {
        var header = Header(0x0122);
        BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(0x3C), 100_000);

        Assert.True(PortableExecutable.IsLargeAddressAware(header).IsFailure);
    }

    [Fact]
    public void IsLargeAddressAware_MissingPeSignature_Fails()
    {
        var header = Header(0x0122);
        header[PeOffset] = (byte)'X';

        Assert.True(PortableExecutable.IsLargeAddressAware(header).IsFailure);
    }

    /// <summary>A minimal valid header: "MZ", e_lfanew at 0x3C, "PE\0\0" at <see cref="PeOffset"/>, characteristics 22 bytes later.</summary>
    internal static byte[] Header(ushort characteristics)
    {
        var bytes = new byte[512];
        bytes[0] = (byte)'M';
        bytes[1] = (byte)'Z';
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(0x3C), PeOffset);
        "PE\0\0"u8.CopyTo(bytes.AsSpan(PeOffset));
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(PeOffset + 22), characteristics);
        return bytes;
    }
}
