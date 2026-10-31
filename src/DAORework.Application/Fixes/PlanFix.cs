using DAORework.Application.Abstractions;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Fixes;
using DAORework.Core.Mods.Planning;

namespace DAORework.Application.Fixes;

/// <summary>
/// Works out the plan to apply or remove one fix in an install folder (ADR-0013): the fix prepares its entry, the
/// install folder is hashed and checked for write access, and the fix planner does the rest. Writes nothing.
/// </summary>
public sealed class PlanFix
{
    private readonly IDeploymentRecordStore _records;
    private readonly IApplyJournalStore _journals;
    private readonly IFixLibrary _library;
    private readonly IInstallFiles _files;
    private readonly IReadOnlyList<IManagedFix> _fixes;

    public PlanFix(IDeploymentRecordStore records, IApplyJournalStore journals, IFixLibrary library, IInstallFiles files, IEnumerable<IManagedFix> fixes)
    {
        _records = records;
        _journals = journals;
        _library = library;
        _files = files;
        _fixes = fixes.ToList();
    }

    public async Task<Result<PlannedFix>> ExecuteAsync(PlanFixRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var fix = _fixes.FirstOrDefault(f => f.Id.Equals(request.Fix, StringComparison.OrdinalIgnoreCase) || f.Aliases.Contains(request.Fix, StringComparer.OrdinalIgnoreCase));
        if (fix is null)
        {
            var known = string.Join(", ", _fixes.Select(f => $"{(f.Aliases.Count > 0 ? f.Aliases[0] : f.Id)} ({FixIds.DisplayName(f.Id)})"));
            return Failure(DiagnosticCodes.FixUnknown, $"DAORework has no fix called '{request.Fix}'.", $"Known fixes: {known}.");
        }

        var install = request.InstallDirectory;
        if (await _journals.LoadAsync(install, cancellationToken) is not null)
        {
            return Failure(DiagnosticCodes.ModApplyUnfinished, $"An earlier change in {install} did not finish.", "Run fixes recover to finish it or roll it back first.");
        }

        if (!(await _records.LoadAsync(install, cancellationToken)).TryGetValue(out var record, out var error))
        {
            return Result.Failure<PlannedFix>(error);
        }

        var library = await _library.ListAsync(cancellationToken);
        return request.Kind == FixChangeKind.Apply
            ? await PlanApplyAsync(fix, new FixApplyContext(install, record, library, request.SourcePath), cancellationToken)
            : await PlanRemoveAsync(fix, install, record, library, cancellationToken);
    }

    private static Result<PlannedFix> Failure(string code, string message, string? recommendation = null) =>
        Result.Failure<PlannedFix>(new Error(code, message, recommendation));

    private static PlanBlocker Blocker(Error error) =>
        new(error.Code == DiagnosticCodes.FixFileInUse ? PlanBlockerKind.FileInUse : PlanBlockerKind.InstallNotWritable, string.Empty, $"{error.Message} {error.Recommendation}".TrimEnd());

    private async Task<Result<PlannedFix>> PlanApplyAsync(IManagedFix fix, FixApplyContext context, CancellationToken cancellationToken)
    {
        if (!(await fix.PrepareAsync(context, cancellationToken)).TryGetValue(out var prepared, out var error))
        {
            return Result.Failure<PlannedFix>(error);
        }

        var paths = prepared.Entry.Files.Select(f => f.TargetPath).ToList();
        var state = await StateAsync(context.InstallDirectory, context.Record, paths, prepared.Replaceable, cancellationToken);
        return state.TryGetValue(out var install, out error)
            ? Result.Success(new PlannedFix(FixIds.DisplayName(fix.Id), FixPlanner.PlanApply(prepared.Entry, install), prepared))
            : Result.Failure<PlannedFix>(error);
    }

    private async Task<Result<PlannedFix>> PlanRemoveAsync(IManagedFix fix, string install, DeploymentRecord record, IReadOnlyList<FixEntry> library, CancellationToken cancellationToken)
    {
        var applied = record.ActiveModIds.FirstOrDefault(id => FixIds.IsEntryOf(id, fix.Id));
        if (applied is null)
        {
            return Failure(DiagnosticCodes.FixNotApplied, $"DAORework has not applied the {FixIds.DisplayName(fix.Id)} in {install}.", "Run fixes status to see what it has applied.");
        }

        var paths = record.Files.Where(f => f.ModId.Equals(applied, StringComparison.OrdinalIgnoreCase)).Select(f => f.TargetPath).ToList();
        var state = await StateAsync(install, record, paths, new HashSet<string>(), cancellationToken);
        return state.TryGetValue(out var current, out var error)
            ? Result.Success(new PlannedFix(FixIds.DisplayName(fix.Id), FixPlanner.PlanRemove(applied, current, library), Prepared: null))
            : Result.Failure<PlannedFix>(error);
    }

    private async Task<Result<InstallState>> StateAsync(string install, DeploymentRecord record, IReadOnlyList<string> paths, IReadOnlySet<string> replaceable, CancellationToken cancellationToken)
    {
        if (!(await _files.HashAsync(install, paths, cancellationToken)).TryGetValue(out var hashes, out var error))
        {
            return Result.Failure<InstallState>(error);
        }

        var problems = await _files.CheckWritableAsync(install, paths, cancellationToken);
        return Result.Success(new InstallState(record, hashes, replaceable, problems.Select(Blocker).ToList()));
    }
}
