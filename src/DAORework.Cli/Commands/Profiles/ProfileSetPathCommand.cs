using DAORework.Application.Profiles;
using DAORework.Cli.Composition;
using DAORework.Cli.Rendering;
using Spectre.Console;
using Spectre.Console.Cli;

namespace DAORework.Cli.Commands.Profiles;

public sealed class ProfileSetPathCommand : AsyncCommand<SetPathSettings>
{
    private readonly UpdateProfilePath _updateProfilePath;
    private readonly ProfileRenderer _profiles;
    private readonly ErrorRenderer _errors;
    private readonly IAnsiConsole _console;

    public ProfileSetPathCommand(UpdateProfilePath updateProfilePath, ProfileRenderer profiles, ErrorRenderer errors, IAnsiConsole console)
    {
        _updateProfilePath = updateProfilePath;
        _profiles = profiles;
        _errors = errors;
        _console = console;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, SetPathSettings settings, CancellationToken cancellationToken)
    {
        var request = new UpdateProfilePathRequest(settings.Profile, settings.ParsedKind, settings.Path);
        if (!(await _updateProfilePath.ExecuteAsync(request, cancellationToken)).TryGetValue(out var profile, out var error))
        {
            _errors.Render(error);
            return ExitCodes.Failure;
        }

        _console.WriteLine($"Updated profile '{profile.Name}' {settings.Kind.ToLowerInvariant()} path.");
        _profiles.Render(profile);
        return ExitCodes.Success;
    }
}
