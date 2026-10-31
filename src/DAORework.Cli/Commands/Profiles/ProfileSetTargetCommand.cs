using DAORework.Application.Profiles;
using DAORework.Cli.Composition;
using DAORework.Cli.Rendering;
using Spectre.Console;
using Spectre.Console.Cli;

namespace DAORework.Cli.Commands.Profiles;

public sealed class ProfileSetTargetCommand : AsyncCommand<SetTargetSettings>
{
    private readonly UpdateLaunchTarget _updateLaunchTarget;
    private readonly ProfileRenderer _profiles;
    private readonly ErrorRenderer _errors;
    private readonly IAnsiConsole _console;

    public ProfileSetTargetCommand(UpdateLaunchTarget updateLaunchTarget, ProfileRenderer profiles, ErrorRenderer errors, IAnsiConsole console)
    {
        _updateLaunchTarget = updateLaunchTarget;
        _profiles = profiles;
        _errors = errors;
        _console = console;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, SetTargetSettings settings, CancellationToken cancellationToken)
    {
        var request = new UpdateLaunchTargetRequest(settings.Profile, settings.ParsedTarget);
        if (!(await _updateLaunchTarget.ExecuteAsync(request, cancellationToken)).TryGetValue(out var profile, out var error))
        {
            _errors.Render(error);
            return ExitCodes.Failure;
        }

        _console.WriteLine($"Updated profile '{profile.Name}' launch target to '{profile.LaunchTarget}'.");
        _profiles.Render(profile);
        return ExitCodes.Success;
    }
}
