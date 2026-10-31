using DAORework.Core.Common;
using Xunit;

namespace DAORework.Core.Tests.Common;

public sealed class ByteSizeTests
{
    [Theory]
    [InlineData(0, "0 B")]
    [InlineData(1023, "1023 B")]
    [InlineData(1024, "1 KB")]
    [InlineData(1536, "1.5 KB")]
    [InlineData(1_572_864, "1.5 MB")]
    [InlineData(1_073_741_824, "1 GB")]
    [InlineData(1_099_511_627_776, "1 TB")]
    [InlineData(1_125_899_906_842_624, "1024 TB")]
    public void ToString_RendersWithBinaryUnits(long bytes, string expected)
    {
        Assert.Equal(expected, new ByteSize(bytes).ToString());
    }

    [Fact]
    public void Constructor_NegativeBytes_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ByteSize(-1));
    }

    [Fact]
    public void Equality_IsByValue()
    {
        Assert.Equal(new ByteSize(10), new ByteSize(10));
        Assert.Equal(ByteSize.Zero, new ByteSize(0));
    }
}
