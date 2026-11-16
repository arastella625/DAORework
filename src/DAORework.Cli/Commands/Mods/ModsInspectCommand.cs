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
    private readonly InspectSteps _steps;
    private readonly ModInspectionRenderer _renderer;
    private readonly AddInsRenderer _addIns;
    private readonly ErrorRenderer _errors;

    public ModsInspectCommand(GetProfile getProfile, InspectSteps steps, ModInspectionRenderer renderer, AddInsRenderer addIns, ErrorRenderer errors)
    {
        _getProfile = getProfile;
        _steps = steps;
        _renderer = renderer;
        _addIns = addIns;
        _errors = errors;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, ProfileNameSettings settings, CancellationToken cancellationToken)
    {
        if (!(await _getProfile.ExecuteAsync(new GetProfileRequest(settings.Profile), cancellationToken)).TryGetValue(out var profile, out var error))
        {
            _errors.Render(error);
            return ExitCodes.Failure;
        }

        if (!(await _steps.Folders.ExecuteAsync(new InspectModFoldersRequest(profile), cancellationToken)).TryGetValue(out var report, out var inspectionError))
        {
            _errors.Render(inspectionError, "Inspection failed");
            return ExitCodes.Failure;
        }

        _renderer.Render(report);

        // The add-ins are part of the summary: when they can't be read it is said there, and the folders above still stand.
        if ((await _steps.AddIns.ExecuteAsync(report.UserDocumentsDirectory, cancellationToken)).TryGetValue(out var addIns, out var addInsError))
        {
            _addIns.Render(addIns);
        }
        else
        {
            _addIns.RenderProblem(addInsError);
        }

        return ExitCodes.Success;
    }
}
