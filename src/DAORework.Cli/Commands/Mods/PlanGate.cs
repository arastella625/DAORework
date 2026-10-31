using DAORework.Cli.Composition;
using DAORework.Cli.Rendering;
using DAORework.Core.Mods.Planning;

namespace DAORework.Cli.Commands.Mods;

/// <summary>Whether a shown plan is applied: never when blocked or empty, not on a dry run, and only once confirmed (or --yes).</summary>
internal static class PlanGate
{
    /// <returns>The exit code when the plan is not to be applied; null to apply it.</returns>
    public static int? StopBeforeApplying(ModActivationPlan plan, bool dryRun, bool yes, ModChangeRenderer renderer)
    {
        if (plan.IsBlocked)
        {
            return ExitCodes.Failure;
        }

        if (plan.IsEmpty)
        {
            return ExitCodes.Success;
        }

        if (dryRun)
        {
            renderer.Say("Dry run: nothing was changed.");
            return ExitCodes.Success;
        }

        return renderer.Confirm(yes, "Apply this change?") ? null : ExitCodes.Failure;
    }
}
