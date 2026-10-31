using DAORework.Application.Support;
using DAORework.Cli.Commands.Profiles;
using DAORework.Cli.Composition;
using DAORework.Cli.Rendering;
using DAORework.Core.Support;
using Spectre.Console;
using Spectre.Console.Cli;

namespace DAORework.Cli.Commands;

public sealed class SupportBundleCommand : AsyncCommand<ProfileNameSettings>
{
    private readonly ExportSupportBundle _exportSupportBundle;
    private readonly ErrorRenderer _errors;
    private readonly IAnsiConsole _console;

    public SupportBundleCommand(ExportSupportBundle exportSupportBundle, ErrorRenderer errors, IAnsiConsole console)
    {
        _exportSupportBundle = exportSupportBundle;
        _errors = errors;
        _console = console;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, ProfileNameSettings settings, CancellationToken cancellationToken)
    {
        if (!(await _exportSupportBundle.ExecuteAsync(new ExportSupportBundleRequest(settings.Profile), cancellationToken)).TryGetValue(out var path, out var error))
        {
            _errors.Render(error);
            return ExitCodes.Failure;
        }

        _console.WriteLine($"Support bundle written to: {path}");
        _console.WriteLine($"It holds the profile, a fresh diagnostic run, the mod inventory and the last week's logs, with your user name replaced by {PathRedactor.Placeholder}.");
        _console.WriteLine("Open it and check the contents before sharing it.");
        return ExitCodes.Success;
    }
}
