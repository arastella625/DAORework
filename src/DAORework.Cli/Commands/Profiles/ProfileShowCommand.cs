using DAORework.Application.Profiles;
using DAORework.Cli.Composition;
using DAORework.Cli.Rendering;
using Spectre.Console.Cli;

namespace DAORework.Cli.Commands.Profiles;

public sealed class ProfileShowCommand : AsyncCommand<ProfileNameSettings>
{
    private readonly GetProfile _getProfile;
    private readonly ProfileRenderer _profiles;
    private readonly ErrorRenderer _errors;

    public ProfileShowCommand(GetProfile getProfile, ProfileRenderer profiles, ErrorRenderer errors)
    {
        _getProfile = getProfile;
        _profiles = profiles;
        _errors = errors;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, ProfileNameSettings settings, CancellationToken cancellationToken)
    {
        if (!(await _getProfile.ExecuteAsync(new GetProfileRequest(settings.Profile), cancellationToken)).TryGetValue(out var profile, out var error))
        {
            _errors.Render(error);
            return ExitCodes.Failure;
        }

        _profiles.Render(profile);
        return ExitCodes.Success;
    }
}
