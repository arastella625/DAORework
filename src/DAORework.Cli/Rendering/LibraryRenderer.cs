using System.Globalization;
using DAORework.Core.Common;
using DAORework.Core.Mods.Library;
using DAORework.Core.Mods.Safety;
using Spectre.Console;

namespace DAORework.Cli.Rendering;

public sealed class LibraryRenderer
{
    private readonly IAnsiConsole _console;

    public LibraryRenderer(IAnsiConsole console)
    {
        _console = console;
    }

    public void RenderImported(LibraryImport import)
    {
        ArgumentNullException.ThrowIfNull(import);
        var entry = import.Entry;
        var verb = import.AlreadyInLibrary ? "Already in the library" : "Imported";
        _console.WriteLine($"{verb}: {entry.Mod.Name} ({Describe(entry.Mod)}) as {entry.Mod.Id}, {entry.Mod.Files.Count} file(s), {new ByteSize(entry.SizeBytes)}.");
        if (!import.AlreadyInLibrary)
        {
            _console.WriteLine("Nothing was installed; the game folders are unchanged.");
        }

        _console.WriteLine(LibraryEntryNotes.Scan(entry));
        if (LibraryEntryNotes.Programs(entry) is { } programs)
        {
            _console.WriteLine(programs);
        }
    }

    public void RenderDownloadCheck(LibraryEntry entry, DownloadCheck check)
    {
        var line = LibraryEntryNotes.DownloadWarning(entry, check) is { } warning
            ? $"[Warning] {warning.Code}: {warning.Message} {warning.Recommendation}"
            : LibraryEntryNotes.Download(check);
        if (line is not null)
        {
            _console.WriteLine(line);
        }
    }

    public void RenderList(IReadOnlyList<LibraryEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        if (entries.Count == 0)
        {
            _console.WriteLine("The mod library is empty. Run: mods import <path to a .dazip, .zip or folder>");
            return;
        }

        foreach (var entry in entries)
        {
            var imported = entry.ImportedAtUtc.ToLocalTime().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            var programs = entry.Mod.Files.Count(ProgramFiles.IsProgram);
            var flag = programs == 0 ? string.Empty : $", {programs} program file(s) never installed";
            _console.WriteLine($"{entry.Mod.Id}  {entry.Mod.Name}  ({Describe(entry.Mod)}, {entry.Mod.Files.Count} file(s), {new ByteSize(entry.SizeBytes)}{flag}, imported {imported} from {entry.SourceName})");
        }
    }

    private static string Describe(LibraryMod mod) => mod.Kind == ModKind.AddIn ? $"add-in {mod.AddInUid}" : "override mod";
}
