using DAORework.Core.Common;
using Spectre.Console;

namespace DAORework.Cli.Rendering;

public sealed class ErrorRenderer
{
    private readonly IAnsiConsole _console;

    public ErrorRenderer(IAnsiConsole console)
    {
        _console = console;
    }

    public void Render(Error error, string? prefix = null)
    {
        ArgumentNullException.ThrowIfNull(error);
        _console.WriteLine(prefix is null ? error.Message : $"{prefix}: {error.Message}");
        if (error.Recommendation is not null)
        {
            _console.WriteLine(error.Recommendation);
        }
    }
}
