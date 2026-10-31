using DAORework.Application.Mods.Apply;
using DAORework.Application.Mods.Changes;
using DAORework.Application.Profiles;
using DAORework.Cli.Composition;
using DAORework.Cli.Rendering;
using DAORework.Core.Mods.Planning;
using Spectre.Console.Cli;

namespace DAORework.Cli.Commands.Mods;

/// <summary>
/// Activates or deactivates mods (ROADMAP P2-06): the plan is always shown first, a blocked plan is never applied, and
/// nothing changes without confirmation (or --yes). The change is backed up and journaled (P2-04, P2-05).
/// </summary>
public abstract class ModChangeCommand : AsyncCommand<ModChangeSettings>
{
    private readonly GetProfile _getProfile;
    private readonly PlanModChange _plan;
    private readonly ApplyModPlan _apply;
    private readonly ModChangeRenderer _renderer;
    private readonly ErrorRenderer _errors;

    protected ModChangeCommand(GetProfile getProfile, PlanModChange plan, ApplyModPlan apply, ModChangeRenderer renderer, ErrorRenderer errors)
    {
        _getProfile = getProfile;
        _plan = plan;
        _apply = apply;
        _renderer = renderer;
        _errors = errors;
    }

    protected abstract ModChangeKind Kind { get; }

    public override async Task<int> ExecuteAsync(CommandContext context, ModChangeSettings settings, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (!(await ModsDocuments.ForProfileAsync(_getProfile, settings.Profile, cancellationToken)).TryGetValue(out var documents, out var error))
        {
            _errors.Render(error);
            return ExitCodes.Failure;
        }

        if (!(await _plan.ExecuteAsync(new PlanModChangeRequest(documents, new ModChange(Kind, settings.ModIds)), cancellationToken)).TryGetValue(out var plan, out error))
        {
            _errors.Render(error, "Cannot plan the change");
            return ExitCodes.Failure;
        }

        _renderer.RenderPlan(plan, settings.ListFiles);
        if (PlanGate.StopBeforeApplying(plan, settings.DryRun, settings.Yes, _renderer) is { } exitCode)
        {
            return exitCode;
        }

        if (!(await _apply.ExecuteAsync(new ApplyModPlanRequest(documents, plan), cancellationToken)).TryGetValue(out var report, out error))
        {
            _errors.Render(error, "The change did not complete");
            return ExitCodes.Failure;
        }

        _renderer.RenderApplied(report);
        return ExitCodes.Success;
    }
}
