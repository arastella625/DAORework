using DAORework.Application.Abstractions;
using DAORework.Application.Fixes;
using DAORework.Application.Mods.Programs;
using DAORework.Cli.Composition;
using DAORework.Cli.Rendering;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Fixes;
using DAORework.Core.Mods.Library;
using DAORework.Core.Mods.Planning;
using DAORework.Core.Mods.Safety;

namespace DAORework.Cli.Commands.Mods;

/// <summary>
/// <c>mods activate</c> and <c>mods deactivate</c> for a mod that runs inside the game (ADR-0033 §3, §4). It goes into
/// the install folder on the fixes' record, so it is changed by itself, never with other mods. Turning one on needs
/// <c>--allow-programs</c> naming it, which <c>--yes</c> doesn't cover, so a script can't do it by accident. Kept apart
/// from the commands so they stay within five dependencies.
/// </summary>
public sealed class ProgramModChange
{
    private readonly IModLibrary _library;
    private readonly PlanGameProgramMod _plan;
    private readonly ApplyGameProgramMod _apply;
    private readonly FixRenderer _renderer;
    private readonly ErrorRenderer _errors;

    public ProgramModChange(IModLibrary library, PlanGameProgramMod plan, ApplyGameProgramMod apply, FixRenderer renderer, ErrorRenderer errors)
    {
        _library = library;
        _plan = plan;
        _apply = apply;
        _renderer = renderer;
        _errors = errors;
    }

    /// <returns>The exit code when the command named such a mod; null when it named none, or one that is on in the documents folder from before DAORework knew what it is.</returns>
    public async Task<int?> RunAsync(ProgramModChangeRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var ids = request.Settings.ModIds;
        var library = await _library.ListAsync(cancellationToken);
        var programs = library.Where(e => e.Mod.Kind == ModKind.GameProgram && ids.Contains(e.Mod.Id, StringComparer.OrdinalIgnoreCase)).Select(e => e.Mod).ToList();
        if (programs.Count == 0)
        {
            return null;
        }

        var verb = request.Kind == ModChangeKind.Activate ? "activate" : "deactivate";
        if (ids.Length > 1)
        {
            _renderer.Say($"{string.Join(", ", programs.Select(m => m.Name))} {(programs.Count == 1 ? "runs" : "run")} inside the game, so each is changed by itself. Nothing was changed.");
            _renderer.Say($"Run: mods {verb} {programs[0].Id}{(request.Kind == ModChangeKind.Activate ? $" --allow-programs {programs[0].Id}" : string.Empty)}");
            return ExitCodes.Failure;
        }

        if (string.IsNullOrWhiteSpace(request.Install))
        {
            var profile = request.Settings.Profile;
            _errors.Render(new Error(DiagnosticCodes.FixInstallNotSet, $"Profile '{profile}' has no install folder.", $"Run: profile set-path {profile} install <folder>"));
            return ExitCodes.Failure;
        }

        return await ChangeAsync(request, programs[0], cancellationToken);
    }

    private async Task<int?> ChangeAsync(ProgramModChangeRequest request, LibraryMod mod, CancellationToken cancellationToken)
    {
        var install = request.Install!;
        var kind = request.Kind == ModChangeKind.Activate ? FixChangeKind.Apply : FixChangeKind.Remove;
        if (!(await _plan.ExecuteAsync(new PlanGameProgramModRequest(install, mod.Id, kind), cancellationToken)).TryGetValue(out var planned, out var error))
        {
            // Not on in the install folder: it may be on in the documents folder, added before DAORework knew what it is.
            if (kind == FixChangeKind.Remove && error.Code == DiagnosticCodes.ModNotActive)
            {
                return null;
            }

            _errors.Render(error, "Cannot plan the change");
            return ExitCodes.Failure;
        }

        Show(planned);
        var accepted = request.Allowed.Contains(mod.Id, StringComparer.OrdinalIgnoreCase);
        if (StopBeforeApplying(planned, request.Settings, accepted) is { } exitCode)
        {
            return exitCode;
        }

        if (!(await _apply.ExecuteAsync(new ApplyGameProgramModRequest(install, planned, accepted), cancellationToken)).TryGetValue(out var outcome, out error))
        {
            _errors.Render(error, "The change did not complete");
            return ExitCodes.Failure;
        }

        _renderer.RenderApplied(outcome);
        return ExitCodes.Success;
    }

    /// <summary>
    /// The exit code when the plan is not to be applied: blocked, a dry run, or not confirmed. Without the player's yes it
    /// goes on to the use case, which refuses and says how to give it, so the prompt is never taken for the yes.
    /// </summary>
    private int? StopBeforeApplying(PlannedGameProgramMod planned, ModChangeSettings settings, bool accepted)
    {
        var change = planned.Plan.Change;
        if (change.IsBlocked)
        {
            return ExitCodes.Failure;
        }

        if (settings.DryRun)
        {
            _renderer.Say("Dry run: nothing was changed.");
            return ExitCodes.Success;
        }

        var asks = accepted || planned.Plan.Kind == FixChangeKind.Remove;
        return !asks || change.IsEmpty || _renderer.Confirm(settings.Yes, "Apply this change?") ? null : ExitCodes.Failure;
    }

    private void Show(PlannedGameProgramMod planned)
    {
        _renderer.RenderPlan(new PlannedFix(planned.ModName, planned.Plan, Prepared: null));
        foreach (var note in planned.Notes)
        {
            _renderer.Say($"Note: {note}");
        }

        if (planned.Plan.Kind == FixChangeKind.Apply && planned.Plan.Change.Operations.Count > 0)
        {
            _renderer.Say($"Warning: {GameProgramTrust.Warning}");
        }
    }
}
