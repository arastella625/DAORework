using DAORework.App.ViewModels;
using Xunit;

namespace DAORework.App.Tests;

/// <summary>Checking the game on open and after each change, never two at once (P2.10-02, ADR-0050 §1).</summary>
public sealed class GameRecheckTests
{
    [Fact]
    public async Task Request_WhileACheckRuns_RunsOneMoreAfterIt_NeverTwoAtOnce()
    {
        var started = 0;
        var running = 0;
        var most = 0;
        var first = new TaskCompletionSource();
        var recheck = new GameRecheck(async () =>
        {
            started++;
            most = Math.Max(most, ++running);
            if (started == 1)
            {
                await first.Task;
            }

            running--;
        });

        recheck.Request();
        recheck.Request(); // Two changes while the first check runs...
        recheck.Request();
        first.SetResult();
        await recheck.Running;

        Assert.Equal(2, started); // ...make one more check, not two, and it checks the last change.
        Assert.Equal(1, most);
    }

    [Fact]
    public async Task Request_WhenIdle_StartsACheck()
    {
        var started = 0;
        var recheck = new GameRecheck(() =>
        {
            started++;
            return Task.CompletedTask;
        });

        recheck.Request();
        await recheck.Running;
        recheck.Request();
        await recheck.Running;

        Assert.Equal(2, started);
    }
}
