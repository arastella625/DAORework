using System.IO.Abstractions.TestingHelpers;
using DAORework.Application.Catalog.Matchers;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Profiles;
using Xunit;
using static DAORework.Application.Tests.Catalog.CatalogTestData;

namespace DAORework.Application.Tests.Catalog;

/// <summary>The two matchers that look in the documents folder: add-in UIDs and override files.</summary>
public sealed class DocumentsMatcherTests
{
    private readonly MockFileSystem _fileSystem = new();

    [Fact]
    public async Task AddInUid_Registered_MatchesAndSaysWhetherItIsEnabled()
    {
        _fileSystem.AddFile(GamePaths.AddInsListFile(DocumentsDirectory), new MockFileData(AddInsXml(("other", true), ("FIXPACK_UID", false))));

        var match = Assert.Single((await AddIn(Profile)).Value);

        Assert.Equal("add-in fixpack_uid in AddIns.xml, disabled", match.Location);
    }

    [Fact]
    public async Task AddInUid_Enabled_IsNotMarkedDisabled()
    {
        _fileSystem.AddFile(GamePaths.AddInsListFile(DocumentsDirectory), new MockFileData(AddInsXml(("fixpack_uid", true))));

        Assert.Equal("add-in fixpack_uid in AddIns.xml", Assert.Single((await AddIn(Profile)).Value).Location);
    }

    [Fact]
    public async Task AddInUid_NotRegistered_MatchesNothing()
    {
        _fileSystem.AddFile(GamePaths.AddInsListFile(DocumentsDirectory), new MockFileData(AddInsXml(("other", true))));

        Assert.Empty((await AddIn(Profile)).Value);
    }

    [Fact]
    public async Task AddInUid_NoAddInsListOrNoDocumentsFolder_MatchesNothing()
    {
        Assert.Empty((await AddIn(Profile)).Value);
        Assert.Empty((await AddIn(new RuntimeProfile())).Value);
    }

    [Fact]
    public async Task AddInUid_MalformedList_IsUnreadable()
    {
        _fileSystem.AddFile(GamePaths.AddInsListFile(DocumentsDirectory), new MockFileData("<AddInsList><AddInItem"));

        Assert.Equal(DiagnosticCodes.KnownItemUnreadable, (await AddIn(Profile)).Error!.Code);
    }

    [Fact]
    public async Task AddInUid_LockedList_IsUnreadable()
    {
        _fileSystem.AddFile(GamePaths.AddInsListFile(DocumentsDirectory), new MockFileData(AddInsXml()) { AllowedFileShare = FileShare.None });

        Assert.Equal(DiagnosticCodes.KnownItemUnreadable, (await AddIn(Profile)).Error!.Code);
    }

    [Fact]
    public async Task OverrideFile_InAnySubfolder_MatchesEachCopyIgnoringCase()
    {
        var overrideDirectory = GamePaths.OverrideDirectory(DocumentsDirectory);
        _fileSystem.AddFile(Path.Combine(overrideDirectory, "My Mods", "Dialog_Patch.dlg"), new MockFileData("a"));
        _fileSystem.AddFile(Path.Combine(overrideDirectory, "dialog_patch.dlg"), new MockFileData("b"));
        _fileSystem.AddFile(Path.Combine(overrideDirectory, "unrelated.dlg"), new MockFileData("c"));

        var matches = (await Override(Profile)).Value;

        Assert.Equal(2, matches.Count);
        Assert.Contains(matches, m => m.Location == Path.Combine("packages", "core", "override", "My Mods", "Dialog_Patch.dlg"));
    }

    [Fact]
    public async Task OverrideFile_NoOverrideFolderOrNoDocumentsFolder_MatchesNothing()
    {
        Assert.Empty((await Override(Profile)).Value);
        Assert.Empty((await Override(new RuntimeProfile())).Value);
    }

    private Task<Result<IReadOnlyList<Core.Catalog.FingerprintMatch>>> AddIn(RuntimeProfile profile) =>
        new AddInUidMatcher(_fileSystem).MatchAsync(Fixpack.Fingerprints[0], profile);

    private Task<Result<IReadOnlyList<Core.Catalog.FingerprintMatch>>> Override(RuntimeProfile profile) =>
        new OverrideFileMatcher(_fileSystem).MatchAsync(DialogPatch.Fingerprints[0], profile);
}
