using DAORework.Core.Looks;
using Xunit;

namespace DAORework.Core.Tests.Looks;

public sealed class PictureTreatmentTests
{
    private static readonly LookColor _background = Hex("#120D18");
    private static readonly LookColor _accent = Hex("#6D45C4");
    private static readonly TextRequirement[] _text = [new(Hex("#E8DFCF"), Contrast.Text), new(Hex("#E57373"), Contrast.Text)];

    [Fact]
    public void NoTint_LeavesThePictureAsItIs()
    {
        Assert.Equal(Hex("#FFFFFF"), PictureTreatment.Tinted(Hex("#FFFFFF"), _background, _accent, 0));
    }

    [Fact]
    public void FullTint_MapsBlackToTheBackgroundAndWhiteToTheAccent()
    {
        Assert.Equal(_background, PictureTreatment.Tinted(Hex("#000000"), _background, _accent, 100));
        Assert.Equal(_accent, PictureTreatment.Tinted(Hex("#FFFFFF"), _background, _accent, 100));
    }

    [Fact]
    public void MoreTint_NeedsLessVeilOverAWhitePicture()
    {
        var plain = PictureTreatment.DimFloor(Hex("#FFFFFF"), _background, _accent, 0, _text);
        var tinted = PictureTreatment.DimFloor(Hex("#FFFFFF"), _background, _accent, 80, _text);

        Assert.True(tinted < plain, $"{tinted} should be below {plain}");
    }

    [Fact]
    public void TheFloor_IsAWholePercentAtWhichTheTextPasses()
    {
        var floor = PictureTreatment.DimFloor(Hex("#FFE9A8"), _background, _accent, 30, _text);
        var seen = Contrast.Over(PictureTreatment.Tinted(Hex("#FFE9A8"), _background, _accent, 30), _background, floor / 100d);

        Assert.InRange(floor, 0, 100);
        Assert.All(_text, requirement => Assert.True(Contrast.Ratio(requirement.Color, seen) >= requirement.Needed));
    }

    [Theory]
    [InlineData("#FFFFFF", 40)]
    [InlineData("#2F6B3A", 70)]
    [InlineData("#D8C9A0", 100)]
    public void TintRgba_MatchesTintedOnePixelAtATime(string hex, int tint)
    {
        var pixel = Hex(hex);
        byte[] rgba = [pixel.R, pixel.G, pixel.B, 200];

        PictureTreatment.TintRgba(rgba, _background, _accent, tint);

        var expected = PictureTreatment.Tinted(pixel, _background, _accent, tint);
        Assert.InRange(rgba[0], expected.R - 1, expected.R + 1); // Tinted rounds twice, TintRgba once.
        Assert.InRange(rgba[1], expected.G - 1, expected.G + 1);
        Assert.InRange(rgba[2], expected.B - 1, expected.B + 1);
        Assert.Equal(200, rgba[3]);
    }

    private static LookColor Hex(string hex) => LookColor.Parse(hex).Value;
}
