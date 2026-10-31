using DAORework.Core.Common;
using DAORework.Core.Mods.Conflicts;
using DAORework.Core.Mods.Library;

namespace DAORework.Core.Mods.Planning;

/// <summary>
/// Builds the <see cref="ModActivationPlan"/> that turns the current deployment into the requested list of mods
/// (ADR-0012). Pure: every fact about the disk comes in through <see cref="PlanInput"/>. The rules that keep the
/// user's files safe live here: only recorded files whose hash still matches are replaced or deleted, nothing is ever
/// written over a file DAORework did not place, and an add-in already registered by hand is not registered again.
/// </summary>
public static class ModActivationPlanner
{
    private static readonly StringComparer _paths = StringComparer.OrdinalIgnoreCase;

    public static Result<ModActivationPlan> Plan(PlanInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (!ConflictScanner.Scan(input.Mods, input.UnmanagedOverridePaths).TryGetValue(out var conflicts, out var error))
        {
            return Result.Failure<ModActivationPlan>(error);
        }

        var state = new PlanState(input);
        state.Blockers.AddRange(conflicts.PathCollisions.Select(c => new PlanBlocker(
            PlanBlockerKind.PathCollision,
            c.TargetPath,
            $"{string.Join(" and ", c.Files.Select(f => f.ModId).Distinct(_paths))} would both write {c.TargetPath}.")));

        // A path collision leaves two files on one path; it already blocks the plan, so each path is planned once.
        var desired = conflicts.Deploy.DistinctBy(f => f.TargetPath, _paths).ToList();
        foreach (var file in desired)
        {
            PlanDesired(state, file);
        }

        var desiredPaths = desired.Select(f => f.TargetPath).ToHashSet(_paths);
        foreach (var recorded in input.Current.Files.Where(r => !desiredPaths.Contains(r.TargetPath)))
        {
            PlanNoLongerDesired(state, recorded);
        }

        PlanAddIns(state, input);
        return Result.Success(new ModActivationPlan(
            input.Current.ActiveModIds,
            input.Mods.Select(m => m.Id).ToList(),
            state.Operations.OrderBy(o => o.Action).ThenBy(o => o.TargetPath, _paths).ToList(),
            state.AddInsChanges,
            state.Unchanged,
            state.Missing,
            state.Blockers,
            conflicts)
        {
            CurrentRecord = input.Current,
            NextRecord = NextRecord(input, desired, state.AddInsChanges),
        });
    }

    /// <summary>What the record says once the plan is applied: every deployed file with the hash it will have, and the add-ins DAORework added.</summary>
    private static DeploymentRecord NextRecord(PlanInput input, List<DeployedFile> desired, List<AddInsListChange> changes)
    {
        var wanted = input.Mods.Where(m => m.Kind == ModKind.AddIn && m.AddInUid is not null).Select(m => m.AddInUid!).ToHashSet(_paths);
        var addIns = input.Current.AddedAddIns
            .Where(a => wanted.Contains(a.Uid))
            .Concat(changes.Where(c => c.Add).Select(c => new RecordedAddIn(c.Uid, c.ModId)))
            .ToList();
        return new DeploymentRecord(
            input.Mods.Select(m => m.Id).ToList(),
            desired.Select(f => new RecordedFile(f.TargetPath, f.Source.Sha256, f.ModId)).ToList(),
            addIns);
    }

    private static void PlanDesired(PlanState state, DeployedFile file)
    {
        var onDisk = state.HashOnDisk(file.TargetPath);
        if (!state.Recorded.TryGetValue(file.TargetPath, out var recorded))
        {
            if (onDisk is null)
            {
                state.Operations.Add(new PlanOperation(PlanAction.Copy, file.TargetPath, file.ModId, file, null));
            }
            else
            {
                state.Block(PlanBlockerKind.UnmanagedFileInTheWay, file.TargetPath, $"{file.TargetPath} is already there and DAORework did not put it there; it is never overwritten.");
            }

            return;
        }

        if (onDisk is null)
        {
            state.Missing.Add(file.TargetPath);
            state.Operations.Add(new PlanOperation(PlanAction.Copy, file.TargetPath, file.ModId, file, null));
        }
        else if (SameHash(onDisk, file.Source.Sha256))
        {
            state.Unchanged++;
        }
        else if (!SameHash(onDisk, recorded.Sha256))
        {
            state.Block(PlanBlockerKind.ChangedSinceDeployed, file.TargetPath, $"{file.TargetPath} was changed after DAORework placed it; it would be replaced.");
        }
        else
        {
            state.Operations.Add(new PlanOperation(PlanAction.Replace, file.TargetPath, file.ModId, file, recorded.Sha256));
        }
    }

    private static void PlanNoLongerDesired(PlanState state, RecordedFile recorded)
    {
        var onDisk = state.HashOnDisk(recorded.TargetPath);
        if (onDisk is null)
        {
            state.Missing.Add(recorded.TargetPath);
        }
        else if (SameHash(onDisk, recorded.Sha256))
        {
            state.Operations.Add(new PlanOperation(PlanAction.Delete, recorded.TargetPath, recorded.ModId, null, recorded.Sha256));
        }
        else
        {
            state.Block(PlanBlockerKind.ChangedSinceDeployed, recorded.TargetPath, $"{recorded.TargetPath} was changed after DAORework placed it; it would be removed.");
        }
    }

    private static void PlanAddIns(PlanState state, PlanInput input)
    {
        var added = input.Current.AddedAddIns.ToDictionary(a => a.Uid, _paths);
        var registered = input.RegisteredAddInUids.ToHashSet(_paths);
        var wanted = input.Mods.Where(m => m.Kind == ModKind.AddIn && !string.IsNullOrWhiteSpace(m.AddInUid)).ToList();
        foreach (var mod in wanted.Where(m => !added.ContainsKey(m.AddInUid!)))
        {
            if (registered.Contains(mod.AddInUid!))
            {
                state.Block(PlanBlockerKind.AddInAlreadyRegistered, mod.AddInUid!, $"The add-in {mod.AddInUid} is already registered in AddIns.xml, installed some other way.");
            }
            else
            {
                state.AddInsChanges.Add(new AddInsListChange(mod.AddInUid!, mod.Id, Add: true));
            }
        }

        // Only entries DAORework added are removed, and only while they are still there.
        var wantedUids = wanted.Select(m => m.AddInUid!).ToHashSet(_paths);
        state.AddInsChanges.AddRange(added.Values
            .Where(a => !wantedUids.Contains(a.Uid) && registered.Contains(a.Uid))
            .Select(a => new AddInsListChange(a.Uid, a.ModId, Add: false)));
    }

    private static bool SameHash(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

    /// <summary>The lists a plan is built up in, so each rule above stays one small method.</summary>
    private sealed class PlanState(PlanInput input)
    {
        private readonly Dictionary<string, string> _disk = new(input.DiskHashes, _paths);

        public Dictionary<string, RecordedFile> Recorded { get; } = input.Current.Files.ToDictionary(f => f.TargetPath, _paths);

        public List<PlanOperation> Operations { get; } = [];

        public List<AddInsListChange> AddInsChanges { get; } = [];

        public List<PlanBlocker> Blockers { get; } = [];

        public List<string> Missing { get; } = [];

        public int Unchanged { get; set; }

        public string? HashOnDisk(string path) => _disk.GetValueOrDefault(path);

        public void Block(PlanBlockerKind kind, string subject, string message) => Blockers.Add(new PlanBlocker(kind, subject, message));
    }
}
