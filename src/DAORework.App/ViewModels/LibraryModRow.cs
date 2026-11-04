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

    /// <summary>Gets a value indicating whether the mod is an add-in (a <c>.dazip</c>), rather than an override mod (ADR-0026).</summary>
    public bool IsAddIn { get; private init; }

    public string Details { get; }

    public IReadOnlyList<string> Notes { get; }

    public string? Warning { get; }

    /// <summary>Gets the page of the known item this mod's download is, or is a copy of (ADR-0034 §7).</summary>
    public string? PageLink { get; private init; }

    [ObservableProperty]
    public partial bool IsActive { get; set; }

    /// <summary>Gets or sets a value indicating whether it is ticked for <i>Activate selected</i> or <i>Deactivate selected</i> (P2.5-14).</summary>
    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    /// <param name="entry">The library mod.</param>
    /// <param name="check">How its download compares with the catalog's known downloads, or the error that kept it from being compared.</param>
    public static LibraryModRow From(LibraryEntry entry, Result<DownloadCheck> check)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentNullException.ThrowIfNull(check);
        var imported = entry.ImportedAtUtc.ToLocalTime().ToString("d MMM yyyy", CultureInfo.InvariantCulture);
        var notes = new List<string> { LibraryEntryNotes.Scan(entry) };
        if (LibraryEntryNotes.Programs(entry) is { } programs)
        {
            notes.Add(programs);
        }

        if (entry.Mod.Kind == ModKind.GameProgram)
        {
            notes.Add("DAORework can't install this kind of mod yet, so turning it on changes nothing.");
        }

        if (entry.Origin.Kind == ModOriginKind.Adopted)
        {
            notes.Add($"Was in {entry.Origin.AdoptedPath} before DAORework managed it.");
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
            $"{KindOf(entry.Mod)} · {Counted.Of(entry.Mod.Files.Count, "file")} · {new ByteSize(entry.SizeBytes)} · added {imported} from {entry.SourceName}",
            notes,
            warning)
        {
            IsAddIn = entry.Mod.AddInUid is not null,
            PageLink = check.IsSuccess ? check.Value.Item?.Source : null,
        };
    }

    private static string KindOf(LibraryMod mod) => mod.Kind switch
    {
        ModKind.GameProgram => "runs in the game",
        ModKind.AddIn => $"add-in {mod.AddInUid}",
        _ => "override mod",
    };
}
