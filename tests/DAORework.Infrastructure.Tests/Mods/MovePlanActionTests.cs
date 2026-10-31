using DAORework.Application.Abstractions;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Mods.Apply;
using DAORework.Core.Mods.Conflicts;
using DAORework.Core.Mods.Planning;
using Xunit;
using static DAORework.Infrastructure.Tests.Mods.ModScenario;

namespace DAORework.Infrastructure.Tests.Mods;

/// <summary>
/// <see cref="PlanAction.Move"/> (ADR-0015) through the real writer, backup store and journal: the user's own override
/// folder moved into DAORework's, backed up as moves rather than copies, and recovered both ways from a crash.
/// </summary>
public sealed class MovePlanActionTests
{
    private const string Mine = "packages/core/override/My Mods/mine.dds";
    private const string Two = "packages/core/override/My Mods/sub/two.dds";
    private const string MineTarget = "packages/core/override/DAORework/mine/mine.dds";
    private const string TwoTarget = "packages/core/override/DAORework/mine/sub/two.dds";

    private static readonly string _downloads = Path.Combine(Path.GetDirectoryName(AppData)!, "Downloads");

    private readonly ModScenario _scenario = new();

    public MovePlanActionTests()
    {
        _scenario.Write(Documents, Two, "two");
    }

    [Fact]
    public async Task Apply_MovesEachFileAndBacksUpOnlyTheMoves()
    {
        var report = await _scenario.Apply(_scenario.Writer).ExecuteAsync(new(Documents, await PlanAsync()));

        Assert.True(report.IsSuccess, report.Error?.Message);
        Assert.Equal("the user's own", Read(MineTarget));
        Assert.Equal("two", Read(TwoTarget));
        Assert.False(_scenario.FileSystem.Directory.Exists(_scenario.Full(Documents, "packages/core/override/My Mods")));
        var backup = Assert.Single(await _scenario.Backups.ListAsync(Documents));
        Assert.Equal([Mine, Two], backup.Entries.Select(e => e.MovedFrom));
        Assert.All(backup.Entries, e => Assert.Null(e.StoredAs));
        Assert.Empty(_scenario.FileSystem.Directory.EnumerateFiles(Path.Combine(AppData, "deployment-backups"), "0000*", SearchOption.AllDirectories));
    }

    [Fact]
    public async Task Restore_AfterMoves_PutsEveryFileBack()
    {
        var before = await FilesAsync();
        await _scenario.Apply(_scenario.Writer).ExecuteAsync(new(Documents, await PlanAsync()));

        var restored = await _scenario.Backups.RestoreAsync(Assert.Single(await _scenario.Backups.ListAsync(Documents)));

        Assert.Equal(2, restored.Value.Restored);
        Assert.Equal(before, await FilesAsync());
    }

    [Fact]
    public async Task Restore_MovedFileGone_CopiesItBackFromTheLibraryByHash()
    {
        _scenario.Write(_downloads, "mine/mine.dds", "the user's own");
        Assert.True((await _scenario.ModLibrary.ImportAsync(Path.Combine(_downloads, "mine"))).IsSuccess);
        await _scenario.Apply(_scenario.Writer).ExecuteAsync(new(Documents, await PlanAsync()));
        _scenario.FileSystem.File.Delete(_scenario.Full(Documents, MineTarget));
        _scenario.Write(Documents, Mine + ".daorework-restore", "a restore killed mid-co");

        var restored = await _scenario.Backups.RestoreAsync(Assert.Single(await _scenario.Backups.ListAsync(Documents)));

        Assert.True(restored.IsSuccess, restored.Error?.Message);
        Assert.Equal("the user's own", Read(Mine));
        Assert.Equal("two", Read(Two));
        Assert.False(_scenario.FileSystem.File.Exists(_scenario.Full(Documents, Mine + ".daorework-restore")));
    }

    [Fact]
    public async Task Restore_ADifferentFileWhereTheMovedOneWas_IsNeverOverwritten()
    {
        await _scenario.Apply(_scenario.Writer).ExecuteAsync(new(Documents, await PlanAsync()));
        _scenario.Write(Documents, Mine, "something new");

        var restored = await _scenario.Backups.RestoreAsync(Assert.Single(await _scenario.Backups.ListAsync(Documents)));

        Assert.Equal(DiagnosticCodes.ModRestoreFailed, restored.Error!.Code);
        Assert.Equal("something new", Read(Mine));
        Assert.Equal("the user's own", Read(MineTarget));
    }

    [Fact]
    public async Task Move_FileChangedOrTargetTaken_IsRefusedAndNothingMoves()
    {
        var plan = await PlanAsync();
        _scenario.Write(Documents, Mine, "edited since");
        _scenario.Write(Documents, TwoTarget, "already here");

        var changed = await _scenario.Writer.ApplyAsync(Documents, plan.Operations[0]);
        var taken = await _scenario.Writer.ApplyAsync(Documents, plan.Operations[1]);

        Assert.Contains("changed after the plan was made", changed.Error!.Message, StringComparison.Ordinal);
        Assert.Contains("already exists", taken.Error!.Message, StringComparison.Ordinal);
        Assert.Equal("edited since", Read(Mine));
        Assert.Equal("two", Read(Two));
    }

