using DAORework.Application.Abstractions;
using DAORework.Application.Mods.Backups;
using DAORework.Application.Tests.Fakes;
using DAORework.Core.Diagnostics;
using DAORework.Core.Mods.Backups;
using DAORework.Core.Mods.Library;
using DAORework.Core.Mods.Planning;
using Xunit;

namespace DAORework.Application.Tests.Mods.Backups;

public sealed class DeploymentBackupUseCaseTests
{
    private const string Documents = "C:/Docs";
    private const string Kept = "packages/core/override/DAORework/a/kept.dds";
    private const string Gone = "packages/core/override/DAORework/old/gone.dds";

    private readonly FakeDeploymentBackupStore _store = new();

    [Fact]
    public async Task BackUp_CapturesEveryTouchedPathAndAddInsListWhenItChanges()
    {
        var disk = new Dictionary<string, string> { [Kept] = "k1", [Gone] = "g1" };
        var record = new DeploymentRecord(["a", "old"], [new RecordedFile(Kept, "k1", "a"), new RecordedFile(Gone, "g1", "old")], []);
        var mods = new[]
        {
            new LibraryMod("a", "A", ModKind.Override, [new LibraryModFile("kept.dds", 5, "k2"), new LibraryModFile("new.dds", 5, "n1")]),
            new LibraryMod("fix", "Fix", ModKind.AddIn, [new LibraryModFile("Contents/addins/fix_uid/x.ncs", 5, "x1")], "fix_uid"),
        };
        var plan = ModActivationPlanner.Plan(new PlanInput(record, mods, [], disk, [])).Value;

        var result = await new BackUpDeployment(_store).ExecuteAsync(new BackUpDeploymentRequest(Documents, plan));

        Assert.True(result.IsSuccess);
        Assert.Equal(
            [
                new BackupTarget("AddIns/fix_uid/x.ncs", null, "x1"),
                new BackupTarget("packages/core/override/DAORework/a/new.dds", null, "n1"),
                new BackupTarget(Kept, "k1", "k2"),
                new BackupTarget(Gone, "g1", null),
                new BackupTarget(BackUpDeployment.AddInsListPath, null, null, AnyContent: true),
            ],
            _store.LastTargets);
    }

    [Fact]
    public async Task BackUp_BlockedPlan_IsRefusedWithoutTouchingTheStore()
    {
        var disk = new Dictionary<string, string> { ["packages/core/override/DAORework/a/x.dds"] = "theirs" };
        var plan = ModActivationPlanner.Plan(new PlanInput(DeploymentRecord.Empty, [new LibraryMod("a", "A", ModKind.Override, [new LibraryModFile("x.dds", 1, "mine")])], [], disk, [])).Value;

        var result = await new BackUpDeployment(_store).ExecuteAsync(new BackUpDeploymentRequest(Documents, plan));

        Assert.Equal(DiagnosticCodes.ModPlanBlocked, result.Error!.Code);
        Assert.Null(_store.LastTargets);
    }

    [Fact]
    public async Task BackUp_EmptyPlan_HasNothingToBackUp()
    {
        var plan = ModActivationPlanner.Plan(new PlanInput(DeploymentRecord.Empty, [], [], new Dictionary<string, string>(), [])).Value;

        var result = await new BackUpDeployment(_store).ExecuteAsync(new BackUpDeploymentRequest(Documents, plan));

        Assert.Equal(DiagnosticCodes.ModPlanEmpty, result.Error!.Code);
    }

    [Fact]
    public async Task Restore_WithoutAnId_RestoresTheNewestBackupOfThatFolder()
    {
        _store.Backups.AddRange([Backup("new", Documents), Backup("old", Documents), Backup("other", "D:/Else")]);

        var report = await new RestoreDeployment(_store).ExecuteAsync(new RestoreDeploymentRequest(Documents));

        Assert.True(report.IsSuccess);
        Assert.Equal("new", _store.Restored!.Id);
    }

    [Fact]
    public async Task Restore_ById_RestoresThatOne()
    {
        _store.Backups.AddRange([Backup("new", Documents), Backup("old", Documents)]);

        await new RestoreDeployment(_store).ExecuteAsync(new RestoreDeploymentRequest(Documents, "OLD"));

        Assert.Equal("old", _store.Restored!.Id);
    }

    [Theory]
    [InlineData(null, "There is no backup of changes")]
    [InlineData("missing", "There is no backup missing of changes")]
    public async Task Restore_NothingToRestore_IsNotFound(string? id, string message)
    {
        var result = await new RestoreDeployment(_store).ExecuteAsync(new RestoreDeploymentRequest(Documents, id));

        Assert.Equal(DiagnosticCodes.ModBackupNotFound, result.Error!.Code);
        Assert.StartsWith(message, result.Error.Message, StringComparison.Ordinal);
        Assert.Null(_store.Restored);
    }

    private static DeploymentBackup Backup(string id, string documents) => new(id, DateTimeOffset.UnixEpoch, documents, []);
}
