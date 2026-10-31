using System.Collections.Concurrent;

namespace DAORework.FaultInjection.Tests.Gate;

/// <summary>
/// The Phase 2 exit gate (ADR-0014): runs a scenario once to count its writes, then, for every write and for both
/// recovery choices, starts from scratch, kills the CLI just before that write, and settles what it left. A journal
/// must recover to exactly the before- or after-state the choice asks for; no journal means the folders are already
/// exactly one of them. Either way the record must then read, and a rollback must undo the last change that happened.
/// </summary>
internal static class GateRunner
{
    public static async Task<GateResult> RunAsync(GateScenario scenario)
    {
        var reference = await ReferenceAsync(scenario);
        var failures = new ConcurrentBag<string>();
        var journals = 0;
        var points = Enumerable.Range(0, reference.Writes).SelectMany(n => new[] { (Write: n, Finish: true), (Write: n, Finish: false) });
        await Parallel.ForEachAsync(points, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, async (point, _) =>
        {
            var (failure, journal) = await KillAndSettleAsync(scenario, reference, point.Write, point.Finish);
            if (failure is not null)
            {
                failures.Add(failure);
            }

            if (journal)
            {
                Interlocked.Increment(ref journals);
            }
        });

        return new GateResult([.. failures.Order(StringComparer.Ordinal)], reference.Writes * 2, journals);
    }

    private static async Task<GateReference> ReferenceAsync(GateScenario scenario)
    {
        using var folders = new GateFolders();
        var ids = GateFixtures.Prepare(folders);
        var previous = FolderState.Take(folders);
        foreach (var setup in scenario.Setup)
        {
            previous = FolderState.Take(folders);
            GateFixtures.Cli(folders, GateFixtures.Resolve(setup, ids));
        }

        var before = FolderState.Take(folders);
        var run = await ChildCli.RunAsync(folders.AppData, ChildCli.NeverKill, GateFixtures.Resolve(scenario.Command, ids));
        if (run.ExitCode != 0 || run.Writes <= 0)
        {
            throw new InvalidOperationException($"{scenario.Name} did not run cleanly without a kill: {run.Output}");
        }

        return new GateReference(run.Writes, previous, before, FolderState.Take(folders));
    }

    private static async Task<(string? Failure, bool Journal)> KillAndSettleAsync(GateScenario scenario, GateReference reference, int write, bool finish)
    {
        using var folders = new GateFolders();
        var ids = GateFixtures.Prepare(folders);
        foreach (var setup in scenario.Setup)
        {
            GateFixtures.Cli(folders, GateFixtures.Resolve(setup, ids));
        }

        var run = await ChildCli.RunAsync(folders.AppData, write, GateFixtures.Resolve(scenario.Command, ids));
        var label = $"{scenario.Name}, {run.KilledBefore}, {(finish ? "finish" : "roll back")}";
        if (!run.Killed)
        {
            return ($"{scenario.Name}: not killed before write {write} (exit {run.ExitCode}): {run.Output}", false);
        }

        var journal = FolderState.HasJournal(folders, scenario.OnInstall ? folders.Install : folders.Documents);
        var settled = journal ? Recover(folders, scenario, reference, finish, out var failure) : AlreadySettled(folders, reference, out failure);
        if (settled is null)
        {
            return ($"{label}: {failure}", journal);
        }

        return (CheckAfterwards(folders, scenario, reference, settled) is { } problem ? $"{label}: {problem}" : null, journal);
    }

    private static IReadOnlyList<string>? Recover(GateFolders folders, GateScenario scenario, GateReference reference, bool finish, out string failure)
    {
        var expected = finish ? reference.After : reference.Before;
        var recovered = InProcessCli.Run(folders.AppData, scenario.Recover(finish));
        var state = FolderState.Take(folders);
        failure = recovered.ExitCode != 0 ? $"recover failed: {recovered.Output.Trim()}"
            : !state.SequenceEqual(expected) ? $"recover left a mix: {FolderState.Diff(expected, state)}"
            : string.Empty;
        return failure.Length == 0 ? expected : null;
    }

    private static IReadOnlyList<string>? AlreadySettled(GateFolders folders, GateReference reference, out string failure)
    {
        var state = FolderState.Take(folders);
        var settled = state.SequenceEqual(reference.Before) ? reference.Before : state.SequenceEqual(reference.After) ? reference.After : null;
        failure = settled is null ? $"no journal, yet the folders are neither before nor after: {FolderState.Diff(reference.Before, state)}" : string.Empty;
        return settled;
    }

    /// <summary>The record reads, and a rollback goes back one real change: to the before-state from the after-state, else to the one before.</summary>
    private static string? CheckAfterwards(GateFolders folders, GateScenario scenario, GateReference reference, IReadOnlyList<string> settled)
    {
        var status = InProcessCli.Run(folders.AppData, scenario.Status);
        if (status.ExitCode != 0)
        {
            return $"status failed afterwards: {status.Output.Trim()}";
        }

        InProcessCli.Run(folders.AppData, scenario.Rollback);
        var expected = ReferenceEquals(settled, reference.After) ? reference.Before : reference.Previous;
        var state = FolderState.Take(folders);
        return state.SequenceEqual(expected) ? null : $"rollback did not undo the last change that happened: {FolderState.Diff(expected, state)}";
    }
}
