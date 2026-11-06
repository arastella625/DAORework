using DAORework.App.Services;
using Xunit;

namespace DAORework.App.Tests;

public sealed class StallWatchTests
{
    [Theory]
    [InlineData(50, false)]
    [InlineData(240, false)]
    [InlineData(300, true)]
    public void IsStall_CountsOnlyWhatIsBeyondTheTick(int gapMilliseconds, bool stalled)
    {
        Assert.Equal(stalled, StallWatch.IsStall(TimeSpan.FromMilliseconds(gapMilliseconds)));
    }
}
