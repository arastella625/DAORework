using System.IO.Abstractions.TestingHelpers;
using DAORework.Cli.Composition;
using Xunit;

namespace DAORework.Cli.Tests;

/// <summary><c>mods inspect</c> lists the add-ins in the game's order with their Priority, and the files they share (P2.5-23).</summary>
public sealed class CliModInspectAddInsTests : IDisposable
{
    private static readonly string _documents = MockUnixSupport.Path(@"C:\Docs");

    private readonly CliTestHost _host = new();

    public CliModInspectAddInsTests()
    {
        _host.Run("init", "Smoke");
        _host.Run("profile", "set-path", "Smoke", "documents", _documents);
    }

    public void Dispose() => _host.Dispose();

    [Fact]
    public void Inspect_ListsTheAddInsInLoadOrder_AndWhoseCopyOfASharedFileTheGameUses()
    {
        AddList("""
            <AddInItem UID="dyes" ExtendedModuleUID="core" Priority="100" Enabled="1"><Title DefaultText="Universal Dye Kit" /></AddInItem>
            <AddInItem UID="DAO_PRC_CP_2" Priority="100" Enabled="1" BioWare="1"><Title DefaultText="The Stone Prisoner" /></AddInItem>
            <AddInItem UID="qwinn" ExtendedModuleUID="Single Player" Priority="109" Enabled="1"><Title DefaultText="Qwinn's Fixpack" /></AddInItem>
            """);
        Add("AddIns/dyes/core/override/dye.uti");
        Add("AddIns/qwinn/core/override/dye.uti");

        var result = _host.Run("mods", "inspect", "Smoke");

        Assert.Equal(ExitCodes.Success, result.ExitCode);
        var lines = result.Output.Split('\n').Select(l => l.TrimEnd()).ToList();
        var header = lines.FindIndex(l => l.Trim() == "Priority  Add-in");
        Assert.Equal(["       109  Qwinn's Fixpack", "       100  The Stone Prisoner (BioWare)", "       100  Universal Dye Kit"], lines.Skip(header + 1).Take(3));
        Assert.Contains("- dye.uti is in both Universal Dye Kit and Qwinn's Fixpack. The game uses Universal Dye Kit's copy, because its Priority is lower (100, against 109).", result.Output, StringComparison.Ordinal);
    }

    [Fact]
    public void Inspect_ABrokenAddInsList_IsSaidAndTheFoldersStillShow()
    {
        _host.FileSystem.AddFile(Path.Combine(_documents, "Settings", "AddIns.xml"), new MockFileData("<AddInsList>"));

        var result = _host.Run("mods", "inspect", "Smoke");

        Assert.Equal(ExitCodes.Success, result.ExitCode);
        Assert.Contains("AddIns Folder:", result.Output, StringComparison.Ordinal);
        Assert.Contains("Settings/AddIns.xml isn't valid XML", result.Output, StringComparison.Ordinal);
    }

    private void AddList(string items) =>
        _host.FileSystem.AddFile(Path.Combine(_documents, "Settings", "AddIns.xml"), new MockFileData($"<AddInsList>{items}</AddInsList>"));

    private void Add(string relative) =>
        _host.FileSystem.AddFile(Path.Combine(_documents, relative.Replace('/', Path.DirectorySeparatorChar)), new MockFileData("x"));
}
