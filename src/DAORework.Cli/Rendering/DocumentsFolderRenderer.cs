using DAORework.Application.Documents;
using DAORework.Core.Documents;
using Spectre.Console;

namespace DAORework.Cli.Rendering;

public sealed class DocumentsFolderRenderer
{
    private readonly IAnsiConsole _console;

    public DocumentsFolderRenderer(IAnsiConsole console)
    {
        _console = console;
    }

    public void Render(DocumentsDetection detection)
    {
        ArgumentNullException.ThrowIfNull(detection);
        _console.WriteLine();
        _console.WriteLine("Documents folders:");
        foreach (var folder in detection.Candidates)
        {
            _console.WriteLine($"  [{folder.Source}] {folder.Path}  ({Describe(folder)})");
        }

        if (detection.RecommendedDiffersFromKnownFolder)
        {
            _console.WriteLine("  The game currently uses the KnownFolder path, but your saves and mods are in the folder listed first.");
        }
    }

    private static string Describe(DocumentsFolder folder)
    {
        var state = folder.Exists ? "exists" : "not found";
        return folder.IsOneDrive ? $"{state}, synced by OneDrive" : state;
    }
}
