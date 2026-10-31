using DAORework.Application.Fixes;
using DAORework.Application.Profiles;
using DAORework.Cli.Rendering;
using DAORework.Core.Fixes;

namespace DAORework.Cli.Commands.Fixes;

public sealed class FixesRemoveCommand(GetProfile getProfile, PlanFix plan, ApplyFix apply, FixRenderer renderer, ErrorRenderer errors)
    : FixChangeCommand(getProfile, plan, apply, renderer, errors)
{
    protected override FixChangeKind Kind => FixChangeKind.Remove;
}
