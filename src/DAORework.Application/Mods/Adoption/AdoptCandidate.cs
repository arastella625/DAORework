using DAORework.Application.Abstractions;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Mods.Adoption;
using DAORework.Core.Mods.Library;
using Microsoft.Extensions.Logging;

namespace DAORework.Application.Mods.Adoption;

/// <summary>
/// Turns one candidate into the library mod it is, importing it if it has to (ADR-0015). A candidate is where a batch
/// meets a stranger's files, so whatever goes wrong with one is that candidate's failure, not the batch's (P2.6-53).
/// </summary>
public sealed class AdoptCandidate
{
    private readonly IAdoptionImporter _importer;
    private readonly ILogger<AdoptCandidate> _logger;

    public AdoptCandidate(IAdoptionImporter importer, ILogger<AdoptCandidate> logger)
    {
        _importer = importer;
        _logger = logger;
    }

    /// <param name="documents">The documents folder.</param>
    /// <param name="candidate">What to adopt.</param>
    /// <param name="looseFilesName">The name for loose override files.</param>
    /// <param name="library">The library by id, which a new import is added to.</param>
    /// <param name="cancellationToken">Stops the import.</param>
    public async Task<Result<AdoptedMod>> ExecuteAsync(
        string documents,
        AdoptionCandidate candidate,
        string? looseFilesName,
        Dictionary<string, LibraryMod> library,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        ArgumentNullException.ThrowIfNull(library);
        try
        {
            return await AdoptAsync(documents, candidate, looseFilesName, library, cancellationToken);
        }

        // STANDARDS-EXCEPTION: one mod is the boundary of a batch. The failure is logged and named, never lost.
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogError(exception, "Unexpected error adopting {Candidate}", candidate.Id);
            return Result.Failure<AdoptedMod>(new Error(DiagnosticCodes.ModImportFailed, $"Something unexpected went wrong while copying {candidate.Name}."));
        }
    }

    private async Task<Result<AdoptedMod>> AdoptAsync(string documents, AdoptionCandidate candidate, string? looseFilesName, Dictionary<string, LibraryMod> library, CancellationToken cancellationToken)
    {
        if (candidate is { Kind: AdoptionCandidateKind.VortexMod, DeployedByVortex: true })
        {
            return Result.Failure<AdoptedMod>(new Error(
                DiagnosticCodes.ModAdoptVortexManages,
                $"Vortex still manages {candidate.Name}, and would put its files back if they were moved.",
                "Leave it in Vortex for now: taking a mod over from Vortex comes with the handover (ADR-0028)."));
        }

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
