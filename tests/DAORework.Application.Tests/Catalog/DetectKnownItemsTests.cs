using System.IO.Abstractions.TestingHelpers;
using DAORework.Application.Catalog;
using DAORework.Application.Catalog.Matchers;
using DAORework.Application.Tests.Fakes;
using DAORework.Core.Catalog;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Formats;
using Xunit;
using static DAORework.Application.Tests.Catalog.CatalogTestData;

namespace DAORework.Application.Tests.Catalog;

public sealed class DetectKnownItemsTests
{
    private readonly MockFileSystem _fileSystem = new();

    [Fact]
    public async Task ExecuteAsync_FindsItemsByEveryKindOfFingerprint()
    {
        _fileSystem.AddFile(D3D9, new MockFileData(DxvkDll("v2.3")));
        _fileSystem.AddFile(GameExecutable, new MockFileData(Pe(PortableExecutableMachine.X86, 0x0122)));
        _fileSystem.AddFile(GamePaths.AddInsListFile(DocumentsDirectory), new MockFileData(AddInsXml(("fixpack_uid", true))));
        _fileSystem.AddFile(Path.Combine(GamePaths.OverrideDirectory(DocumentsDirectory), "dialog_patch.dlg"), new MockFileData("x"));

        var scan = (await Detect([Dxvk, Fixpack, DialogPatch, LargeAddressAware], [FixpackVersusDialogPatch])).Value;

        Assert.Equal(["dxvk", "fixpack", "dialog-patch", "laa"], scan.Found.Select(f => f.Item.Id));
        Assert.Equal("v2.3", scan.Found[0].Version);
        Assert.Empty(scan.Missing);
        Assert.Equal([FixpackVersusDialogPatch], scan.Conflicts);
    }

    [Fact]
    public async Task ExecuteAsync_NothingInstalled_ListsEveryItemAsMissing()
    {
        var scan = (await Detect([Dxvk, Fixpack])).Value;

        Assert.Empty(scan.Found);
        Assert.Equal([Dxvk, Fixpack], scan.Missing);
    }

    [Fact]
    public async Task ExecuteAsync_DownloadHash_IsNeverLookedForOnDisk()
    {
        var withDownload = Fixpack with { Fingerprints = [.. Fixpack.Fingerprints, new KnownItemFingerprint(FingerprintKind.DownloadSha256, new string('a', 64), Version: "1.0")] };

        var scan = (await Detect([withDownload])).Value;

        Assert.Empty(scan.Unresolved);
        Assert.Equal([withDownload], scan.Missing);
    }

    [Fact]
    public async Task ExecuteAsync_UnrecognizedFile_IsUnresolvedNotMissing()
    {
        _fileSystem.AddFile(D3D9, new MockFileData(Pe(PortableExecutableMachine.X86, 0x2102)));

        var scan = (await Detect([Dxvk])).Value;

        Assert.Empty(scan.Missing);
        var error = Assert.Single(scan.Unresolved);
        Assert.Equal(DiagnosticCodes.KnownItemUnrecognized, error.Code);
        Assert.StartsWith("Could not tell whether DXVK is installed: ", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteAsync_OneFingerprintMatches_TheItemIsFoundEvenIfAnotherCouldNotBeChecked()
    {
        _fileSystem.AddFile(D3D9, new MockFileData(Pe(PortableExecutableMachine.X86, 0x2102)));
        _fileSystem.AddFile(GamePaths.AddInsListFile(DocumentsDirectory), new MockFileData(AddInsXml(("fixpack_uid", true))));
        var item = Fixpack with { Fingerprints = [Dxvk.Fingerprints[0], Fixpack.Fingerprints[0]] };

        var scan = (await Detect([item])).Value;

        Assert.Single(scan.Found);
        Assert.Empty(scan.Unresolved);
    }

    [Fact]
    public async Task ExecuteAsync_FingerprintKindWithoutAMatcher_IsUnresolvedAsACatalogProblem()
    {
        var useCase = new DetectKnownItems(new FakeKnownItemsCatalogSource([Dxvk]), [new AddInUidMatcher(_fileSystem)]);

        var scan = (await useCase.ExecuteAsync(new DetectKnownItemsRequest(Profile))).Value;

        Assert.Equal(DiagnosticCodes.CatalogInvalid, Assert.Single(scan.Unresolved).Code);
    }

    [Fact]
    public async Task ExecuteAsync_CatalogFailsToLoad_ReturnsTheFailure()
    {
        var broken = Result.Failure<KnownItemsCatalog>(new Error(DiagnosticCodes.CatalogInvalid, "broken"));
        var useCase = new DetectKnownItems(new FakeKnownItemsCatalogSource(broken), Matchers());

        var result = await useCase.ExecuteAsync(new DetectKnownItemsRequest(Profile));

        Assert.Equal(DiagnosticCodes.CatalogInvalid, result.Error!.Code);
    }

    internal static DetectKnownItems UseCase(MockFileSystem fileSystem, IReadOnlyList<KnownItem> items, IReadOnlyList<KnownConflict>? conflicts = null) =>
        new(new FakeKnownItemsCatalogSource(items, conflicts), Matchers(fileSystem));

    private static IFingerprintMatcher[] Matchers(MockFileSystem? fileSystem = null)
    {
        fileSystem ??= new MockFileSystem();
        return [new AddInUidMatcher(fileSystem), new OverrideFileMatcher(fileSystem), new GameFileMatcher(fileSystem), new LargeAddressAwareMatcher(fileSystem)];
    }

    private Task<Result<KnownItemsScan>> Detect(IReadOnlyList<KnownItem> items, IReadOnlyList<KnownConflict>? conflicts = null) =>
        UseCase(_fileSystem, items, conflicts).ExecuteAsync(new DetectKnownItemsRequest(Profile));
}