    [Fact]
    public async Task Move_Repeated_SucceedsWithoutChangingAnything()
    {
        var move = (await PlanAsync()).Operations[0];

        Assert.True((await _scenario.Writer.ApplyAsync(Documents, move)).IsSuccess);
        Assert.True((await _scenario.Writer.ApplyAsync(Documents, move)).IsSuccess);
        Assert.Equal("the user's own", Read(MineTarget));
    }

    [Fact]
    public async Task Backup_MovedFileChangedAfterThePlan_FailsAndNothingIsApplied()
    {
        var plan = await PlanAsync();
        _scenario.Write(Documents, Two, "edited since");

        var result = await _scenario.Apply(_scenario.Writer).ExecuteAsync(new(Documents, plan));

        Assert.Equal(DiagnosticCodes.ModBackupFailed, result.Error!.Code);
        Assert.Equal("the user's own", Read(Mine));
    }

    [Theory]
    [InlineData(0, false, RecoveryChoice.Finish)]
    [InlineData(0, false, RecoveryChoice.RollBack)]
    [InlineData(0, true, RecoveryChoice.Finish)]
    [InlineData(0, true, RecoveryChoice.RollBack)]
    [InlineData(1, false, RecoveryChoice.Finish)]
    [InlineData(1, false, RecoveryChoice.RollBack)]
    [InlineData(1, true, RecoveryChoice.Finish)]
    [InlineData(1, true, RecoveryChoice.RollBack)]
    public async Task Apply_DyingAtAnyMove_RecoversToExactlyOneEndState(int step, bool moved, RecoveryChoice choice)
    {
        var before = await _scenario.SnapshotAsync();
        var plan = await PlanAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() => _scenario.Apply(new DyingWriter(_scenario, step, moved)).ExecuteAsync(new(Documents, plan)));
        var recovered = await _scenario.Recover().ExecuteAsync(new(Documents, choice));

        Assert.True(recovered.IsSuccess, recovered.Error?.Message);
        var after = await _scenario.SnapshotAsync();
        if (choice == RecoveryChoice.RollBack)
        {
            Assert.Equal(before, after);
        }
        else
        {
            Assert.Contains($"{MineTarget}=the user's own", after);
            Assert.Contains($"{TwoTarget}=two", after);
            Assert.DoesNotContain(after, f => f.StartsWith("packages/core/override/My Mods", StringComparison.Ordinal));
            Assert.Contains("record=ui,old,mine", after);
        }
    }

    private static PlanOperation Move(string from, string to, string content) =>
        new(PlanAction.Move, to, "mine", null, Sha(content)) { MovedFrom = from };

    private string Read(string relative) => _scenario.FileSystem.File.ReadAllText(_scenario.Full(Documents, relative));

    private async Task<List<string>> FilesAsync() => (await _scenario.SnapshotAsync()).Where(f => !f.StartsWith("record=", StringComparison.Ordinal)).ToList();

    private async Task<ModActivationPlan> PlanAsync()
    {
        var current = (await _scenario.Records.LoadAsync(Documents)).Value;
        var next = current with
        {
            ActiveModIds = [.. current.ActiveModIds, "mine"],
            Files = [.. current.Files, new RecordedFile(MineTarget, Sha("the user's own"), "mine"), new RecordedFile(TwoTarget, Sha("two"), "mine")],
        };
        return new ModActivationPlan(current.ActiveModIds, next.ActiveModIds, [Move(Mine, MineTarget, "the user's own"), Move(Two, TwoTarget, "two")], [], 0, [], [], new ConflictReport([], [], [], [], []))
        {
            CurrentRecord = current,
            NextRecord = next,
        };
    }

    /// <summary>The real writer until step <c>dieAt</c>; there it dies before the rename, or after it but before the step is logged.</summary>
    private sealed class DyingWriter(ModScenario scenario, int dieAt, bool afterTheMove) : IDeploymentWriter
    {
        private int _step;

        public async Task<Result> ApplyAsync(string documentsDirectory, PlanOperation operation, CancellationToken cancellationToken = default)
        {
            if (_step++ != dieAt)
            {
                return await scenario.Writer.ApplyAsync(documentsDirectory, operation, cancellationToken);
            }

            if (afterTheMove)
            {
                await scenario.Writer.ApplyAsync(documentsDirectory, operation, cancellationToken);
            }

            throw new InvalidOperationException("The process died.");
        }

        public Task<Result> EditAddInsListAsync(string documentsDirectory, IReadOnlyList<AddInsListChange> changes, CancellationToken cancellationToken = default) =>
            scenario.Writer.EditAddInsListAsync(documentsDirectory, changes, cancellationToken);
    }
}
