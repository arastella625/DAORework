using DAORework.Core.Looks;
using Xunit;

namespace DAORework.Core.Tests.Looks;

/// <summary>ADR-0017: over any picture, the veil floor keeps every text token readable.</summary>
public sealed class VeilFloorTests
{
    private static readonly LookColor _background = Hex("#120D18");

    // Witch of the Wilds' text tokens with the contrast each needs.
    private static readonly TextRequirement[] _text =
    [
        new(Hex("#E8DFCF"), Contrast.Text), new(Hex("#A99BB8"), Contrast.Text), new(Hex("#A98BFA"), Contrast.Text),
        new(Hex("#E57373"), Contrast.Text), new(Hex("#C9A227"), Contrast.Text), new(Hex("#C9A96E"), Contrast.LargeOrUi),
    ];

    public static TheoryData<string> Pictures()
    {
        var data = new TheoryData<string> { "#FFFFFF", "#000000", "#808080", "#404040", "#FFE9A8", "#2F6B3A" };
        var random = new Random(1709);
        for (var i = 0; i < 20; i++)
        {
            data.Add($"#{random.Next(0x1000000):X6}"); // Noise: any color a picture's brightest part could be.
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(Pictures))]
    public void AtTheFloor_EveryTextTokenPasses(string brightest)
    {
        var floor = VeilFloor.Lowest(_background, Hex(brightest), _text);
        var seen = Contrast.Over(Hex(brightest), _background, floor);

        Assert.All(_text, requirement => Assert.True(Contrast.Ratio(requirement.Color, seen) >= requirement.Needed, $"{requirement.Color} over {brightest} at {floor:P0}"));
    }

    [Fact]
    public void DarkPictures_NeedLessVeilThanBrightOnes()
    {
        var bright = VeilFloor.Lowest(_background, Hex("#808080"), _text);
        var dark = VeilFloor.Lowest(_background, Hex("#404040"), _text);

        Assert.Equal(0.72, bright, precision: 2); // The figures ADR-0017 gives for a mid-grey and a dark picture.
        Assert.Equal(0.35, dark, precision: 2);
    }

    [Fact]
    public void APictureAsDarkAsTheTheme_NeedsNoVeil()
    {
        Assert.Equal(0, VeilFloor.Lowest(_background, _background, _text));
    }

    private static LookColor Hex(string hex) => LookColor.Parse(hex).Value;
}
