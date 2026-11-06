using DAORework.Application.Common;
using DAORework.Infrastructure.Mods;
using Microsoft.Extensions.Options;
using Xunit;

namespace DAORework.Infrastructure.Tests.Mods;

/// <summary>One window at a time on a data folder (ADR-0044). Each test has a folder of its own, since the lock is the system's.</summary>
public sealed class DataFolderWindowLockTests
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "daorework-lock-" + Guid.NewGuid().ToString("N"));

    [Fact]
    public void ASecondCopy_OnTheSameFolder_IsTurnedAway()
    {
        using var first = Lock(_folder);
        using var second = Lock(_folder);

        Assert.True(first.TryTake());
        Assert.False(second.TryTake());
    }

    [Fact]
    public void ACopy_OnAnotherFolder_Opens()
    {
        using var first = Lock(_folder);
        using var other = Lock(_folder + "-other");

        Assert.True(first.TryTake());
        Assert.True(other.TryTake());
    }

    [Fact]
    public void OnceTheFirstCloses_TheNextOpens()
    {
        var first = Lock(_folder);
        Assert.True(first.TryTake());

        first.Dispose();

        using var next = Lock(_folder);
        Assert.True(next.TryTake());
    }

    private static DataFolderWindowLock Lock(string folder) => new(Options.Create(new DaoReworkOptions { AppDataDirectory = folder }));
}
