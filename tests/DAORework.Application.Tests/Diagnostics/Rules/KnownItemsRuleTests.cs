using System.IO.Abstractions.TestingHelpers;
using DAORework.Application.Catalog;
using DAORework.Application.Catalog.Matchers;
using DAORework.Application.Diagnostics.Rules;
using DAORework.Application.Tests.Catalog;
using DAORework.Application.Tests.Fakes;
using DAORework.Core.Catalog;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Formats;
using Xunit;
using static DAORework.Application.Tests.Catalog.CatalogTestData;

namespace DAORework.Application.Tests.Diagnostics.Rules;

public sealed class KnownItemsRuleTests
{
    private readonly MockFileSystem _fileSystem = new();

    [Fact]
    public async Task EvaluateAsync_NothingFound_SaysSoAndSuggestsOnlyItemsWithAdvice()
    {
        var issues = await Evaluate([Dxvk, Fixpack]);

        Assert.Equal([DiagnosticCodes.KnownItemsNone, DiagnosticCodes.KnownItemSuggested], issues.Select(i => i.Code));
        var suggestion = issues[1];
        Assert.Equal(DiagnosticSeverity.Info, suggestion.Severity);
        Assert.Equal("DXVK is not installed.", suggestion.Message);
        Assert.Equal("DXVK avoids the memory-related crashes. Source: https://github.com/doitsujin/dxvk", suggestion.Recommendation);
    }

    [Fact]
    public async Task EvaluateAsync_Found_ListsNamesVersionsAndWhere()
    {
        _fileSystem.AddFile(D3D9, new MockFileData(DxvkDll("v2.3")));
        _fileSystem.AddFile(GamePaths.AddInsListFile(DocumentsDirectory), new MockFileData(AddInsXml(("fixpack_uid", true))));

        var issue = Assert.Single(await Evaluate([Dxvk, Fixpack]));

        Assert.Equal(DiagnosticCodes.KnownItemsFound, issue.Code);
        Assert.Equal(DiagnosticSeverity.Info, issue.Severity);
        Assert.Equal(
            $"Known fixes and mods installed: DXVK v2.3 ({Path.Combine("bin_ship", "d3d9.dll")}); Fixpack (add-in fixpack_uid in AddIns.xml).",
            issue.Message);
    }

    [Fact]
    public async Task EvaluateAsync_OfficialDlcFound_LeavesItOutOfTheSummary()
    {
        var dlc = Mod("dlc-witch-hunt", "Witch Hunt", FingerprintKind.AddInUid, "DAO_PRC_STR") with { Kind = KnownItemKind.Official };
        _fileSystem.AddFile(GamePaths.AddInsListFile(DocumentsDirectory), new MockFileData(AddInsXml(("DAO_PRC_STR", true), ("fixpack_uid", true))));

        var issue = Assert.Single(await Evaluate([dlc, Fixpack]));

        Assert.Equal(DiagnosticCodes.KnownItemsFound, issue.Code);
        Assert.Equal("Known fixes and mods installed: Fixpack (add-in fixpack_uid in AddIns.xml).", issue.Message);
    }

    [Fact]
    public async Task EvaluateAsync_OnlyOfficialDlcFound_SaysNoFixesOrModsWereFound()
    {
        var dlc = Mod("dlc-witch-hunt", "Witch Hunt", FingerprintKind.AddInUid, "DAO_PRC_STR") with { Kind = KnownItemKind.Official };
        _fileSystem.AddFile(GamePaths.AddInsListFile(DocumentsDirectory), new MockFileData(AddInsXml(("DAO_PRC_STR", true))));

        var issue = Assert.Single(await Evaluate([dlc]));

        Assert.Equal(DiagnosticCodes.KnownItemsNone, issue.Code);
    }

    [Fact]
    public async Task EvaluateAsync_BothSidesOfAConflict_WarnsWithTheSource()
    {
        _fileSystem.AddFile(GamePaths.AddInsListFile(DocumentsDirectory), new MockFileData(AddInsXml(("fixpack_uid", true))));
        _fileSystem.AddFile(Path.Combine(GamePaths.OverrideDirectory(DocumentsDirectory), "dialog_patch.dlg"), new MockFileData("x"));

        var issues = await Evaluate([Fixpack, DialogPatch], [FixpackVersusDialogPatch]);

        var conflict = Assert.Single(issues, i => i.Code == DiagnosticCodes.KnownItemConflict);
        Assert.Equal(DiagnosticSeverity.Warning, conflict.Severity);
        Assert.Equal("Fixpack and Dialog Patch are both installed. The patch undoes the fixpack's dialogue fixes.", conflict.Message);
        Assert.Equal("Source: https://example.org/fixpack#compatibility", conflict.Recommendation);
    }

    [Fact]
    public async Task EvaluateAsync_64BitDxvk_WarnsAboutTheArchitecture()
    {
        _fileSystem.AddFile(D3D9, new MockFileData(DxvkDll("v3.1.1", PortableExecutableMachine.X64)));

        var issues = await Evaluate([Dxvk]);

        Assert.Equal([DiagnosticCodes.KnownItemsFound, DiagnosticCodes.KnownItemWrongArchitecture], issues.Select(i => i.Code));
        Assert.Equal(DiagnosticSeverity.Warning, issues[1].Severity);
        Assert.Contains("x32", issues[1].Recommendation, StringComparison.Ordinal);
    }

    [Fact]
    public async Task EvaluateAsync_UnrecognizedD3D9_IsInfoAndDoesNotSuggestInstallingDxvk()
    {
        _fileSystem.AddFile(D3D9, new MockFileData(Pe(PortableExecutableMachine.X86, 0x2102)));

        var issues = await Evaluate([Dxvk]);

        Assert.Equal([DiagnosticCodes.KnownItemsNone, DiagnosticCodes.KnownItemUnrecognized], issues.Select(i => i.Code));
        Assert.Equal(DiagnosticSeverity.Info, issues[1].Severity);
    }

    [Fact]
    public async Task EvaluateAsync_UnreadableFile_IsAWarning()
    {
        _fileSystem.AddFile(D3D9, new MockFileData(DxvkDll("v2.3")) { AllowedFileShare = FileShare.None });

        var issues = await Evaluate([Dxvk]);

        var unreadable = Assert.Single(issues, i => i.Code == DiagnosticCodes.KnownItemUnreadable);
        Assert.Equal(DiagnosticSeverity.Warning, unreadable.Severity);
    }

    [Fact]
    public async Task EvaluateAsync_CatalogFailsToLoad_WarnsWithoutBlockingTheLaunch()
    {
        var broken = Result.Failure<KnownItemsCatalog>(new Error(DiagnosticCodes.CatalogInvalid, "broken"));
        var rule = new KnownItemsRule(new DetectKnownItems(new FakeKnownItemsCatalogSource(broken), [new GameFileMatcher(_fileSystem)]));

        var issue = Assert.Single(await rule.EvaluateAsync(Profile));

        Assert.Equal(DiagnosticCodes.CatalogInvalid, issue.Code);
        Assert.Equal(DiagnosticSeverity.Warning, issue.Severity);
    }

    private Task<IReadOnlyList<DiagnosticIssue>> Evaluate(IReadOnlyList<KnownItem> items, IReadOnlyList<KnownConflict>? conflicts = null) =>
        new KnownItemsRule(DetectKnownItemsTests.UseCase(_fileSystem, items, conflicts)).EvaluateAsync(Profile);
}
