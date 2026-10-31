using System.IO.Abstractions.TestingHelpers;
using DAORework.Application.Mods;
using DAORework.Application.Tests.Fakes;
using DAORework.Core.Common;
using DAORework.Core.Profiles;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace DAORework.Application.Tests.Mods;

public sealed class InspectModFoldersTests
{
    private static readonly DateTimeOffset _now = new(2026, 9, 29, 12, 0, 0, TimeSpan.Zero);

    private readonly MockFileSystem _fileSystem = new();
    private readonly FakeKnownFolders _knownFolders = new();

    private InspectModFolders CreateUseCase() => new(_fileSystem, _knownFolders, new FakeTimeProvider(_now));

    [Fact]
    public async Task ExecuteAsync_NoDocumentsDirectoryOnProfile_UsesDefaultUnderMyDocuments()
    {
        var result = await CreateUseCase().ExecuteAsync(new InspectModFoldersRequest(new RuntimeProfile { Name = "P" }));

        Assert.True(result.IsSuccess);
        var report = result.Value;
        Assert.Equal(GamePaths.DefaultDocumentsDirectory(_knownFolders.MyDocuments), report.UserDocumentsDirectory);
        Assert.False(report.OverrideFolder.Exists);
        Assert.False(report.AddInsFolder.Exists);
        Assert.Equal(_now, report.CreatedAtUtc);
    }

    [Fact]
    public async Task ExecuteAsync_OverrideFolderWithFiles_CountsFilesFoldersAndBytes()
    {
        var documents = TestPaths.Root("Docs");
        var overrideDir = GamePaths.OverrideDirectory(documents);
        _fileSystem.AddFile(Path.Combine(overrideDir, "a.erf"), new MockFileData(new byte[100]));
        _fileSystem.AddFile(Path.Combine(overrideDir, "sub", "b.gda"), new MockFileData(new byte[50]));

        var report = (await CreateUseCase().ExecuteAsync(new InspectModFoldersRequest(new RuntimeProfile { Name = "P", UserDocumentsDirectory = documents }))).Value;

        Assert.True(report.OverrideFolder.Exists);
        Assert.Equal(2, report.OverrideFolder.FileCount);
        Assert.Equal(1, report.OverrideFolder.FolderCount);
        Assert.Equal(150, report.OverrideFolder.SizeBytes);
        Assert.Equal(0, report.OverrideFolder.UnreadableFileCount);
        Assert.Equal("150 B", report.OverrideFolder.SizeDisplay);
        Assert.Equal("AddIns Folder", report.AddInsFolder.Name);
    }
}
