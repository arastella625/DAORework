using DAORework.App.Services;
using Xunit;

namespace DAORework.App.Tests;

public sealed class StallWatchTests
{
    [Theory]
    [InlineData(200, false)]
    [InlineData(390, false)]
    [InlineData(450, true)]
    public void IsStall_CountsOnlyWhatIsBeyondTheTick(int gapMilliseconds, bool stalled)
    {
        Assert.Equal(stalled, StallWatch.IsStall(TimeSpan.FromMilliseconds(gapMilliseconds)));
    }
}
