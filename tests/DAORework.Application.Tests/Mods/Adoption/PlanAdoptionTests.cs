using DAORework.Application.Abstractions;
using DAORework.Application.Mods.Adoption;
using DAORework.Application.Tests.Fakes;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Mods.Adoption;
using DAORework.Core.Mods.Library;
using DAORework.Core.Mods.Planning;
using Xunit;

namespace DAORework.Application.Tests.Mods.Adoption;

public sealed class PlanAdoptionTests
{
    private static readonly string _documents = TestPaths.Root("Users", "amber", "Documents", "BioWare", "Dragon Age");

    private static readonly LibraryMod _hair = new("better-hair-1", "Better Hair", ModKind.Override, [new LibraryModFile("hair.dds", 4, "h")]);

    private static readonly AdoptionCandidate _folder = new(
        "override:Better Hair",
        AdoptionCandidateKind.OverrideFolder,
        "Better Hair",
        "packages/core/override/Better Hair",
        [new HandInstalledFile("packages/core/override/Better Hair/hair.dds", 4)]);

    private readonly InMemoryDeploymentStores _stores = new();
    private readonly FakeImporter _importer = new();

    [Fact]
    public async Task Execute_CandidateImported_MovesItsFilesFromWhereTheyAre()
    {
        var plan = (await UseCase([]).ExecuteAsync(new PlanAdoptionRequest(_documents, [_folder]))).Value;

        var move = Assert.Single(plan.Operations);
        Assert.Equal((PlanAction.Move, "packages/core/override/Better Hair/hair.dds"), (move.Action, move.MovedFrom));
        Assert.Equal(["Better Hair"], _importer.Imported);
    }

    [Fact]
    public async Task Execute_CandidateExactlyALibraryMod_IsThatModAndNothingIsImported()
    {
        var matched = _folder with { LibraryMatch = new LibraryMatch("better-hair-1", "Better Hair", 0) { Sources = new Dictionary<string, string> { ["hair.dds"] = "packages/core/override/Better Hair/hair.dds" } } };

        var plan = (await UseCase([_hair]).ExecuteAsync(new PlanAdoptionRequest(_documents, [matched]))).Value;

        Assert.Equal("better-hair-1", Assert.Single(plan.Operations).ModId);
        Assert.Empty(_importer.Imported);
    }

    [Fact]
    public async Task Execute_LooseFilesWithoutAName_FailsBeforeImporting()
    {
        var loose = _folder with { Id = AdoptionIds.LooseOverrideFiles, Kind = AdoptionCandidateKind.LooseOverrideFiles };

        var result = await UseCase([]).ExecuteAsync(new PlanAdoptionRequest(_documents, [loose]));

        Assert.Equal(DiagnosticCodes.ModAdoptNameRequired, result.Error!.Code);
        Assert.Empty(_importer.Imported);
    }

    [Fact]
    public async Task Execute_AnUnfinishedChange_IsSettledFirst()
    {
        await _stores.StartAsync(new Core.Mods.Apply.ApplyJournal(_documents, "b", [], [], DeploymentRecord.Empty, DeploymentRecord.Empty));

        var result = await UseCase([]).ExecuteAsync(new PlanAdoptionRequest(_documents, [_folder]));

        Assert.Equal(DiagnosticCodes.ModApplyUnfinished, result.Error!.Code);
    }

    private PlanAdoption UseCase(IReadOnlyList<LibraryMod> library) => new(_importer, new FakeLibrary(library), _stores, new FakeScanner(), _stores);

    private sealed class FakeImporter : IAdoptionImporter
    {
        public List<string> Imported { get; } = [];

        public Task<Result<LibraryEntry>> ImportAsync(string documentsDirectory, AdoptionCandidate candidate, string name, CancellationToken cancellationToken = default)
        {
            Imported.Add(name);
            return Task.FromResult(Result.Success(new LibraryEntry(_hair, name, "sha", DateTimeOffset.UnixEpoch)));
        }
    }

    private sealed class FakeLibrary(IReadOnlyList<LibraryMod> mods) : IModLibrary
    {
        public Task<Result<LibraryImport>> ImportAsync(string sourcePath, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<IReadOnlyList<LibraryEntry>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<LibraryEntry>>(mods.Select(m => new LibraryEntry(m, m.Id, "sha", DateTimeOffset.UnixEpoch)).ToList());

        // No chargenmorphcfg.xml in these tests: combining never reads or stores anything.
        public Task<Result<string>> ReadTextAsync(string modId, string relativePath, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<Result<LibraryModFile>> StoreCombinedAsync(string fileName, string text, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class FakeScanner : IDocumentsScanner
    {
        public Task<Result<DocumentsSnapshot>> ScanAsync(string documentsDirectory, IReadOnlyCollection<string> paths, CancellationToken cancellationToken = default) =>
            Task.FromResult(Result.Success(new DocumentsSnapshot(new Dictionary<string, string> { ["packages/core/override/Better Hair/hair.dds"] = "h" }, ["Better Hair/hair.dds"], [])));
    }
}
