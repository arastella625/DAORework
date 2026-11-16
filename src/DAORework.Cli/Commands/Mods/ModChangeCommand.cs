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
/// nothing changes without confirmation (or --yes). The change is backed up and journaled (P2-04, P2-05). A mod that
/// runs inside the game is changed by itself, in the install folder (ADR-0033 §3).
/// </summary>
public abstract class ModChangeCommand<TSettings> : AsyncCommand<TSettings>
    where TSettings : ModChangeSettings
{
    private readonly GetProfile _getProfile;
    private readonly ModChangeSteps _steps;
    private readonly ModChangeRenderer _renderer;
    private readonly ErrorRenderer _errors;
    private readonly ProgramModChange _programs;

    protected ModChangeCommand(GetProfile getProfile, ModChangeSteps steps, ModChangeRenderer renderer, ErrorRenderer errors, ProgramModChange programs)
    {
        _getProfile = getProfile;
        _steps = steps;
        _renderer = renderer;
        _errors = errors;
        _programs = programs;
    }

    protected abstract ModChangeKind Kind { get; }

    public override async Task<int> ExecuteAsync(CommandContext context, TSettings settings, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (!(await ModsDocuments.FoldersAsync(_getProfile, settings.Profile, cancellationToken)).TryGetValue(out var folders, out var error))
        {
            _errors.Render(error);
            return ExitCodes.Failure;
        }

        if (await _programs.RunAsync(new ProgramModChangeRequest(Kind, folders.Install, settings, AllowedPrograms(settings)), cancellationToken) is { } handled)
        {
            return handled;
        }

        var documents = folders.Documents;
        if (!(await _steps.Plan.ExecuteAsync(new PlanModChangeRequest(documents, new ModChange(Kind, settings.ModIds), folders.Install), cancellationToken)).TryGetValue(out var plan, out error))
        {
            _errors.Render(error, "Cannot plan the change");
            return ExitCodes.Failure;
        }

        _renderer.RenderPlan(plan, settings.ListFiles);
        if (PlanGate.StopBeforeApplying(plan, settings.DryRun, settings.Yes, _renderer) is { } exitCode)
        {
            return exitCode;
        }

        if (!(await _steps.Apply.ExecuteAsync(new ApplyModPlanRequest(documents, plan), cancellationToken)).TryGetValue(out var report, out error))
        {
            _errors.Render(error, "The change did not complete");
            return ExitCodes.Failure;
        }

        _renderer.RenderApplied(report);
        return ExitCodes.Success;
    }

    /// <summary>The mods that run inside the game the player said yes to by name; only <c>mods activate</c> asks.</summary>
    protected virtual IReadOnlyCollection<string> AllowedPrograms(TSettings settings) => [];
}
