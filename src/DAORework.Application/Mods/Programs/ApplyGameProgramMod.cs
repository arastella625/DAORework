using DAORework.Application.Fixes;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Fixes;
using DAORework.Core.Mods.Safety;

namespace DAORework.Application.Mods.Programs;

/// <summary>
/// Applies a plan for a mod that runs inside the game (ADR-0033 §4) through the same backup, journal and executor as a
/// fix. Turning one on puts a program in the game folder, so it needs the player's yes first (§3); turning it off doesn't.
/// </summary>
public sealed class ApplyGameProgramMod
{
    private readonly ApplyFix _apply;

    public ApplyGameProgramMod(ApplyFix apply)
    {
        _apply = apply;
    }

    public async Task<Result<FixApplyOutcome>> ExecuteAsync(ApplyGameProgramModRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var planned = request.Planned;
        if (planned.Plan.Kind == FixChangeKind.Apply && planned.Plan.Change.Operations.Count > 0 && !request.ProgramAccepted)
        {
            return Result.Failure<FixApplyOutcome>(new Error(
                DiagnosticCodes.ModProgramNotAccepted,
                $"{planned.ModName} runs its own program inside the game, so it isn't turned on until you say you trust it.",
                $"Tick \"{GameProgramTrust.Question}\" in its preview, or name it with --allow-programs on the command line."));
        }

        return await _apply.ExecuteAsync(new ApplyFixRequest(request.InstallDirectory, new PlannedFix(planned.ModName, planned.Plan, Prepared: null)), cancellationToken);
    }
}
