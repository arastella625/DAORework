using DAORework.Core.Mods;
using Xunit;

namespace DAORework.Core.Tests.Mods;

/// <summary>The cozy guide's steps and when it shows (P2.9-03, ADR-0051).</summary>
public sealed class CozyGuideTests
{
    [Theory]
    [InlineData(false, 0, 0, GuideStepKind.FindYourGame)]
    [InlineData(true, 0, 0, GuideStepKind.AddMods)]
    [InlineData(true, 3, 0, GuideStepKind.TurnThemOn)]
    [InlineData(true, 3, 1, GuideStepKind.Play)]
    public void Current_IsTheFirstStepNotDone(bool profile, int library, int on, GuideStepKind expected)
    {
        var steps = CozyGuide.For(new NextMoveFacts(profile, false, false, 0, library, on));

        Assert.Equal(expected, steps[CozyGuide.Current(steps)].Kind);
    }

    [Fact]
    public void For_HasFourSteps_EachWithAButton_AndNoWordVoiceForbids()
    {
        var steps = CozyGuide.For(new NextMoveFacts(true, false, false, 0, 0, 0));

        Assert.Equal([GuideStepKind.FindYourGame, GuideStepKind.AddMods, GuideStepKind.TurnThemOn, GuideStepKind.Play], steps.Select(s => s.Kind));
        Assert.All(steps, s => Assert.False(string.IsNullOrWhiteSpace(s.Button)));
        Assert.All(steps, s => Assert.DoesNotMatch(@"(?i)\b(simply|just|tutorial|wizard|onboarding)\b", s.Title + " " + s.Text));
        Assert.All(steps, s => Assert.InRange(s.Text.Split(". ").Length, 1, 2)); // Two sentences at most.
    }

    [Fact]
    public void For_ModsFoundInTheGame_CountAsAdded()
    {
        var steps = CozyGuide.For(new NextMoveFacts(true, false, false, 4, 0, 0));

        Assert.True(steps[1].IsDone);
    }

    [Theory]
    [InlineData(false, 0, false, true)]
    [InlineData(true, 0, false, false)] // Skipped or finished: for good.
    [InlineData(false, 2, false, false)] // Mods on already, and it wasn't seen: a player who knows the way.
    [InlineData(false, 2, true, true)] // Seen, then a mod went on: on to Play.
    public void Shows_UntilClosed_WhileNoModIsOn_OrOnToPlay(bool closed, int on, bool seen, bool expected)
    {
        Assert.Equal(expected, CozyGuide.Shows(closed, new NextMoveFacts(true, false, false, 0, 3, on), seen));
    }
}
