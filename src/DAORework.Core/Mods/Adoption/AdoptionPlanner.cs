using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Mods.Library;
using DAORework.Core.Mods.Planning;
using DAORework.Core.Mods.Safety;

namespace DAORework.Core.Mods.Adoption;

/// <summary>
/// Plans an adoption (ADR-0015, ROADMAP P2.5-03) with <see cref="ModActivationPlanner"/>'s own rules. The adopted mods
/// are appended to the active list, and the planner is shown a record in which their files are already DAORework's at
/// their targets and their add-ins already registered by it. What it plans is then put back on the real folder: a copy
/// to an adopted file's target becomes a move from where the file is, a file already at its target is only recorded,
/// and an adopted file that loses to a later one by name is deleted where it is, with a backup. The plan keeps the
/// real record, so a rollback returns to it. Pure.
/// </summary>
public static class AdoptionPlanner
{
    private static readonly StringComparer _paths = StringComparer.OrdinalIgnoreCase;

    /// <param name="input">The folder as it is, with the active mods; the adopted mods' files left out of <see cref="PlanInput.UnmanagedOverridePaths"/>.</param>
    /// <param name="adopted">The mods to adopt, in the order the user confirmed; later ones win by file name.</param>
    public static Result<ModActivationPlan> Plan(PlanInput input, IReadOnlyList<AdoptedMod> adopted)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(adopted);

        if (adopted.FirstOrDefault(a => input.Mods.Any(m => _paths.Equals(m.Id, a.Mod.Id))) is { } active)
        {
            return Failed(DiagnosticCodes.ModAdoptAlreadyActive, $"{active.Mod.Name} ({active.Mod.Id}) is already active; there is nothing to adopt.");
        }

        if (!Sources(adopted).TryGetValue(out var sources, out var error))
        {
            return Result.Failure<ModActivationPlan>(error);
        }

        var current = input.Current;
        var virtualCurrent = current with
        {
            Files = [.. current.Files, .. sources.Select(s => new RecordedFile(s.Key, s.Value.Sha256, s.Value.ModId))],
            AddedAddIns = [.. current.AddedAddIns, .. adopted.Select(a => a.Mod.AddInUid is { } uid ? new RecordedAddIn(uid, a.Mod.Id) : null).OfType<RecordedAddIn>()],
        };
        if (!ModActivationPlanner.Plan(input with { Current = virtualCurrent, Mods = [.. input.Mods, .. adopted.Select(a => a.Mod)] }).TryGetValue(out var plan, out error))
        {
            return Result.Failure<ModActivationPlan>(error);
        }

        return Result.Success(OnTheRealFolder(plan, current, sources));
    }

    private static ModActivationPlan OnTheRealFolder(ModActivationPlan plan, DeploymentRecord current, Dictionary<string, Source> sources)
    {
        var desired = plan.NextRecord.Files.Select(f => f.TargetPath).ToHashSet(_paths);
        var operations = plan.Operations
            .Select(o => o.Action == PlanAction.Copy && sources.TryGetValue(o.TargetPath, out var s)
                ? new PlanOperation(PlanAction.Move, o.TargetPath, o.ModId, null, s.Sha256) { MovedFrom = s.Path }
                : o)
            .Concat(sources
                .Where(s => !desired.Contains(s.Key) && !_paths.Equals(s.Key, s.Value.Path))
                .Select(s => new PlanOperation(PlanAction.Delete, s.Value.Path, s.Value.ModId, null, s.Value.Sha256)))
            .OrderBy(o => o.Action)
            .ThenBy(o => o.TargetPath, _paths)
            .ToList();
        return plan with
        {
            Operations = operations,
            UnchangedFileCount = plan.UnchangedFileCount - sources.Count(s => _paths.Equals(s.Key, s.Value.Path) && desired.Contains(s.Key)),
            MissingRecordedFiles = plan.MissingRecordedFiles.Where(m => !sources.ContainsKey(m)).ToList(),
            CurrentRecord = current,
            AdoptedInPlaceCount = sources.Count(s => _paths.Equals(s.Key, s.Value.Path) && desired.Contains(s.Key)),
        };
    }

    /// <summary>
    /// Every adopted data file's target, with where it is now, its hash and its mod; a deployed file with no source fails.
    /// Program files are left out: they are never deployed, so an adoption leaves them where they are (ADR-0018).
    /// </summary>
    private static Result<Dictionary<string, Source>> Sources(IReadOnlyList<AdoptedMod> adopted)
    {
        var sources = new Dictionary<string, Source>(_paths);
        foreach (var mod in adopted)
        {
            if (!ModLayout.Map(mod.Mod).TryGetValue(out var files, out var error))
            {
                return Result.Failure<Dictionary<string, Source>>(error);
            }

            foreach (var file in files.Where(f => !ProgramFiles.IsProgram(f.Source)))
            {
                if (!mod.SourceByLibraryPath.TryGetValue(file.Source.RelativePath, out var path))
                {
                    return Result.Failure<Dictionary<string, Source>>(new Error(
                        DiagnosticCodes.ModAdoptSourceMissing,
                        $"{file.Source.RelativePath} of {mod.Mod.Name} is not in the documents folder, so the mod cannot be adopted as it is."));
                }

                sources[file.TargetPath] = new Source(path, file.Source.Sha256, mod.Mod.Id);
            }
        }

        return Result.Success(sources);
    }

    private static Result<ModActivationPlan> Failed(string code, string message) => Result.Failure<ModActivationPlan>(new Error(code, message));

    private sealed record Source(string Path, string Sha256, string ModId);
}
