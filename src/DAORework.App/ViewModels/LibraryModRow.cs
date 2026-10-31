using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using DAORework.Core.Common;
using DAORework.Core.Mods.Library;
using DAORework.Core.Mods.Safety;

namespace DAORework.App.ViewModels;

/// <summary>
/// One mod in the Mods tab's <i>Library</i> list (P3-04): its name, a details line, the safety notes worded as the
/// CLI words them (ADR-0018), a warning when its download differs from every known one, and whether it is active in
/// the selected profile's documents folder.
/// </summary>
public sealed partial class LibraryModRow : ObservableObject
{
    private LibraryModRow(string id, string title, string details, IReadOnlyList<string> notes, string? warning)
    {
        Id = id;
        Title = title;
        Details = details;
        Notes = notes;
        Warning = warning;
    }

    public string Id { get; }

    public string Title { get; }

    public string Details { get; }

    public IReadOnlyList<string> Notes { get; }

    public string? Warning { get; }

    [ObservableProperty]
    public partial bool IsActive { get; set; }

    /// <param name="entry">The library mod.</param>
    /// <param name="check">How its download compares with the catalog's known downloads, or the error that kept it from being compared.</param>
    public static LibraryModRow From(LibraryEntry entry, Result<DownloadCheck> check)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentNullException.ThrowIfNull(check);
        var kind = entry.Mod.AddInUid is { } uid ? $"add-in {uid}" : "override mod";
        var imported = entry.ImportedAtUtc.ToLocalTime().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var notes = new List<string> { LibraryEntryNotes.Scan(entry) };
        if (LibraryEntryNotes.Programs(entry) is { } programs)
        {
            notes.Add(programs);
        }

        if (entry.Origin.Kind == ModOriginKind.Adopted)
        {
            notes.Add($"Adopted from {entry.Origin.AdoptedPath}.");
        }

        string? warning = null;
        if (check.TryGetValue(out var download, out var error))
        {
            warning = LibraryEntryNotes.DownloadWarning(entry, download) is { } issue ? $"{issue.Message} {issue.Recommendation} ({issue.Code})" : null;
            if (LibraryEntryNotes.Download(download) is { } note)
            {
                notes.Add(note);
            }
        }
        else
        {
            notes.Add($"It could not be compared with the known downloads: {error.Message}");
        }

        return new LibraryModRow(
            entry.Mod.Id,
            entry.Mod.Name,
            $"{entry.Mod.Id} · {kind} · {entry.Mod.Files.Count} file(s) · {new ByteSize(entry.SizeBytes)} · imported {imported} from {entry.SourceName}",
            notes,
            warning);
    }
}
