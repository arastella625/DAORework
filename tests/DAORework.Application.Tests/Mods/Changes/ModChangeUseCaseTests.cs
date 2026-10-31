using DAORework.Application.Abstractions;
using DAORework.Application.Mods.Apply;
using DAORework.Application.Mods.Backups;
using DAORework.Application.Mods.Changes;
using DAORework.Application.Tests.Fakes;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Mods.Library;
using DAORework.Core.Mods.Planning;
using Xunit;

namespace DAORework.Application.Tests.Mods.Changes;

public sealed class ModChangeUseCaseTests
{
    private const string Documents = "C:/Docs";
    private const string HairPath = "packages/core/override/DAORework/hair/hair.dds";

    private readonly InMemoryDeploymentStores _stores = new();
    private readonly FakeDeploymentBackupStore _backups = new();
    private readonly FakeLibrary _library = new();
    private readonly FakeScanner _scanner = new();

    [Fact]
    public async Task Plan_Activate_PlansTheLibraryModAgainstWhatIsOnDisk()
    {
        var plan = (await Plan(ModChangeKind.Activate, "hair")).Value;

        Assert.Equal(["hair"], plan.TargetModIds);
        Assert.Equal([HairPath], _scanner.AskedFor);
        Assert.Equal(PlanAction.Copy, Assert.Single(plan.Operations).Action);
    }

    [Fact]
    public async Task Plan_UsesTheScannedStateToBlockAFileTheUserPutThere()
    {
        _scanner.Hashes[HairPath] = "someone-elses";

        var plan = (await Plan(ModChangeKind.Activate, "hair")).Value;

        Assert.Equal(PlanBlockerKind.UnmanagedFileInTheWay, Assert.Single(plan.Blockers).Kind);
    }

