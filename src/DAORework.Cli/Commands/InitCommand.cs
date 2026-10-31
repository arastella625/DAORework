using DAORework.Application.Profiles;
using DAORework.Cli.Commands.Profiles;
using DAORework.Cli.Composition;
using DAORework.Cli.Rendering;
using Spectre.Console;
using Spectre.Console.Cli;

namespace DAORework.Cli.Commands;

public sealed class InitCommand : AsyncCommand<ProfileNameSettings>
{
    private readonly CreateProfile _createProfile;
    private readonly ProfileRenderer _profiles;
    private readonly ErrorRenderer _errors;
    private readonly IAnsiConsole _console;

    public InitCommand(CreateProfile createProfile, ProfileRenderer profiles, ErrorRenderer errors, IAnsiConsole console)
    {
        _createProfile = createProfile;
        _profiles = profiles;
        _errors = errors;
        _console = console;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, ProfileNameSettings settings, CancellationToken cancellationToken)
    {
        if (!(await _createProfile.ExecuteAsync(new CreateProfileRequest(settings.Profile), cancellationToken)).TryGetValue(out var profile, out var error))
        {
            _errors.Render(error, "Invalid profile name");
            return ExitCodes.Failure;
        }

        _console.WriteLine($"Saved profile '{profile.Name}'.");
        _profiles.Render(profile);
        return ExitCodes.Success;
    }
}
