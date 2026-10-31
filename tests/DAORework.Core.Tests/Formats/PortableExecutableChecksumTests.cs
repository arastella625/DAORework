using DAORework.Core.Formats;
using DAORework.Core.Tests.Fixes;
using Xunit;

namespace DAORework.Core.Tests.Formats;

public sealed class PortableExecutableChecksumTests
{
    [Theory]
    [InlineData(new byte[] { 1, 0, 2, 0 }, 7u)]
    [InlineData(new byte[] { 0xFF, 0xFF, 2, 0 }, 6u)]
    [InlineData(new byte[] { 1, 0, 5 }, 9u)]
    public void Compute_SumsWordsFoldsTheCarryAndAddsTheLength(byte[] image, uint expected)
    {
        Assert.Equal(expected, PortableExecutableChecksum.Compute(image, fieldOffset: 100));
    }

    [Fact]
    public void Compute_CountsTheChecksumFieldAsZero()
    {
        byte[] image = [1, 0, 9, 9, 9, 9, 2, 0];

        Assert.Equal(11u, PortableExecutableChecksum.Compute(image, fieldOffset: 2));
    }

    [Theory]
    [InlineData(0x0102, 1024, 0x1E9Cu)]
    [InlineData(0x0122, 1024, 0x1EBCu)]
    [InlineData(0x0102, 1023, 0xC09Au)]
    public void Compute_MatchesPefileOnASyntheticImage(int characteristics, int length, uint expected)
    {
        var image = SyntheticImage.Build((ushort)characteristics, length);

        Assert.Equal(expected, PortableExecutableChecksum.Compute(image, PortableExecutableChecksum.FieldOffset(SyntheticImage.CoffOffset)));
    }
}
