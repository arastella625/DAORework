using DAORework.App.Views;
using Xunit;

namespace DAORework.App.Tests;

/// <summary>Times read as a person says them (docs/VOICE.md rule 5).</summary>
public sealed class FriendlyTimeTests
{
    private static readonly DateTimeOffset _now = new(2026, 11, 1, 18, 30, 0, TimeSpan.FromHours(1)); // A Sunday.

    [Theory]
    [InlineData(0, "Just now")]
    [InlineData(1, "1 minute ago")]
    [InlineData(25, "25 minutes ago")]
    [InlineData(150, "Today, 16:00")]
    public void Recent_ReadsAsMinutesOrToday(int minutesAgo, string expected) =>
        Assert.Equal(expected, FriendlyTime.Of(_now.AddMinutes(-minutesAgo), _now));

    [Fact]
    public void Earlier_ReadsAsYesterdayAWeekdayADateOrAYear()
    {
        Assert.Equal("Yesterday, 21:40", FriendlyTime.Of(new DateTimeOffset(2026, 10, 31, 21, 40, 0, _now.Offset), _now));
        Assert.Equal("Tuesday, 19:05", FriendlyTime.Of(new DateTimeOffset(2026, 10, 27, 19, 5, 0, _now.Offset), _now));
        Assert.Equal("3 Oct, 09:12", FriendlyTime.Of(new DateTimeOffset(2026, 10, 3, 9, 12, 0, _now.Offset), _now));
        Assert.Equal("3 Oct 2025", FriendlyTime.Of(new DateTimeOffset(2025, 10, 3, 9, 12, 0, _now.Offset), _now));
    }

    [Fact]
    public void ALaterTime_FromAClockThatMoved_ReadsAsToday() =>
        Assert.Equal("Today, 18:45", FriendlyTime.Of(_now.AddMinutes(15), _now));
}
