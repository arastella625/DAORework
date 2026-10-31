using System.Text;
using DAORework.Core.Formats;
using Xunit;

namespace DAORework.Core.Tests.Formats;

public sealed class BinaryMarkerTests
{
    /// <summary>The layout read out of DXVK 1.10.3, 2.3 and 3.1.1's d3d9.dll: "Game: ", "DXVK: ", then the version.</summary>
    [Theory]
    [InlineData("v2.3")]
    [InlineData("v1.10.3")]
    [InlineData("v3.1.1")]
    public void TryFind_DxvkLayout_ReturnsTheVersion(string version)
    {
        var data = Bytes($"\0\0junk\0Game: \0DXVK: \0{version}\0Build: \0x86\0");

        Assert.True(BinaryMarker.TryFind(data, "DXVK: ", out var following));
        Assert.Equal(version, following);
    }

    [Fact]
    public void TryFind_PaddingBetweenMarkerAndText_IsSkipped()
    {
        Assert.True(BinaryMarker.TryFind(Bytes("\0DXVK: \0\0\0\0v2.3\0"), "DXVK: ", out var following));
        Assert.Equal("v2.3", following);
    }

    [Fact]
    public void TryFind_MarkerOnlyAsPartOfALongerString_IsNotAMatch()
    {
        var data = Bytes("\0Old DXVK: \0v1.0\0DXVK: shader cache\0");

        Assert.False(BinaryMarker.TryFind(data, "DXVK: ", out _));
    }

    [Fact]
    public void TryFind_LaterWholeStringAfterAPartialOne_IsFound()
    {
        var data = Bytes("\0Old DXVK: \0v1.0\0DXVK: \0v2.3\0");

        Assert.True(BinaryMarker.TryFind(data, "DXVK: ", out var following));
        Assert.Equal("v2.3", following);
    }

    [Fact]
    public void TryFind_MarkerAtTheStartOfTheData_IsFound()
    {
        Assert.True(BinaryMarker.TryFind(Bytes("DXVK: \0v2.3\0"), "DXVK: ", out var following));
        Assert.Equal("v2.3", following);
    }

    [Theory]
    [InlineData("\0DXVK: \0")]
    [InlineData("\0DXVK: \0v2.3")]
    [InlineData("\0DXVK: \0\u0001\u0002\0")]
    public void TryFind_NoReadableTextAfterTheMarker_FindsTheMarkerWithoutText(string text)
    {
        Assert.True(BinaryMarker.TryFind(Bytes(text), "DXVK: ", out var following));
        Assert.Null(following);
    }

    [Fact]
    public void TryFind_TextLongerThanAVersion_IsNotReturned()
    {
        var data = Bytes($"\0DXVK: \0{new string('a', BinaryMarker.MaxFollowingTextLength + 1)}\0");

        Assert.True(BinaryMarker.TryFind(data, "DXVK: ", out var following));
        Assert.Null(following);
    }

    [Fact]
    public void TryFind_NoMarker_ReturnsFalse()
    {
        Assert.False(BinaryMarker.TryFind(Bytes("\0ReShade\0v6.0\0"), "DXVK: ", out var following));
        Assert.Null(following);
    }

    private static byte[] Bytes(string text) => Encoding.Latin1.GetBytes(text);
}