    [Fact]
    public async Task Plan_ModNotInTheLibrary_FailsNamingIt()
    {
        var result = await Plan(ModChangeKind.Activate, "missing");

        Assert.Equal(DiagnosticCodes.ModNotInLibrary, result.Error!.Code);
        Assert.Contains("missing", result.Error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Plan_DeactivatingAModThatIsNotActive_Fails()
    {
        Assert.Equal(DiagnosticCodes.ModNotActive, (await Plan(ModChangeKind.Deactivate, "hair")).Error!.Code);
    }

    [Fact]
    public async Task Plan_WhileAChangeIsUnfinished_Fails()
    {
        await _stores.StartAsync(new(Documents, "b0", [], [], DeploymentRecord.Empty, DeploymentRecord.Empty));

        Assert.Equal(DiagnosticCodes.ModApplyUnfinished, (await Plan(ModChangeKind.Activate, "hair")).Error!.Code);
    }

    [Fact]
    public async Task Plan_ScanFails_PassesTheErrorOn()
    {
        _scanner.Fail = true;

        Assert.Equal(DiagnosticCodes.ModScanFailed, (await Plan(ModChangeKind.Activate, "hair")).Error!.Code);
    }

    [Fact]
    public async Task RollBack_RestoresTheNewestChangeAndPutsItsPreviousRecordBack()
    {
        var plan = (await Plan(ModChangeKind.Activate, "hair")).Value;
        await new ApplyModPlan(new BackUpDeployment(_backups), _stores, new JournaledApplyRunner(_stores, _stores, _stores)).ExecuteAsync(new(Documents, plan));
        Assert.Equal(["hair"], _stores.Saved!.ActiveModIds);

        var report = await RollBack().ExecuteAsync(new RollBackModChangeRequest(Documents));

        Assert.True(report.IsSuccess);
        Assert.Equal("b0", _backups.Restored!.Id);
        Assert.Empty(_stores.Saved!.ActiveModIds);
    }

    [Fact]
    public async Task RollBack_IgnoresTheBackupOfAChangeThatDiedBeforeItsJournal()
    {
        var plan = (await Plan(ModChangeKind.Activate, "hair")).Value;
        await new ApplyModPlan(new BackUpDeployment(_backups), _stores, new JournaledApplyRunner(_stores, _stores, _stores)).ExecuteAsync(new(Documents, plan));
        await _backups.SaveAsync(Documents, [], _stores.Saved);

        var report = await RollBack().ExecuteAsync(new RollBackModChangeRequest(Documents));

        Assert.True(report.IsSuccess);
        Assert.Equal(("b1", "b0"), (_backups.Backups[0].Id, _backups.Restored!.Id));
        Assert.Empty(_stores.Saved!.ActiveModIds);
    }

    [Fact]
    public async Task RollBack_BackAtTheStart_HasNothingMoreToUndo()
    {
        var plan = (await Plan(ModChangeKind.Activate, "hair")).Value;
        await new ApplyModPlan(new BackUpDeployment(_backups), _stores, new JournaledApplyRunner(_stores, _stores, _stores)).ExecuteAsync(new(Documents, plan));
        await RollBack().ExecuteAsync(new RollBackModChangeRequest(Documents));

        Assert.Equal(DiagnosticCodes.ModBackupNotFound, (await RollBack().ExecuteAsync(new RollBackModChangeRequest(Documents))).Error!.Code);
    }

    [Fact]
    public async Task RollBack_RecordWrittenBeforeRecordsNamedTheirBackup_UsesTheNewest()
    {
        await _stores.SaveAsync(Documents, new DeploymentRecord(["hair"], [new RecordedFile(HairPath, "h", "hair")], []));
        _backups.Backups.Add(new("old", DateTimeOffset.UnixEpoch, Documents, []) { PreviousRecord = DeploymentRecord.Empty });

        Assert.True((await RollBack().ExecuteAsync(new RollBackModChangeRequest(Documents))).IsSuccess);
        Assert.Equal("old", _backups.Restored!.Id);
    }

    [Fact]
    public async Task RollBack_NothingToRollBack_Fails()
    {
        Assert.Equal(DiagnosticCodes.ModBackupNotFound, (await RollBack().ExecuteAsync(new RollBackModChangeRequest(Documents))).Error!.Code);
    }

    [Fact]
    public async Task RollBack_WhileAChangeIsUnfinished_PointsAtRecover()
    {
        await _stores.StartAsync(new(Documents, "b0", [], [], DeploymentRecord.Empty, DeploymentRecord.Empty));

        var result = await RollBack().ExecuteAsync(new RollBackModChangeRequest(Documents));

        Assert.Equal(DiagnosticCodes.ModApplyUnfinished, result.Error!.Code);
        Assert.Contains("recover --finish", result.Error.Recommendation, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Status_ReportsTheRecordTheUnfinishedChangeAndTheBackups()
    {
        await _stores.SaveAsync(Documents, new DeploymentRecord(["hair"], [], []));
        await _stores.StartAsync(new(Documents, "b0", [], [], DeploymentRecord.Empty, DeploymentRecord.Empty));
        _backups.Backups.Add(new("b0", DateTimeOffset.UnixEpoch, Documents, []));

        var status = (await new GetModStatus(_stores, _stores, _backups).ExecuteAsync(Documents)).Value;

        Assert.Equal(["hair"], status.Record.ActiveModIds);
        Assert.True(status.ChangeUnfinished);
        Assert.Single(status.Backups);
    }

    private Task<Result<ModActivationPlan>> Plan(ModChangeKind kind, params string[] ids) =>
        new PlanModChange(_stores, _library, _scanner, _stores).ExecuteAsync(new PlanModChangeRequest(Documents, new ModChange(kind, ids)));

    private RollBackModChange RollBack() => new(_stores, _backups, new RestoreDeployment(_backups), _stores);

    private sealed class FakeLibrary : IModLibrary
    {
        public Task<Result<LibraryImport>> ImportAsync(string sourcePath, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<IReadOnlyList<LibraryEntry>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<LibraryEntry>>([new LibraryEntry(new LibraryMod("hair", "Hair", ModKind.Override, [new LibraryModFile("hair.dds", 4, "h1")]), "hair.zip", "abc", DateTimeOffset.UnixEpoch)]);
    }

    private sealed class FakeScanner : IDocumentsScanner
    {
        public Dictionary<string, string> Hashes { get; } = new(StringComparer.OrdinalIgnoreCase);

        public List<string> AskedFor { get; } = [];

        public bool Fail { get; set; }

        public Task<Result<DocumentsSnapshot>> ScanAsync(string documentsDirectory, IReadOnlyCollection<string> paths, CancellationToken cancellationToken = default)
        {
            AskedFor.AddRange(paths);
            return Task.FromResult(Fail
                ? Result.Failure<DocumentsSnapshot>(new Error(DiagnosticCodes.ModScanFailed, "locked"))
                : Result.Success(new DocumentsSnapshot(Hashes, [], [])));
        }
    }
}
