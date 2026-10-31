using DAORework.Application.Mods;
using DAORework.Application.Profiles;
using DAORework.Cli.Commands.Profiles;
using DAORework.Cli.Composition;
using DAORework.Cli.Rendering;
using Spectre.Console.Cli;

namespace DAORework.Cli.Commands.Mods;

public sealed class ModsInspectCommand : AsyncCommand<ProfileNameSettings>
{
    private readonly GetProfile _getProfile;
    private readonly InspectModFolders _inspectModFolders;
    private readonly ModInspectionRenderer _renderer;
    private readonly ErrorRenderer _errors;

    public ModsInspectCommand(GetProfile getProfile, InspectModFolders inspectModFolders, ModInspectionRenderer renderer, ErrorRenderer errors)
    {
        _getProfile = getProfile;
        _inspectModFolders = inspectModFolders;
        _renderer = renderer;
        _errors = errors;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, ProfileNameSettings settings, CancellationToken cancellationToken)
    {
        if (!(await _getProfile.ExecuteAsync(new GetProfileRequest(settings.Profile), cancellationToken)).TryGetValue(out var profile, out var error))
        {
            _errors.Render(error);
            return ExitCodes.Failure;
        }

        if (!(await _inspectModFolders.ExecuteAsync(new InspectModFoldersRequest(profile), cancellationToken)).TryGetValue(out var report, out var inspectionError))
        {
            _errors.Render(inspectionError, "Inspection failed");
            return ExitCodes.Failure;
        }

        _renderer.Render(report);
        return ExitCodes.Success;
    }
}
