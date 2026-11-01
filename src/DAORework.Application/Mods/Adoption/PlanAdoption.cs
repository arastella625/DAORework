using DAORework.Application.Abstractions;
using DAORework.Application.Mods.Combining;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Mods.Adoption;
using DAORework.Core.Mods.Library;
using DAORework.Core.Mods.Planning;

namespace DAORework.Application.Mods.Adoption;

/// <summary>
/// Works out the plan for adopting hand-installed mods (ROADMAP P2.5-03, ADR-0015). A candidate that is exactly a
/// library mod is that mod; any other is imported into the library from its files first, which writes only app-data.
/// The plan itself is <see cref="AdoptionPlanner"/>'s, applied like any mod change by <c>ApplyModPlan</c>.
/// </summary>
public sealed class PlanAdoption
{
    private readonly IAdoptionImporter _importer;
    private readonly IModLibrary _library;
    private readonly IDeploymentRecordStore _records;
    private readonly IDocumentsScanner _scanner;
    private readonly IApplyJournalStore _journals;

    public PlanAdoption(IAdoptionImporter importer, IModLibrary library, IDeploymentRecordStore records, IDocumentsScanner scanner, IApplyJournalStore journals)
    {
        _importer = importer;
        _library = library;
        _records = records;
        _scanner = scanner;
        _journals = journals;
    }

    public async Task<Result<ModActivationPlan>> ExecuteAsync(PlanAdoptionRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var documents = request.DocumentsDirectory;
        if (await _journals.LoadAsync(documents, cancellationToken) is not null)
        {
            return Failure(new Error(DiagnosticCodes.ModApplyUnfinished, $"An earlier mod change in {documents} did not finish.", "Run mods recover to finish it or roll it back first."));
        }

        if (!(await _records.LoadAsync(documents, cancellationToken)).TryGetValue(out var record, out var error))
        {
            return Failure(error);
        }

        var library = (await _library.ListAsync(cancellationToken)).ToDictionary(e => e.Mod.Id, e => e.Mod, StringComparer.OrdinalIgnoreCase);
        var adopted = new List<AdoptedMod>();
        foreach (var candidate in request.Candidates)
        {
            if (!(await AdoptedAsync(documents, candidate, request.LooseFilesName, library, cancellationToken)).TryGetValue(out var mod, out error))
            {
                return Failure(error);
            }

            adopted.Add(mod);
        }

        if (record.ActiveModIds.FirstOrDefault(id => !library.ContainsKey(id)) is { } missing)
        {
            return Failure(new Error(DiagnosticCodes.ModNotInLibrary, $"Not in the library: {missing}.", "Run mods library for the ids, or mods import to add a mod."));
        }

        return await PlanAsync(documents, record, record.ActiveModIds.Select(id => library[id]).ToList(), adopted, cancellationToken);
    }

    private static Result<ModActivationPlan> Failure(Error error) => Result.Failure<ModActivationPlan>(error);

    private async Task<Result<ModActivationPlan>> PlanAsync(string documents, DeploymentRecord record, List<LibraryMod> active, List<AdoptedMod> adopted, CancellationToken cancellationToken)
    {
        // The adopted mods come after the active ones, so their copies of chargenmorphcfg.xml combine last (ADR-0025).
        if (!(await CombineChargenMorph.PrepareAsync(_library, [.. active, .. adopted.Select(a => a.Mod)], cancellationToken)).TryGetValue(out var combine, out var error))
        {
            return Failure(error);
        }

        var sources = adopted.SelectMany(a => a.SourceByLibraryPath.Values).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var paths = record.Files.Select(f => f.TargetPath)
            .Concat(active.Concat(adopted.Select(a => a.Mod)).SelectMany(m => ModLayout.Map(m).TryGetValue(out var files, out _) ? files.Select(f => f.TargetPath) : []))
            .Concat(sources)
            .Concat(combine.TargetPaths)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (!(await _scanner.ScanAsync(documents, paths, cancellationToken)).TryGetValue(out var disk, out error))
        {
            return Failure(error);
        }

        // The adopted files become DAORework's, so they are no longer the user's own override files.
        var unmanaged = disk.UnmanagedOverridePaths.Where(p => !sources.Contains(ModLayout.OverridePrefix + p)).ToList();
        return AdoptionPlanner.Plan(
            new PlanInput(record, active, unmanaged, disk.Hashes, disk.RegisteredAddInUids) { Combined = combine.Combined, CombineProblems = combine.Problems },
            adopted);
    }

    private async Task<Result<AdoptedMod>> AdoptedAsync(
        string documents,
        AdoptionCandidate candidate,
        string? looseFilesName,
        Dictionary<string, LibraryMod> library,
        CancellationToken cancellationToken)
    {
        if (candidate.LibraryMatch is { IsExact: true } match && library.TryGetValue(match.ModId, out var matched))
        {
            return Result.Success(new AdoptedMod(matched, match.Sources));
        }

        var name = candidate.Kind == AdoptionCandidateKind.LooseOverrideFiles ? looseFilesName : candidate.Name;
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<AdoptedMod>(new Error(DiagnosticCodes.ModAdoptNameRequired, "The loose override files need a name to be adopted as one mod.", "Add --name \"<name>\"."));
        }

        if (!(await _importer.ImportAsync(documents, candidate, name, cancellationToken)).TryGetValue(out var entry, out var error))
        {
            return Result.Failure<AdoptedMod>(error);
        }

        library[entry.Mod.Id] = entry.Mod;
        return Result.Success(new AdoptedMod(entry.Mod, AdoptionLayout.Sources(candidate)));
    }
}
