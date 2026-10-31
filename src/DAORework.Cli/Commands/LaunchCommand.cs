using DAORework.Application.Launching;
using DAORework.Cli.Commands.Profiles;
using DAORework.Cli.Composition;
using DAORework.Cli.Rendering;
using Spectre.Console;
using Spectre.Console.Cli;

namespace DAORework.Cli.Commands;

public sealed class LaunchCommand : AsyncCommand<ProfileNameSettings>
{
    private readonly LaunchGame _launchGame;
    private readonly ErrorRenderer _errors;
    private readonly IAnsiConsole _console;

    public LaunchCommand(LaunchGame launchGame, ErrorRenderer errors, IAnsiConsole console)
    {
        _launchGame = launchGame;
        _errors = errors;
        _console = console;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, ProfileNameSettings settings, CancellationToken cancellationToken)
    {
        if (!(await _launchGame.ExecuteAsync(new LaunchGameRequest(settings.Profile), cancellationToken)).TryGetValue(out var launched, out var error))
        {
            _errors.Render(error, "Launch failed");
            return ExitCodes.Failure;
        }

        _console.WriteLine($"Launch succeeded. ProcessId: {launched.ProcessId}. Executable: {launched.ExecutablePath}");
        return ExitCodes.Success;
    }
}
