using DAORework.Application.Fixes;
using DAORework.Application.Profiles;
using DAORework.Cli.Composition;
using DAORework.Cli.Rendering;
using DAORework.Core.Fixes;
using Spectre.Console.Cli;

namespace DAORework.Cli.Commands.Fixes;

/// <summary>
/// Applies or removes a managed fix (ADR-0013): the plan is always shown first, a blocked plan is never applied, and
/// nothing changes without confirmation (or --yes). The change is backed up and journaled like a mod change.
/// </summary>
public abstract class FixChangeCommand : AsyncCommand<FixChangeSettings>
{
    private readonly GetProfile _getProfile;
    private readonly PlanFix _plan;
    private readonly ApplyFix _apply;
    private readonly FixRenderer _renderer;
    private readonly ErrorRenderer _errors;

    protected FixChangeCommand(GetProfile getProfile, PlanFix plan, ApplyFix apply, FixRenderer renderer, ErrorRenderer errors)
    {
        _getProfile = getProfile;
        _plan = plan;
        _apply = apply;
        _renderer = renderer;
        _errors = errors;
    }

    protected abstract FixChangeKind Kind { get; }

    public override async Task<int> ExecuteAsync(CommandContext context, FixChangeSettings settings, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (!(await FixesInstall.ForProfileAsync(_getProfile, settings.Profile, cancellationToken)).TryGetValue(out var install, out var error))
        {
            _errors.Render(error);
            return ExitCodes.Failure;
        }

        if (!(await _plan.ExecuteAsync(new PlanFixRequest(install, settings.Fix, Kind, settings.Source), cancellationToken)).TryGetValue(out var planned, out error))
        {
            _errors.Render(error, "Cannot plan the change");
            return ExitCodes.Failure;
        }

        _renderer.RenderPlan(planned);
        if (StopBeforeApplying(planned, settings) is { } exitCode)
        {
            return exitCode;
        }

        if (!(await _apply.ExecuteAsync(new ApplyFixRequest(install, planned), cancellationToken)).TryGetValue(out var outcome, out error))
        {
            _errors.Render(error, "The change did not complete");
            return ExitCodes.Failure;
        }

        _renderer.RenderApplied(outcome);
        return ExitCodes.Success;
    }

    /// <summary>The exit code when the plan is not to be applied: blocked, a dry run, or not confirmed. A plan that only updates the record goes ahead.</summary>
    private int? StopBeforeApplying(PlannedFix planned, FixChangeSettings settings)
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

        return change.IsEmpty || _renderer.Confirm(settings.Yes, "Apply this change?") ? null : ExitCodes.Failure;
    }
}
