using DAORework.FaultInjection.Tests.Gate;
using Xunit;

namespace DAORework.FaultInjection.Tests;

/// <summary>The harness itself: the child CLI counts its writes, and dies before the one asked for with nothing after it written.</summary>
public sealed class KillingFileSystemTests
{
    [Fact]
    public async Task Import_WithoutAKill_CountsItsWrites()
    {
        using var folders = new GateFolders();
        var ids = GateFixtures.Prepare(folders);
        var library = Path.Combine(folders.AppData, "library");
        Directory.Delete(library, recursive: true);

        var run = await ChildCli.RunAsync(folders.AppData, ChildCli.NeverKill, ["mods", "import", Path.Combine(folders.Downloads, "addin.dazip")]);

        Assert.Equal(0, run.ExitCode);
        Assert.False(run.Killed);
        Assert.True(run.Writes > 0, run.Output);
        Assert.True(File.Exists(Path.Combine(library, ids["{addin}"] + ".mod.json")));
    }

    [Fact]
    public async Task Import_KilledBeforeItsFirstWrite_WritesNothing()
    {
        using var folders = new GateFolders();
        GateFixtures.Prepare(folders);
        var library = Path.Combine(folders.AppData, "library");
        Directory.Delete(library, recursive: true);

        var run = await ChildCli.RunAsync(folders.AppData, 0, ["mods", "import", Path.Combine(folders.Downloads, "addin.dazip")]);

        Assert.True(run.Killed, run.Output);
        Assert.NotEqual(0, run.ExitCode);
        Assert.False(Directory.Exists(library));
    }
}
