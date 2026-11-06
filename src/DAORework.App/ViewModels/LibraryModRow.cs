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

    /// <summary>Gets a value indicating whether every game file changes only how things look, so it can't break a quest (ADR-0042).</summary>
    public bool OnlyLooks { get; private init; }

    /// <summary>Gets the bigger mod this one is inside, when it isn't simply a copy of it (ADR-0041).</summary>
    public ModInside? Inside { get; private init; }

    /// <summary>Gets what the button that keeps the bigger mod says.</summary>
    public string? KeepWholeLabel => Inside is { } inside ? $"Keep {ModDisplayName.For(inside.WholeName)}" : null;

    /// <summary>Gets when it was added, for sorting by newest (P2.6-30).</summary>
    public DateTimeOffset AddedAt { get; private init; }

    /// <summary>Gets the page of the known item this mod's download is, or is a copy of (ADR-0034 §7).</summary>
    public string? PageLink { get; private init; }

    /// <summary>Gets where it stands among the library's copies of this mod, or null when it is the only one (P2.6-18).</summary>
    public ModCopy? Copy { get; private init; }

    [ObservableProperty]
    public partial bool IsActive { get; set; }

    /// <summary>Gets or sets a value indicating whether it is also in the game, so it shows once and turning it on takes that over (ADR-0037).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Glance))]
    public partial bool IsInGame { get; set; }

    /// <summary>Gets or sets what it does to the files it shares with other mods, or null when it shares none that differ (P2.6-33).</summary>
    [ObservableProperty]
    public partial string? ConflictMark { get; set; }

    /// <summary>Gets or sets a value indicating whether its row is expanded to show its details (ADR-0038).</summary>
    [ObservableProperty]
    public partial bool IsExpanded { get; set; }

    /// <summary>Gets what a one-line row says beside the name: its kind, and anything worth a glance (ADR-0038).</summary>
    public string Glance
    {
        get
        {
            var kind = Details.Split(" · ")[0].StartsWith("add-in", StringComparison.Ordinal) ? "add-in" : Details.Split(" · ")[0];
            return string.Join(" · ", new[] { kind, IsInGame ? "in your game" : null, Copy is null ? null : $"{Copy.Count} copies" }.OfType<string>());
        }
    }

    /// <summary>Gets or sets a value indicating whether it is ticked for <i>Activate selected</i> or <i>Deactivate selected</i> (P2.5-14).</summary>
    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    /// <summary>Whether this row says all that <paramref name="other"/> does, so the row on screen can stay (P2.6-49).</summary>
    public bool ShowsTheSameAs(LibraryModRow other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return Id == other.Id && Title == other.Title && Details == other.Details && Warning == other.Warning
            && PageLink == other.PageLink && AddedAt == other.AddedAt && Copy == other.Copy && Inside == other.Inside && Notes.SequenceEqual(other.Notes);
    }

    /// <param name="entry">The library mod.</param>
    /// <param name="check">How its download compares with the catalog's known downloads, or the error that kept it from being compared.</param>
    /// <param name="copy">Where it stands among its copies, when the library holds it more than once.</param>
    /// <param name="inside">The bigger mod it is inside (ADR-0041), when it is.</param>
    public static LibraryModRow From(LibraryEntry entry, Result<DownloadCheck> check, ModCopy? copy = null, ModInside? inside = null)
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

        if (RelatedNote(copy, inside) is { } related)
        {
            notes.Add(related);
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
            ModDisplayName.For(entry.Mod.Name),
            $"{KindOf(entry.Mod)} · {Counted.Of(entry.Mod.Files.Count, "file")} · {new ByteSize(entry.SizeBytes)} · added {imported} from {entry.SourceName}",
            notes,
            warning)
        {
            IsAddIn = entry.Mod.AddInUid is not null,
            AddedAt = entry.ImportedAtUtc,
            PageLink = check.IsSuccess ? check.Value.Item?.Source : null,
            Copy = copy,
            Inside = inside,
            OnlyLooks = ChangesOnlyLooks(entry.Mod),
        };
    }

    private static bool ChangesOnlyLooks(LibraryMod mod) =>
        mod.Files.All(f => NonModFiles.Is(f.RelativePath) || GameDataFiles.Looks.Contains(Path.GetExtension(f.RelativePath)));

    private static string? RelatedNote(ModCopy? copy, ModInside? inside) =>
        copy is not null ? CopyNote(copy) : inside is not null ? InsideNote(inside) : null;

    private static string InsideNote(ModInside inside) =>
        $"Everything in this mod {(inside.ByName ? "looks to be" : "is")} {(inside.ByName ? "in" : "also in")} {ModDisplayName.For(inside.WholeName)}{(inside.ByName ? " too" : string.Empty)}, so you could keep just that one. If this smaller mod is the one you chose, leave it.";

    private static string CopyNote(ModCopy copy)
    {
        var what = (copy.IsPart, copy.AlsoNamed) switch
        {
            (true, { } other) => $"Everything in this mod is also in {ModDisplayName.For(other)}.",
            (true, null) => "Everything in this mod is also in its other copy.",
            (false, { } other) => $"This is the same mod as {ModDisplayName.For(other)}.",
            _ => $"You have {copy.Count} copies of this mod.",
        };
        return $"{what} Select all picks the one that's on, or else the newest.";
    }

    private static string KindOf(LibraryMod mod) => mod.Kind switch
    {
        ModKind.GameProgram => "runs in the game",
        ModKind.AddIn => $"add-in {mod.AddInUid}",
        _ => "override mod",
    };
}
