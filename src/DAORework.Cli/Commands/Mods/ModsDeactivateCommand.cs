using DAORework.Application.Mods.Apply;
using DAORework.Application.Mods.Changes;
using DAORework.Application.Profiles;
using DAORework.Cli.Rendering;
using DAORework.Core.Mods.Planning;

namespace DAORework.Cli.Commands.Mods;

public sealed class ModsDeactivateCommand(GetProfile getProfile, PlanModChange plan, ApplyModPlan apply, ModChangeRenderer renderer, ErrorRenderer errors)
    : ModChangeCommand(getProfile, plan, apply, renderer, errors)
{
    protected override ModChangeKind Kind => ModChangeKind.Deactivate;
}
