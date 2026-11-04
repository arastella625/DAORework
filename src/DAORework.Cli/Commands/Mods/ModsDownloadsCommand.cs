using DAORework.Application.Mods.Downloads;
using DAORework.Cli.Composition;
using DAORework.Cli.Rendering;
using Spectre.Console;
using Spectre.Console.Cli;

namespace DAORework.Cli.Commands.Mods;

/// <summary><c>mods downloads</c> (ADR-0034 §8): the downloads that look like Dragon Age mods. Asking is consent, so it works with the window's setting off. Only reads.</summary>
public sealed class ModsDownloadsCommand : AsyncCommand<DownloadsSettings>
{
    private readonly FindNewDownloads _find;
    private readonly IAnsiConsole _console;
    private readonly ErrorRenderer _errors;

    public ModsDownloadsCommand(FindNewDownloads find, IAnsiConsole console, ErrorRenderer errors)
    {
        _find = find;
        _console = console;
        _errors = errors;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, DownloadsSettings settings, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (!(await _find.ExecuteAsync(new FindNewDownloadsRequest(settings.Folder, Asked: true), cancellationToken)).TryGetValue(out var downloads, out var error))
        {
            _errors.Render(error);
            return ExitCodes.Failure;
        }

        foreach (var download in downloads)
        {
            _console.WriteLine(download.File.Path);
        }

        _console.WriteLine(downloads.Count == 0 ? "No downloads here look like Dragon Age mods." : "To add one: mods import <path>");
        return ExitCodes.Success;
    }
}
