using DAORework.Core.Mods;
using Spectre.Console;

namespace DAORework.Cli.Rendering;

public sealed class ModInventoryRenderer
{
    private const int DuplicatesShown = 20;

    private readonly IAnsiConsole _console;

    public ModInventoryRenderer(IAnsiConsole console)
    {
        _console = console;
    }

    public void Render(ModInventory inventory, string reportPath)
    {
        ArgumentNullException.ThrowIfNull(inventory);
        _console.WriteLine();
        _console.WriteLine($"DAO Mod Inventory: {inventory.ProfileName}");
        _console.WriteLine($"Documents Directory: {inventory.UserDocumentsDirectory}");
        _console.WriteLine($"Includes Hashes:     {(inventory.IncludesHashes ? "Yes" : "No")}");
        _console.WriteLine($"Total Files:         {inventory.TotalFiles}");
        _console.WriteLine($"Total Size:          {inventory.TotalSizeDisplay}");
        _console.WriteLine($"Unreadable Files:    {inventory.UnreadableFileCount}");
        _console.WriteLine($"Duplicate Names:     {inventory.DuplicateFileNames.Count}");
        _console.WriteLine($"Duplicate Paths:     {inventory.DuplicateRelativePaths.Count}");
        _console.WriteLine();
        _console.WriteLine($"Inventory written to: {reportPath}");

        if (inventory.DuplicateFileNames.Count == 0)
        {
            return;
        }

        _console.WriteLine();
        _console.WriteLine("First duplicate file names:");
        foreach (var duplicate in inventory.DuplicateFileNames.Take(DuplicatesShown))
        {
            _console.WriteLine($"  {duplicate}");
        }

        if (inventory.DuplicateFileNames.Count > DuplicatesShown)
        {
            _console.WriteLine($"  ...and {inventory.DuplicateFileNames.Count - DuplicatesShown} more");
        }
    }
}
