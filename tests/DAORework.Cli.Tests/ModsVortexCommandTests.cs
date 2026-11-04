using System.IO.Abstractions.TestingHelpers;
using System.Text.Json;
using DAORework.Core.Common;
using Xunit;

namespace DAORework.Cli.Tests;

/// <summary><c>mods vortex</c> and <c>mods vortex copy</c> (ADR-0028 §3 and §5), over the real composition.</summary>
public sealed class ModsVortexCommandTests : IDisposable
{
    private const string Winner = "Blue Hair-1-1-0";
    private const string Loser = "Red Hair-2-1-0";

    private static readonly string _documents = MockUnixSupport.Path(@"D:\Docs\BioWare\Dragon Age");
    private static readonly string _staging = MockUnixSupport.Path(@"E:\VortexMods\dragonage");
    private static readonly string _override = GamePaths.OverrideDirectory(_documents);

    private readonly CliTestHost _host = new();

    public ModsVortexCommandTests()
    {
        _host.FileSystem.AddFile(Path.Combine(_staging, Winner, "hair.dds"), new MockFileData("blue"));
        _host.FileSystem.AddFile(Path.Combine(_staging, Loser, "hair.dds"), new MockFileData("red"));
        _host.FileSystem.AddFile(Path.Combine(_staging, Loser, "red.dds"), new MockFileData("more"));
        _host.FileSystem.AddFile(Path.Combine(_override, "hair.dds"), new MockFileData("blue"));
        _host.FileSystem.AddFile(Path.Combine(_override, "red.dds"), new MockFileData("more"));
        _host.FileSystem.AddFile(Path.Combine(_override, "vortex.deployment.json"), new MockFileData(Record(("hair.dds", Winner), ("red.dds", Loser))));
        _host.FileSystem.AddFile(Path.Combine(_documents, "Settings", "AddIns.xml"), new MockFileData("<AddInsList />"));
        _host.Run("init", "Default");
        _host.Run("profile", "set-path", "Default", "documents", _documents);
    }

    public void Dispose() => _host.Dispose();

    [Fact]
    public void Vortex_ListsTheModsAndTheNextStep_AndChangesNothing()
    {
        var before = _host.FileSystem.AllFiles.Count();

        var result = _host.Run("mods", "vortex");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Blue Hair", result.Output, StringComparison.Ordinal);
        Assert.Contains("Next: mods vortex copy", result.Output, StringComparison.Ordinal);
        Assert.Equal(before, _host.FileSystem.AllFiles.Count());
    }

    [Fact]
    public void Copy_ThenThePurge_ThenTurningOn_FollowsTheSteps_WithTheWinnerLast()
    {
        var game = GameFiles();

        var copy = _host.Run("mods", "vortex", "copy");

        Assert.Equal(0, copy.ExitCode);
        Assert.Contains("2 mod(s) copied", copy.Output, StringComparison.Ordinal);
        Assert.Equal(game, GameFiles());
        Assert.Contains("Purge", _host.Run("mods", "vortex").Output, StringComparison.Ordinal);

        _host.FileSystem.File.Delete(Path.Combine(_override, "hair.dds"));
        _host.FileSystem.File.Delete(Path.Combine(_override, "red.dds"));
        _host.FileSystem.File.Delete(Path.Combine(_override, "vortex.deployment.json"));

        var next = _host.Run("mods", "vortex").Output;
        Assert.Contains("Next, in this order: mods activate", next, StringComparison.Ordinal);
        Assert.True(next.IndexOf("red-hair", StringComparison.OrdinalIgnoreCase) < next.IndexOf("blue-hair", StringComparison.OrdinalIgnoreCase), next);
    }

    [Fact]
    public void Copy_AgainAfterItWorked_AddsNothingNew()
    {
        _host.Run("mods", "vortex", "copy");
        var library = _host.Run("mods", "library").Output;

        var again = _host.Run("mods", "vortex", "copy");

        Assert.Equal(0, again.ExitCode);
        Assert.Equal(library, _host.Run("mods", "library").Output);
    }

    [Fact]
    public void Copy_AModThatRunsInTheGame_IsLeftOut_AndDoesNotHoldUpTheHandover()
    {
        const string Dll = "DAO Lib-7052-1-2";
        _host.FileSystem.AddFile(Path.Combine(_staging, Dll, "dinput8.dll"), new MockFileData("dll"));
        _host.FileSystem.AddFile(Path.Combine(_override, "dinput8.dll"), new MockFileData("dll"));
        _host.FileSystem.File.WriteAllText(Path.Combine(_override, "vortex.deployment.json"), Record(("hair.dds", Winner), ("red.dds", Loser), ("dinput8.dll", Dll)));

        var copy = _host.Run("mods", "vortex", "copy");

        Assert.Contains("DAO Lib runs inside the game", copy.Output, StringComparison.Ordinal);
        Assert.Contains("2 mod(s) copied", copy.Output, StringComparison.Ordinal);
        Assert.Contains("Purge", _host.Run("mods", "vortex").Output, StringComparison.Ordinal);
    }

    [Fact]
    public void Copy_NeverWritesVortexsOwnFiles()
    {
        var staged = _host.FileSystem.AllFiles.Where(f => f.StartsWith(_staging, StringComparison.OrdinalIgnoreCase)).ToList();

        _host.Run("mods", "vortex", "copy");

        Assert.Equal(staged, _host.FileSystem.AllFiles.Where(f => f.StartsWith(_staging, StringComparison.OrdinalIgnoreCase)));
    }

    private static string Record(params (string RelPath, string Source)[] files) => JsonSerializer.Serialize(new
    {
        targetPath = _override,
        stagingPath = _staging,
        files = files.Select(f => new { relPath = f.RelPath, source = f.Source }),
    });

    private List<string> GameFiles() =>
        [.. _host.FileSystem.AllFiles.Where(f => f.StartsWith(_documents, StringComparison.OrdinalIgnoreCase)).Order()];
}
