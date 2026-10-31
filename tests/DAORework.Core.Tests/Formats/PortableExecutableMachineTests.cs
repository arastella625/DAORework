using System.Buffers.Binary;
using DAORework.Core.Diagnostics;
using DAORework.Core.Formats;
using Xunit;

namespace DAORework.Core.Tests.Formats;

public sealed class PortableExecutableMachineTests
{
    [Theory]
    [InlineData(PortableExecutableMachine.X86)]
    [InlineData(PortableExecutableMachine.X64)]
    public void Read_ReturnsTheMachineField(ushort machine)
    {
        Assert.Equal(machine, PortableExecutableMachine.Read(Header(machine)).Value);
    }

    [Fact]
    public void Read_NotAnExecutable_FailsWithTheKnownItemCode()
    {
        Assert.Equal(DiagnosticCodes.KnownItemUnreadable, PortableExecutableMachine.Read("plain text"u8).Error!.Code);
    }

    [Theory]
    [InlineData(PortableExecutableMachine.X86, "32-bit (x86)")]
    [InlineData(PortableExecutableMachine.X64, "64-bit (x64)")]
    [InlineData(0xAA64, "machine type 0xAA64")]
    public void Describe_NamesTheMachine(ushort machine, string expected)
    {
        Assert.Equal(expected, PortableExecutableMachine.Describe(machine));
    }

    /// <summary>A header whose COFF machine field (right after "PE\0\0") is set; it reuses the LAA test layout.</summary>
    internal static byte[] Header(ushort machine)
    {
        var bytes = PortableExecutableTests.Header(0x0102);
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(0x80 + 4), machine);
        return bytes;
    }
}
