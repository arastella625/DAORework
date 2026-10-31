using DAORework.Application.Abstractions;
using DAORework.Application.Mods.Adoption;
using DAORework.Application.Tests.Fakes;
using DAORework.Core.Catalog;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Mods.Adoption;
using DAORework.Core.Mods.Library;
using DAORework.Core.Mods.Planning;
using Xunit;

namespace DAORework.Application.Tests.Mods.Adoption;

public sealed class ScanForAdoptionTests
{
    private static readonly string _documents = TestPaths.Root("Users", "amber", "Documents", "BioWare", "Dragon Age");

    private static readonly HandInstalledSnapshot _snapshot = new(
        [new RegisteredAddIn("users_own", "User's Own"), new RegisteredAddIn("managed", "Managed")],
        new Dictionary<string, IReadOnlyList<HandInstalledFile>>(StringComparer.OrdinalIgnoreCase)
        {
            ["users_own"] = [new HandInstalledFile("AddIns/users_own/a.erf", 1)],
            ["managed"] = [new HandInstalledFile("AddIns/managed/b.erf", 1)],
        },
        new Dictionary<string, IReadOnlyList<HandInstalledFile>>(),
        []);

    private readonly InMemoryDeploymentStores _stores = new();

    [Fact]
    public async Task Execute_LeavesOutWhatTheRecordHolds()
    {
        await _stores.SaveAsync(_documents, new DeploymentRecord(["m"], [], [new RecordedAddIn("managed", "m")]));

        var scan = (await UseCase(new FakeKnownItemsCatalogSource([])).ExecuteAsync(_documents)).Value;

        Assert.Equal(["addin:users_own"], scan.Candidates.Select(c => c.Id));
    }

    [Fact]
    public async Task Execute_CandidateLikeALibraryMod_IsMatchedHashingOnlyTheFilesOfTheSameSize()
    {
        var mod = new LibraryMod("users-own-1", "User's Own", ModKind.AddIn, [new LibraryModFile("Contents/addins/users_own/a.erf", 1, "h")], "users_own");
        var library = new FakeLibrary([new LibraryEntry(mod, "users_own.dazip", "sha", DateTimeOffset.UnixEpoch)]);
        var hasher = new FakeHasher(new Dictionary<string, string> { ["AddIns/users_own/a.erf"] = "h" });

        var scan = (await new ScanForAdoption(new FakeReader(Result.Success(_snapshot)), _stores, new FakeKnownItemsCatalogSource([]), library, hasher).ExecuteAsync(_documents)).Value;

        var match = scan.Candidates.Single(c => c.Id == "addin:users_own").LibraryMatch!;
        Assert.Equal(("users-own-1", "User's Own", 0), (match.ModId, match.ModName, match.DifferingFiles));
        Assert.Null(scan.Candidates.Single(c => c.Id == "addin:managed").LibraryMatch);
        Assert.Equal(["AddIns/users_own/a.erf"], hasher.Asked);
    }

    [Fact]
    public async Task Execute_CatalogDoesNotLoad_FailsRatherThanRiskOfferingDlc()
    {
        var broken = new FakeKnownItemsCatalogSource(Result.Failure<KnownItemsCatalog>(new Error(DiagnosticCodes.CatalogInvalid, "broken")));

        var result = await UseCase(broken).ExecuteAsync(_documents);

        Assert.Equal(DiagnosticCodes.CatalogInvalid, result.Error!.Code);
    }

    [Fact]
    public async Task Execute_FolderUnreadable_PassesTheReadersError()
    {
        var failure = Result.Failure<HandInstalledSnapshot>(new Error(DiagnosticCodes.ModAdoptScanFailed, "locked"));

        var result = await new ScanForAdoption(new FakeReader(failure), _stores, new FakeKnownItemsCatalogSource([]), new FakeLibrary(), new FakeHasher()).ExecuteAsync(_documents);

        Assert.Equal(DiagnosticCodes.ModAdoptScanFailed, result.Error!.Code);
    }

    private ScanForAdoption UseCase(IKnownItemsCatalogSource catalog) => new(new FakeReader(Result.Success(_snapshot)), _stores, catalog, new FakeLibrary(), new FakeHasher());

    private sealed class FakeLibrary(IReadOnlyList<LibraryEntry>? entries = null) : IModLibrary
    {
        public Task<Result<LibraryImport>> ImportAsync(string sourcePath, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<IReadOnlyList<LibraryEntry>> ListAsync(CancellationToken cancellationToken = default) => Task.FromResult(entries ?? []);
    }

    private sealed class FakeHasher(IReadOnlyDictionary<string, string>? hashes = null) : IDocumentsScanner
    {
        public List<string> Asked { get; } = [];

        public Task<Result<DocumentsSnapshot>> ScanAsync(string documentsDirectory, IReadOnlyCollection<string> paths, CancellationToken cancellationToken = default)
        {
            Asked.AddRange(paths);
            return Task.FromResult(Result.Success(new DocumentsSnapshot(hashes ?? new Dictionary<string, string>(), [], [])));
        }
    }

    private sealed class FakeReader(Result<HandInstalledSnapshot> result) : IHandInstalledModsReader
    {
        public Task<Result<HandInstalledSnapshot>> ReadAsync(string documentsDirectory, CancellationToken cancellationToken = default) => Task.FromResult(result);
    }
}
