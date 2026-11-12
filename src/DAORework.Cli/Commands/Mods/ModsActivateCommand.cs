using DAORework.Application.Profiles;
using DAORework.Cli.Rendering;
using DAORework.Core.Mods.Planning;

namespace DAORework.Cli.Commands.Mods;

public sealed class ModsActivateCommand(GetProfile getProfile, ModChangeSteps steps, ModChangeRenderer renderer, ErrorRenderer errors, ProgramModChange programs)
    : ModChangeCommand<ModActivateSettings>(getProfile, steps, renderer, errors, programs)
{
    protected override ModChangeKind Kind => ModChangeKind.Activate;

    protected override IReadOnlyCollection<string> AllowedPrograms(ModActivateSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return settings.AllowPrograms;
    }
}
