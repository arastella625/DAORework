using DAORework.Application.Abstractions;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Mods.Apply;
using DAORework.Core.Mods.Planning;
using Xunit;

namespace DAORework.Infrastructure.Tests.Mods;

/// <summary>
/// The journal's promise (ROADMAP P2-05; the Phase 2 exit gate in miniature): an apply that dies at any step, before
/// or after that step's effect reached the disk, is recovered to exactly the before-state or exactly the after-state,
/// whichever the user chooses, and never a mix.
/// </summary>
public sealed class ApplyRecoveryTests
{
    public enum Crash
    {
        /// <summary>The process dies while writing: a half-written temporary file is left next to the target.</summary>
        MidStep,

        /// <summary>The step's change reached the disk but the process died before logging it as done.</summary>
        AfterEffect,
    }

    public static TheoryData<int, Crash, RecoveryChoice> EveryStep()
    {
        var data = new TheoryData<int, Crash, RecoveryChoice>();
        for (var step = 0; step < 6; step++)
        {
            foreach (var crash in Enum.GetValues<Crash>())
            {
                data.Add(step, crash, RecoveryChoice.Finish);
                data.Add(step, crash, RecoveryChoice.RollBack);
            }
        }

        return data;
    }

    [Fact]
    public async Task Apply_Uninterrupted_ReachesThePlannedStateAndLeavesNoJournal()
    {
        var scenario = new ModScenario();
        var before = await scenario.SnapshotAsync();
        var plan = await scenario.PlanAsync();

        var report = (await scenario.Apply(scenario.Writer).ExecuteAsync(new(ModScenario.Documents, plan))).Value;

        Assert.Equal(6, report.StepsRun);
        Assert.Null(await scenario.Journals.LoadAsync(ModScenario.Documents));
        var after = await scenario.SnapshotAsync();
        Assert.NotEqual(before, after);
        Assert.Contains("packages/core/override/DAORework/ui/ui.gda=ui v2", after);
        Assert.Contains("packages/core/override/My Mods/mine.dds=the user's own", after);
        Assert.DoesNotContain(after, f => f.StartsWith("packages/core/override/DAORework/old", StringComparison.Ordinal));
        Assert.Contains("record=ui,hair,fixpack", after);
        var addIns = scenario.FileSystem.File.ReadAllText(scenario.Full(ModScenario.Documents, "Settings/AddIns.xml"));
        Assert.Contains("users_own", addIns, StringComparison.Ordinal);
        Assert.Contains("qwinn_fixpack_3", addIns, StringComparison.Ordinal);
    }

    [Theory]
    [MemberData(nameof(EveryStep))]
    public async Task Apply_DyingAtAnyStep_RecoversToExactlyOneEndState(int step, Crash crash, RecoveryChoice choice)
    {
        var expected = choice == RecoveryChoice.Finish ? await AfterStateAsync() : await BeforeStateAsync();
        var scenario = new ModScenario();
        var plan = await scenario.PlanAsync();

        await Assert.ThrowsAsync<SimulatedCrash>(() => scenario.Apply(new CrashingWriter(scenario, step, crash)).ExecuteAsync(new(ModScenario.Documents, plan)));
        var blocked = await scenario.Apply(scenario.Writer).ExecuteAsync(new(ModScenario.Documents, plan));
        var recovered = await scenario.Recover().ExecuteAsync(new(ModScenario.Documents, choice));

        Assert.Equal(DiagnosticCodes.ModApplyUnfinished, blocked.Error!.Code);
        Assert.True(recovered.IsSuccess, recovered.Error?.Message);
        Assert.Equal(expected, await scenario.SnapshotAsync());
        Assert.Null(await scenario.Journals.LoadAsync(ModScenario.Documents));
    }

    [Fact]
    public async Task Recover_WithNothingUnfinished_SaysSo()
    {
        var result = await new ModScenario().Recover().ExecuteAsync(new(ModScenario.Documents, RecoveryChoice.Finish));

        Assert.Equal(DiagnosticCodes.ModNothingToRecover, result.Error!.Code);
    }

    [Fact]
    public async Task Apply_StepThatFailsCleanly_KeepsTheJournalForRecovery()
    {
        var scenario = new ModScenario();
        var plan = await scenario.PlanAsync();
        scenario.FileSystem.File.WriteAllText(Path.Combine(ModScenario.AppData, "library", "hair", "eyes.dds"), "tampered");

        var result = await scenario.Apply(scenario.Writer).ExecuteAsync(new(ModScenario.Documents, plan));

        Assert.Equal(DiagnosticCodes.ModApplyFailed, result.Error!.Code);
        Assert.Contains("does not match its recorded hash", result.Error.Message, StringComparison.Ordinal);
        Assert.NotNull(await scenario.Journals.LoadAsync(ModScenario.Documents));
        Assert.True((await scenario.Recover().ExecuteAsync(new(ModScenario.Documents, RecoveryChoice.RollBack))).IsSuccess);
        Assert.Equal(await BeforeStateAsync(), await scenario.SnapshotAsync());
    }

    private static Task<List<string>> BeforeStateAsync() => new ModScenario().SnapshotAsync();

    private static async Task<List<string>> AfterStateAsync()
    {
        var scenario = new ModScenario();
        await scenario.Apply(scenario.Writer).ExecuteAsync(new(ModScenario.Documents, await scenario.PlanAsync()));
        return await scenario.SnapshotAsync();
    }

    private sealed class SimulatedCrash : Exception
    {
    }

    /// <summary>Behaves like the real writer until step <c>crashAt</c>, then dies the way <see cref="Crash"/> says.</summary>
    private sealed class CrashingWriter(ModScenario scenario, int crashAt, Crash crash) : IDeploymentWriter
    {
        private int _step;

        public async Task<Result> ApplyAsync(string documentsDirectory, PlanOperation operation, CancellationToken cancellationToken = default)
        {
            if (_step++ != crashAt)
            {
                return await scenario.Writer.ApplyAsync(documentsDirectory, operation, cancellationToken);
            }

            if (crash == Crash.AfterEffect)
            {
                await scenario.Writer.ApplyAsync(documentsDirectory, operation, cancellationToken);
            }
            else if (operation.Action != PlanAction.Delete)
            {
                scenario.Write(documentsDirectory, operation.TargetPath + ".daorework-apply", "half a fi");
            }

            throw new SimulatedCrash();
        }

        public async Task<Result> EditAddInsListAsync(string documentsDirectory, IReadOnlyList<AddInsListChange> changes, CancellationToken cancellationToken = default)
        {
            if (_step++ != crashAt)
            {
                return await scenario.Writer.EditAddInsListAsync(documentsDirectory, changes, cancellationToken);
            }

            if (crash == Crash.AfterEffect)
            {
                await scenario.Writer.EditAddInsListAsync(documentsDirectory, changes, cancellationToken);
            }
            else
            {
                scenario.Write(documentsDirectory, "Settings/AddIns.xml.daorework-apply", "<AddInsLi");
            }

            throw new SimulatedCrash();
        }
    }
}
