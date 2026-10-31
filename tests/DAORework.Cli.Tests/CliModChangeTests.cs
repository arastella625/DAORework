using System.IO.Abstractions.TestingHelpers;
using System.IO.Compression;
using DAORework.Cli.Composition;
using Xunit;

namespace DAORework.Cli.Tests;

/// <summary>mods activate, deactivate, status and rollback end to end, over a documents folder in memory.</summary>
public sealed class CliModChangeTests : IDisposable
{
    private static readonly string _documents = MockUnixSupport.Path(@"C:\Users\Me\Documents\BioWare\Dragon Age");
    private static readonly string _download = MockUnixSupport.Path(@"C:\Downloads\dog.dazip");
    private static readonly string _deployed = Path.Combine(_documents, "AddIns", "Extra_Dog_Slot", "dog.gda");

    private readonly CliTestHost _host = new();
    private readonly string _modId;

    public CliModChangeTests()
    {
        _host.FileSystem.AddFile(Path.Combine(_documents, "Settings", "AddIns.xml"), new MockFileData("<AddInsList>\r\n  <AddInItem UID=\"users_own\" />\r\n</AddInsList>"));
        _host.FileSystem.AddFile(_download, new MockFileData(Dazip()));
        _host.Run("init", "Mods");
        _host.Run("profile", "set-path", "Mods", "documents", _documents);
        var imported = _host.Run("mods", "import", _download).Output;
        _modId = imported[(imported.IndexOf(" as ", StringComparison.Ordinal) + 4)..imported.IndexOf(',', StringComparison.Ordinal)];
    }

    public void Dispose() => _host.Dispose();

    [Fact]
    public void Activate_DryRun_ShowsThePlanAndChangesNothing()
    {
        var result = _host.Run("mods", "activate", _modId, "-p", "Mods", "--dry-run", "--files");

        Assert.Equal(ExitCodes.Success, result.ExitCode);
        Assert.Contains("AddIns.xml: add Extra_Dog_Slot", result.Output, StringComparison.Ordinal);
        Assert.Contains("+ AddIns/Extra_Dog_Slot/dog.gda", result.Output, StringComparison.Ordinal);
        Assert.Contains("Dry run: nothing was changed.", result.Output, StringComparison.Ordinal);
        Assert.False(_host.FileSystem.File.Exists(_deployed));
    }

    [Fact]
    public void Activate_WithoutYesWhenTheConsoleCannotAsk_ChangesNothing()
    {
        var result = _host.Run("mods", "activate", _modId, "-p", "Mods");

        Assert.Equal(ExitCodes.Failure, result.ExitCode);
        Assert.Contains("add --yes", result.Output, StringComparison.Ordinal);
        Assert.False(_host.FileSystem.File.Exists(_deployed));
    }

    [Fact]
    public void ActivateThenDeactivate_InstallsAndThenLeavesTheFolderAsItWas()
    {
        var addInsBefore = ReadAddIns();

        var activated = _host.Run("mods", "activate", _modId, "-p", "Mods", "--yes");
        var status = _host.Run("mods", "status", "-p", "Mods");

        Assert.Equal(ExitCodes.Success, activated.ExitCode);
        Assert.Contains("Undo with: mods rollback", activated.Output, StringComparison.Ordinal);
        Assert.Equal("dog", _host.FileSystem.File.ReadAllText(_deployed));
        Assert.Contains("Extra_Dog_Slot", ReadAddIns(), StringComparison.Ordinal);
        Assert.Contains($"Active, lowest priority first: {_modId}", status.Output, StringComparison.Ordinal);

        var deactivated = _host.Run("mods", "deactivate", _modId, "-p", "Mods", "--yes");

        Assert.Equal(ExitCodes.Success, deactivated.ExitCode);
        Assert.False(_host.FileSystem.File.Exists(_deployed));
        Assert.Equal(addInsBefore, ReadAddIns());
        Assert.Contains("No mods are active", _host.Run("mods", "status", "-p", "Mods").Output, StringComparison.Ordinal);
    }

    [Fact]
    public void Rollback_UndoesTheLastActivation()
    {
        _host.Run("mods", "activate", _modId, "-p", "Mods", "--yes");

        var result = _host.Run("mods", "rollback", "-p", "Mods", "--yes");

        Assert.Equal(ExitCodes.Success, result.ExitCode);
        Assert.Contains("Rolled back:", result.Output, StringComparison.Ordinal);
        Assert.False(_host.FileSystem.File.Exists(_deployed));
        Assert.DoesNotContain("Extra_Dog_Slot", ReadAddIns(), StringComparison.Ordinal);
    }

    [Fact]
    public void Activate_AFileTheUserPutThere_IsBlockedAndLeftAlone()
    {
        _host.FileSystem.AddFile(_deployed, new MockFileData("mine"));

        var result = _host.Run("mods", "activate", _modId, "-p", "Mods", "--yes");

        Assert.Equal(ExitCodes.Failure, result.ExitCode);
        Assert.Contains("Blocked; nothing will be changed:", result.Output, StringComparison.Ordinal);
        Assert.Equal("mine", _host.FileSystem.File.ReadAllText(_deployed));
    }

    [Theory]
    [InlineData("mods", "activate", "nope-1234", "-p", "Mods", "--yes")]
    [InlineData("mods", "recover", "-p", "Mods", "--finish")]
    [InlineData("mods", "rollback", "-p", "Mods", "--yes")]
    [InlineData("mods", "status", "-p", "NoSuchProfile")]
    public void Commands_WithNothingToActOn_Fail(params string[] args)
    {
        Assert.Equal(ExitCodes.Failure, _host.Run(args).ExitCode);
    }

    [Fact]
    public void Recover_WithBothOrNeitherChoice_IsInvalidUsage()
    {
        Assert.Equal(ExitCodes.InvalidUsage, _host.Run("mods", "recover", "-p", "Mods").ExitCode);
    }

    private static byte[] Dazip()
    {
        using var buffer = new MemoryStream();
        using (var zip = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            Write(zip, "Manifest.xml", "<Manifest><AddInsList><AddInItem UID=\"Extra_Dog_Slot\" Name=\"Extra Dog Slot\" /></AddInsList></Manifest>");
            Write(zip, "Contents/addins/Extra_Dog_Slot/dog.gda", "dog");
        }

        return buffer.ToArray();
    }

    private static void Write(ZipArchive zip, string name, string content)
    {
        using var writer = new StreamWriter(zip.CreateEntry(name).Open());
        writer.Write(content);
    }

    private string ReadAddIns() => _host.FileSystem.File.ReadAllText(Path.Combine(_documents, "Settings", "AddIns.xml"));
}
