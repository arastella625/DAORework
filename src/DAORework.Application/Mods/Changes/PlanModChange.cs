using DAORework.Application.Abstractions;
using DAORework.Application.Mods.Combining;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Mods.Library;
using DAORework.Core.Mods.Planning;

namespace DAORework.Application.Mods.Changes;

/// <summary>
/// Works out the plan for a requested change (ROADMAP P2-06): the new ordered list of active mods from the deployment
/// record, the target mods from the library, and the documents folder's current state; then the planner (P2-03).
/// Reads only. An unfinished apply is settled first, since its folder is in neither end state.
/// </summary>
public sealed class PlanModChange
{
    private readonly IDeploymentRecordStore _records;
    private readonly IModLibrary _library;
    private readonly IDocumentsScanner _scanner;
    private readonly IApplyJournalStore _journals;

    public PlanModChange(IDeploymentRecordStore records, IModLibrary library, IDocumentsScanner scanner, IApplyJournalStore journals)
    {
        _records = records;
        _library = library;
        _scanner = scanner;
        _journals = journals;
    }

    public async Task<Result<ModActivationPlan>> ExecuteAsync(PlanModChangeRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var documents = request.DocumentsDirectory;
        if (await _journals.LoadAsync(documents, cancellationToken) is not null)
        {
            return Failure(DiagnosticCodes.ModApplyUnfinished, $"An earlier mod change in {documents} did not finish.", "Run mods recover to finish it or roll it back first.");
        }

        if (!(await _records.LoadAsync(documents, cancellationToken)).TryGetValue(out var record, out var error))
        {
            return Result.Failure<ModActivationPlan>(error);
        }

        var mods = await TargetModsAsync(record, request.Change, cancellationToken);
        if (!mods.TryGetValue(out var targets, out error))
        {
            return Result.Failure<ModActivationPlan>(error);
        }

        // Copies of chargenmorphcfg.xml from several mods are combined into one file (ADR-0025).
        if (!(await CombineChargenMorph.PrepareAsync(_library, targets, cancellationToken)).TryGetValue(out var combine, out error))
        {
            return Result.Failure<ModActivationPlan>(error);
        }

        var paths = record.Files.Select(f => f.TargetPath)
            .Concat(targets.SelectMany(m => ModLayout.Map(m).TryGetValue(out var files, out _) ? files.Select(f => f.TargetPath) : []))
            .Concat(combine.TargetPaths)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (!(await _scanner.ScanAsync(documents, paths, cancellationToken)).TryGetValue(out var disk, out error))
        {
            return Result.Failure<ModActivationPlan>(error);
        }

        return ModActivationPlanner.Plan(new PlanInput(record, targets, disk.UnmanagedOverridePaths, disk.Hashes, disk.RegisteredAddInUids)
        {
            Combined = combine.Combined,
            CombineProblems = combine.Problems,
        });
    }

    private static Result<ModActivationPlan> Failure(string code, string message, string? recommendation = null) =>
        Result.Failure<ModActivationPlan>(new Error(code, message, recommendation));

    private async Task<Result<IReadOnlyList<LibraryMod>>> TargetModsAsync(DeploymentRecord record, ModChange change, CancellationToken cancellationToken)
    {
        var library = (await _library.ListAsync(cancellationToken)).ToDictionary(e => e.Mod.Id, e => e.Mod, StringComparer.OrdinalIgnoreCase);
        if (change.Kind == ModChangeKind.Deactivate && change.ModIds.FirstOrDefault(id => !record.ActiveModIds.Contains(id, StringComparer.OrdinalIgnoreCase)) is { } inactive)
        {
            return Result.Failure<IReadOnlyList<LibraryMod>>(new Error(DiagnosticCodes.ModNotActive, $"{inactive} is not active in this documents folder."));
        }

        var targetIds = change.Apply(record.ActiveModIds);
        var missing = targetIds.Where(id => !library.ContainsKey(id)).ToList();
        return missing.Count > 0
            ? Result.Failure<IReadOnlyList<LibraryMod>>(new Error(
                DiagnosticCodes.ModNotInLibrary,
                $"Not in the library: {string.Join(", ", missing)}.",
                "Run mods library for the ids, or mods import to add a mod."))
            : Result.Success<IReadOnlyList<LibraryMod>>(targetIds.Select(id => library[id]).ToList());
    }
}
