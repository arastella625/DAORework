using DAORework.Core.Installations;
using Spectre.Console;

namespace DAORework.Cli.Rendering;

public sealed class InstallationRenderer
{
    private readonly IAnsiConsole _console;

    public InstallationRenderer(IAnsiConsole console)
    {
        _console = console;
    }

    public void Render(IReadOnlyList<Installation> installations)
    {
        ArgumentNullException.ThrowIfNull(installations);
        if (installations.Count == 0)
        {
            _console.WriteLine("No Dragon Age: Origins installation was detected automatically.");
            _console.WriteLine("You can still create a profile and set the paths with: profile set-path");
            return;
        }

        foreach (var install in installations)
        {
            _console.WriteLine($"[{install.Distribution}] {install.InstallDirectory}");
            _console.WriteLine($"  Game:     {install.GameExecutablePath ?? "Not found"}");
            _console.WriteLine($"  Launcher: {install.LauncherExecutablePath ?? "Not found"}");
            if (!string.IsNullOrWhiteSpace(install.Notes))
            {
                _console.WriteLine($"  Source:   {install.Notes}");
            }
        }
    }
}
