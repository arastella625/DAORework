using DAORework.Core.Diagnostics;
using DAORework.Core.Looks;
using Xunit;

namespace DAORework.Core.Tests.Looks;

public sealed class LookColorTests
{
    [Theory]
    [InlineData("#8B5CF6", 0x8B, 0x5C, 0xF6)]
    [InlineData("6d45c4", 0x6D, 0x45, 0xC4)]
    [InlineData("  #120D18 ", 0x12, 0x0D, 0x18)]
    public void Parse_ReadsHexColors(string text, byte r, byte g, byte b)
    {
        var color = LookColor.Parse(text);

        Assert.Equal(new LookColor(r, g, b), color.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("#12345")]
    [InlineData("#GGGGGG")]
    [InlineData("#8B5CF6FF")]
    public void Parse_RefusesWhatIsNotAColor(string? text)
    {
        var color = LookColor.Parse(text);

        Assert.True(color.IsFailure);
        Assert.Equal(DiagnosticCodes.LookColorInvalid, color.Error!.Code);
    }

    [Fact]
    public void ToString_IsTheHexALookStores()
    {
        Assert.Equal("#8B5CF6", new LookColor(0x8B, 0x5C, 0xF6).ToString());
    }

    [Theory]
    [InlineData("#8B5CF6")]
    [InlineData("#9E2B2B")]
    [InlineData("#4F8A6E")]
    [InlineData("#E8DFCF")]
    [InlineData("#7F7F7F")]
    public void Hsl_RoundTrips(string hex)
    {
        var color = LookColor.Parse(hex).Value;

        Assert.Equal(color, LookColor.FromHsl(color.ToHsl()));
    }

    [Fact]
    public void Contrast_MatchesWcagAtItsEnds()
    {
        Assert.Equal(21, Contrast.Ratio(new LookColor(0, 0, 0), new LookColor(255, 255, 255)), precision: 2);
        Assert.Equal(1, Contrast.Ratio(new LookColor(0x8B, 0x5C, 0xF6), new LookColor(0x8B, 0x5C, 0xF6)), precision: 6);
    }

    [Fact]
    public void Over_BlendsByOpacity()
    {
        var black = new LookColor(0, 0, 0);
        var white = new LookColor(255, 255, 255);

        Assert.Equal(white, Contrast.Over(black, white, 1));
        Assert.Equal(black, Contrast.Over(black, white, 0));
        Assert.Equal(new LookColor(128, 128, 128), Contrast.Over(black, white, 0.5));
    }
}
