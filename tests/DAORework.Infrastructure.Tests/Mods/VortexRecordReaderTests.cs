using System.IO.Abstractions.TestingHelpers;
using System.Text.Json;
using DAORework.Core.Diagnostics;
using DAORework.Infrastructure.Mods;
using Xunit;

namespace DAORework.Infrastructure.Tests.Mods;

/// <summary>Reading one of Vortex's deployment records (ADR-0028 §1). Nothing of Vortex's is ever written.</summary>
public sealed class VortexRecordReaderTests
{
    private static readonly string _documents = MockUnixSupport.Path(@"C:\Games\BioWare\Dragon Age");
    private static readonly string _staging = MockUnixSupport.Path(@"C:\Vortex\dragonage\mods");

    private readonly MockFileSystem _fileSystem = new();

    private string Override => _fileSystem.Path.Combine(_documents, "packages", "core", "override");

    private string RecordPath => _fileSystem.Path.Combine(Override, "vortex.deployment.json");

    [Fact]
    public async Task Read_ARecordAtItsTarget_ListsItsFilesBelowTheDocumentsFolder_AndTheModsStagingHolds()
    {
        _fileSystem.AddDirectory(_fileSystem.Path.Combine(_staging, "Hair-1-0"));
        Write(Override.Replace('/', '\\'), _staging, ("next part\\hair.dds", "Hair-1-0"), ("eyes.dds", "Eyes-2-0"));
        var before = _fileSystem.AllFiles.Count();

        var record = (await ReadAsync()).Value;

        Assert.Equal(
            [("packages/core/override/next part/hair.dds", "Hair-1-0"), ("packages/core/override/eyes.dds", "Eyes-2-0")],
            record.Files.Select(f => (f.Path, f.Mod)));
        Assert.True(record.AtItsTarget);
        Assert.True(record.IsLive("Hair-1-0"));
        Assert.False(record.IsLive("Eyes-2-0"));
        Assert.Equal(before, _fileSystem.AllFiles.Count());
    }

    [Fact]
    public async Task Read_ARecordWhoseFolderHasMoved_IsNotAtItsTarget()
    {
        _fileSystem.AddDirectory(_fileSystem.Path.Combine(_staging, "Hair-1-0"));
        Write(@"C:\Users\Me\Documents\BioWare\Dragon Age\packages\core\override", _staging, ("hair.dds", "Hair-1-0"));

        var record = (await ReadAsync()).Value;

        Assert.False(record.AtItsTarget);
        Assert.False(record.IsLive("Hair-1-0"));
    }

    [Fact]
    public async Task Read_PathsOutsideTheRecordsFolder_AreSkipped()
    {
        Write(Override, string.Empty, ("..\\..\\Settings\\AddIns.xml", "Bad"), ("C:\\Windows\\x.dll", "Bad"), ("/etc/x", "Bad"), ("ok.dds", "Good"), ("missing-source.dds", string.Empty));

        var record = (await ReadAsync()).Value;

        Assert.Equal(["packages/core/override/ok.dds"], record.Files.Select(f => f.Path));
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("{\"files\": 3}")]
    [InlineData("[]")]
    public async Task Read_ARecordThatCannotBeRead_SaysSo(string content)
    {
        _fileSystem.AddFile(RecordPath, new MockFileData(content));

        var result = await ReadAsync();

        Assert.Equal(DiagnosticCodes.VortexRecordUnreadable, result.Error!.Code);
    }

    private Task<Core.Common.Result<Core.Mods.Adoption.VortexRecord>> ReadAsync() =>
        new VortexRecordReader(_fileSystem).ReadAsync(_documents, RecordPath);

    private void Write(string targetPath, string stagingPath, params (string RelPath, string Source)[] files) =>
        _fileSystem.AddFile(RecordPath, new MockFileData(JsonSerializer.Serialize(new
        {
            version = 1,
            deploymentMethod = "hardlink_activator",
            targetPath,
            stagingPath,
            files = files.Select(f => new { relPath = f.RelPath, source = f.Source, time = 1 }),
        })));
}
