using DAORework.Core.Mods;
using Spectre.Console;

namespace DAORework.Cli.Rendering;

public sealed class ModInspectionRenderer
{
    private readonly IAnsiConsole _console;

    public ModInspectionRenderer(IAnsiConsole console)
    {
        _console = console;
    }

    public void Render(ModInspectionReport report)
    {
        ArgumentNullException.ThrowIfNull(report);
        _console.WriteLine($"DAO Mod Inspection: {report.ProfileName}");
        _console.WriteLine();
        _console.WriteLine($"Documents Directory: {report.UserDocumentsDirectory}");
        _console.WriteLine();
        RenderFolder(report.OverrideFolder);
        _console.WriteLine();
        RenderFolder(report.AddInsFolder);
        _console.WriteLine();
    }

    private void RenderFolder(ModFolderSummary folder)
    {
        _console.WriteLine($"{folder.Name}:");
        _console.WriteLine($"  Path:    {folder.Path}");
        _console.WriteLine($"  Exists:  {(folder.Exists ? "Yes" : "No")}");
        _console.WriteLine($"  Files:   {folder.FileCount}");
        _console.WriteLine($"  Folders: {folder.FolderCount}");
        _console.WriteLine($"  Size:    {folder.SizeDisplay}");
        if (folder.UnreadableFileCount > 0)
        {
            _console.WriteLine($"  Unreadable: {folder.UnreadableFileCount} file(s) could not be read; the size excludes them.");
        }
    }
}
