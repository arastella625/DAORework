using System.Text.Json;
using DAORework.FaultInjection.Tests.Gate;
using Xunit;

namespace DAORework.FaultInjection.Tests;

/// <summary>
/// <c>mods vortex copy</c> killed before each of its writes (ADR-0014, ADR-0028 §3). It writes only DAORework's own
/// folder, so there is no journal: the game must be exactly as it was, and running it again must finish the job. Turning
/// the copies on is <c>mods activate</c>, which the exit gate already covers.
/// </summary>
public sealed class VortexCopyGateTests
{
    private const string Winner = "Blue Hair-1-1-0";
    private const string Loser = "Red Hair-2-1-0";

    private static readonly string[] _copy = ["mods", "vortex", "copy", "-p", GateFixtures.Profile];
    private static readonly string[] _state = ["mods", "vortex", "-p", GateFixtures.Profile];

    [Fact]
    public async Task Copy_KilledBeforeAnyWrite_LeavesTheGameAlone_AndRunningItAgainFinishes()
    {
        int writes;
        string expected;
        using (var folders = new GateFolders())
        {
            Prepare(folders);
            var clean = await ChildCli.RunAsync(folders.AppData, ChildCli.NeverKill, _copy);
            Assert.True(clean.ExitCode == 0 && clean.Writes > 0, clean.Output);
            writes = clean.Writes;
            expected = InProcessCli.Run(folders.AppData, _state).Output;
        }

        Assert.True(writes >= 10, $"Only {writes} writes: the scenario hardly writes.");
        Assert.Contains("Purge", expected, StringComparison.Ordinal);
        await Parallel.ForEachAsync(Enumerable.Range(0, writes), new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, async (write, _) =>
        {
            using var folders = new GateFolders();
            Prepare(folders);
            var before = FolderState.Take(folders);

            var killed = await ChildCli.RunAsync(folders.AppData, write, _copy);

            Assert.True(killed.Killed, $"Not killed before write {write}: {killed.Output}");
            Assert.Equal(before, FolderState.Take(folders));
            Assert.Equal(0, InProcessCli.Run(folders.AppData, _copy).ExitCode);
            Assert.Equal(expected, InProcessCli.Run(folders.AppData, _state).Output);
            Assert.Equal(before, FolderState.Take(folders));
        });
    }

    private static void Prepare(GateFolders folders)
    {
        GateFixtures.Prepare(folders);
        var staging = Path.Combine(folders.Root, "vortex");
        var target = Path.Combine(folders.Documents, "packages", "core", "override");
        Directory.CreateDirectory(target);
        foreach (var (mod, file, text) in new[] { (Winner, "gate_hair.dds", "blue"), (Loser, "gate_hair.dds", "red"), (Loser, "gate_red.dds", "more") })
        {
            Directory.CreateDirectory(Path.Combine(staging, mod));
            File.WriteAllText(Path.Combine(staging, mod, file), text);
        }

        File.WriteAllText(Path.Combine(target, "gate_hair.dds"), "blue");
        File.WriteAllText(Path.Combine(target, "gate_red.dds"), "more");
        File.WriteAllText(Path.Combine(target, "vortex.deployment.json"), JsonSerializer.Serialize(new
        {
            targetPath = target,
            stagingPath = staging,
            files = new[] { new { relPath = "gate_hair.dds", source = Winner }, new { relPath = "gate_red.dds", source = Loser } },
        }));
    }
}
