using Spectre.Console;

namespace DAORework.Cli.Composition;

/// <summary>
/// The process console. Interactive output wraps at the terminal's width. Redirected output (scripts, CI, the
/// owner's verify script) is written unwrapped: Spectre would otherwise fold long lines, splitting file paths
/// across lines where no script can read them back.
/// </summary>
public static class CliConsole
{
    /// <summary>Effectively "never wrap": wider than any path or message the CLI prints.</summary>
    public const int RedirectedWidth = 4096;

    /// <summary>The floor for a real terminal that reports an implausibly small width.</summary>
    public const int MinimumWidth = 80;

    public static IAnsiConsole Create() => Create(Console.Out, Console.IsOutputRedirected);

    public static IAnsiConsole Create(TextWriter output, bool outputRedirected)
    {
        var console = AnsiConsole.Create(new AnsiConsoleSettings
        {
            Ansi = outputRedirected ? AnsiSupport.No : AnsiSupport.Detect,
            ColorSystem = outputRedirected ? ColorSystemSupport.NoColors : ColorSystemSupport.Detect,
            Out = new AnsiConsoleOutput(output),
        });

        if (outputRedirected)
        {
            console.Profile.Width = RedirectedWidth;
        }
        else if (console.Profile.Width < MinimumWidth)
        {
            console.Profile.Width = MinimumWidth;
        }

        return console;
    }
}
