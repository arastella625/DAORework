using DAORework.Core.Profiles;
using Spectre.Console;

namespace DAORework.Cli.Rendering;

public sealed class ProfileRenderer
{
    private readonly IAnsiConsole _console;

    public ProfileRenderer(IAnsiConsole console)
    {
        _console = console;
    }

    public void Render(RuntimeProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        _console.WriteLine();
        _console.WriteLine($"Profile:           {profile.Name}");
        _console.WriteLine($"Runtime Mode:      {profile.RuntimeMode}");
        _console.WriteLine($"Launch Target:     {profile.LaunchTarget}");
        _console.WriteLine($"Install Directory: {profile.InstallDirectory ?? "Not set"}");
        _console.WriteLine($"Game Executable:   {profile.GameExecutablePath ?? "Not set"}");
        _console.WriteLine($"Launcher:          {profile.LauncherExecutablePath ?? "Not set"}");
        _console.WriteLine($"Documents:         {profile.UserDocumentsDirectory ?? "Not set"}");
        _console.WriteLine($"Mod Profile:       {profile.ActiveModProfileName ?? "None"}");
        _console.WriteLine();
    }

    public void RenderNames(IReadOnlyList<string> names)
    {
        ArgumentNullException.ThrowIfNull(names);
        if (names.Count == 0)
        {
            _console.WriteLine("No profiles found. Run: init");
            return;
        }

        foreach (var name in names)
        {
            _console.WriteLine(name);
        }
    }
}
