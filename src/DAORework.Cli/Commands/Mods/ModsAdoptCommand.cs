using DAORework.Application.Mods.Adoption;
using DAORework.Application.Profiles;
using DAORework.Cli.Composition;
using DAORework.Cli.Rendering;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Mods.Adoption;
using Spectre.Console.Cli;

namespace DAORework.Cli.Commands.Mods;

/// <summary>
/// <c>mods adopt</c> (ROADMAP P2.5-02, P2.5-03, ADR-0015): lists the hand-installed mods DAORework could take over, or
/// adopts the ones named. The ids are checked against a fresh scan, so only a current candidate is ever adopted.
/// </summary>
public sealed class ModsAdoptCommand : AsyncCommand<AdoptSettings>
{
    private readonly GetProfile _getProfile;
    private readonly ScanForAdoption _scan;
    private readonly AdoptionRenderer _renderer;
    private readonly AdoptionApplier _applier;
    private readonly ErrorRenderer _errors;

    public ModsAdoptCommand(GetProfile getProfile, ScanForAdoption scan, AdoptionRenderer renderer, AdoptionApplier applier, ErrorRenderer errors)
    {
        _getProfile = getProfile;
        _scan = scan;
        _renderer = renderer;
        _applier = applier;
        _errors = errors;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, AdoptSettings settings, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (!(await ModsDocuments.ForProfileAsync(_getProfile, settings.Profile, cancellationToken)).TryGetValue(out var documents, out var error)
            || !(await _scan.ExecuteAsync(documents, cancellationToken)).TryGetValue(out var scan, out error))
        {
            _errors.Render(error);
            return ExitCodes.Failure;
        }

        if (settings.Candidates.Length == 0)
        {
            _renderer.RenderScan(scan, settings.ListFiles);
            return ExitCodes.Success;
        }

        if (!Chosen(scan, settings.Candidates).TryGetValue(out var candidates, out error))
        {
            _errors.Render(error);
            return ExitCodes.Failure;
        }

        return await _applier.RunAsync(documents, candidates, settings, cancellationToken);
    }

    /// <summary>The named candidates in the order given; an id that is not one fails, with the reason when the scan has one.</summary>
    private static Result<IReadOnlyList<AdoptionCandidate>> Chosen(AdoptionScan scan, IReadOnlyList<string> ids)
    {
        var chosen = new List<AdoptionCandidate>();
        foreach (var id in ids.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (scan.Candidates.FirstOrDefault(c => c.Id.Equals(id, StringComparison.OrdinalIgnoreCase)) is { } candidate)
            {
                chosen.Add(candidate);
                continue;
            }

            var reason = scan.Exclusions.FirstOrDefault(e => e.Id.Equals(id, StringComparison.OrdinalIgnoreCase)) is { } excluded
                ? $"{id} cannot be adopted: {AdoptionRenderer.Reason(excluded.Reason)}."
                : $"{id} is not something installed by hand in this documents folder.";
            return Result.Failure<IReadOnlyList<AdoptionCandidate>>(new Error(DiagnosticCodes.ModAdoptNotACandidate, reason, "Run mods adopt --dry-run for the ids."));
        }

        return Result.Success<IReadOnlyList<AdoptionCandidate>>(chosen);
    }
}
