using DAORework.Application.Mods.Adoption;
using DAORework.Application.Mods.Apply;
using DAORework.Cli.Composition;
using DAORework.Cli.Rendering;
using DAORework.Core.Mods.Adoption;

namespace DAORework.Cli.Commands.Mods;

/// <summary>The second half of <c>mods adopt</c>: plan the chosen candidates, show the plan, and apply it once confirmed.</summary>
public sealed class AdoptionApplier
{
    private readonly PlanAdoption _plan;
    private readonly ApplyModPlan _apply;
    private readonly ModChangeRenderer _renderer;
    private readonly ErrorRenderer _errors;

    public AdoptionApplier(PlanAdoption plan, ApplyModPlan apply, ModChangeRenderer renderer, ErrorRenderer errors)
    {
        _plan = plan;
        _apply = apply;
        _renderer = renderer;
        _errors = errors;
    }

    public async Task<int> RunAsync(string documents, IReadOnlyList<AdoptionCandidate> candidates, AdoptSettings settings, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (!(await _plan.ExecuteAsync(new PlanAdoptionRequest(documents, candidates, settings.Name), cancellationToken)).TryGetValue(out var plan, out var error))
        {
            _errors.Render(error, "Cannot plan the adoption");
            return ExitCodes.Failure;
        }

        _renderer.RenderPlan(plan, settings.ListFiles);
        if (PlanGate.StopBeforeApplying(plan, settings.DryRun, settings.Yes, _renderer) is { } exitCode)
        {
            return exitCode;
        }

        if (!(await _apply.ExecuteAsync(new ApplyModPlanRequest(documents, plan), cancellationToken)).TryGetValue(out var report, out error))
        {
            _errors.Render(error, "The adoption did not complete");
            return ExitCodes.Failure;
        }

        _renderer.RenderApplied(report);
        return ExitCodes.Success;
    }
}
