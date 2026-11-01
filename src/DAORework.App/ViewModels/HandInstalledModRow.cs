using CommunityToolkit.Mvvm.ComponentModel;
using DAORework.Core.Common;
using DAORework.Core.Mods.Adoption;

namespace DAORework.App.ViewModels;

/// <summary>
/// One line of the <i>Installed by hand</i> list: a title, a details line and any notes, worded as the CLI words them,
/// and for a mod that can be adopted, the candidate itself and whether it is ticked for <i>Adopt selected</i> (P2.5-13).
/// </summary>
public sealed partial class HandInstalledModRow : ObservableObject
{
    public HandInstalledModRow(string title, string details, IReadOnlyList<string> notes, AdoptionCandidate? candidate = null)
    {
        Title = title;
        Details = details;
        Notes = notes;
        Candidate = candidate;
    }

    public string Title { get; }

    public string Details { get; }

    public IReadOnlyList<string> Notes { get; }

    public AdoptionCandidate? Candidate { get; }

    /// <summary>Gets a value indicating whether this is an add-in found in the game, rather than override files.</summary>
    public bool IsAddIn => Candidate?.Kind == AdoptionCandidateKind.AddIn;

    /// <summary>Gets or sets a value indicating whether it is ticked to be adopted with the others by <i>Adopt selected</i>.</summary>
    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    /// <summary>Gets a value indicating whether adopting it needs a name: loose override files have none of their own.</summary>
    public bool NeedsName => Candidate?.Kind == AdoptionCandidateKind.LooseOverrideFiles;

    public static HandInstalledModRow From(AdoptionCandidate candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        var kind = candidate.Kind switch
        {
            AdoptionCandidateKind.AddIn => "add-in",
            AdoptionCandidateKind.OverrideFolder => "override folder",
            _ => "loose override files, named when you manage them",
        };
        var notes = new List<string>();
        if (candidate.CatalogItem is { } item)
        {
            notes.Add($"Known as {item.Name}: {item.Source}");
        }

        if (candidate.LibraryMatch is { } match)
        {
            notes.Add(match.IsExact
                ? $"Same as {match.ModName} in your Library: managing it adds nothing new."
                : $"Differs from library mod {match.ModName} in {Counted.Of(match.DifferingFiles, "file")}: managing it adds this copy to the Library.");
        }

        if (candidate.Programs.Count > 0)
        {
            var names = string.Join(", ", candidate.Programs.Select(p => p.Path[(p.Path.LastIndexOf('/') + 1)..]));
            notes.Add($"Holds {Counted.Of(candidate.Programs.Count, "program file")} ({names}): managing it leaves them where they are, and DAORework never installs programs.");
        }

        if (candidate.DeployedByVortex)
        {
            var folder = candidate.Kind == AdoptionCandidateKind.AddIn ? "AddIns" : "override";
            notes.Add($"Vortex manages files in {folder}. Purge this mod in Vortex before managing it here, or Vortex may undo the move.");
        }

        return new HandInstalledModRow(
            candidate.Name,
            $"{kind} · {Counted.Of(candidate.Files.Count, "file")} · {new ByteSize(candidate.SizeBytes)} · in {candidate.Path}",
            notes,
            candidate);
    }

    public static HandInstalledModRow From(AdoptionExclusion exclusion)
    {
        ArgumentNullException.ThrowIfNull(exclusion);
        var reason = exclusion.Reason switch
        {
            AdoptionExclusionReason.NoFolderInDocuments => "Registered in AddIns.xml, but its folder is not in the documents folder (it is in the install folder, or missing).",
            AdoptionExclusionReason.NotRegistered => "A folder in AddIns with no entry in AddIns.xml, so the game does not load it.",
            _ => "Official DLC.",
        };
        return new HandInstalledModRow(exclusion.Name, "Can't be managed here", [reason]);
    }
}
