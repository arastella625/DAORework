using DAORework.Application.Profiles;
using DAORework.Cli.Rendering;
using DAORework.Core.Mods.Planning;

namespace DAORework.Cli.Commands.Mods;

public sealed class ModsDeactivateCommand(GetProfile getProfile, ModChangeSteps steps, ModChangeRenderer renderer, ErrorRenderer errors, ProgramModChange programs)
    : ModChangeCommand<ModChangeSettings>(getProfile, steps, renderer, errors, programs)
{
    protected override ModChangeKind Kind => ModChangeKind.Deactivate;
}
