using DAORework.Core.Common;
using DAORework.Core.Mods.Adoption;

namespace DAORework.App.ViewModels;

/// <summary>
/// One line of the <i>Installed by hand</i> list: a title, a details line and any notes, worded as the CLI words them,
/// and for a mod that can be adopted, the candidate itself.
/// </summary>
public sealed record HandInstalledModRow(string Title, string Details, IReadOnlyList<string> Notes, AdoptionCandidate? Candidate = null)
{
    /// <summary>Gets a value indicating whether adopting it needs a name: loose override files have none of their own.</summary>
    public bool NeedsName => Candidate?.Kind == AdoptionCandidateKind.LooseOverrideFiles;

    public static HandInstalledModRow From(AdoptionCandidate candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        var kind = candidate.Kind switch
        {
            AdoptionCandidateKind.AddIn => "add-in",
            AdoptionCandidateKind.OverrideFolder => "override folder",
            _ => "loose override files, named when adopted",
        };
        var notes = new List<string>();
        if (candidate.CatalogItem is { } item)
        {
            notes.Add($"Known as {item.Name}: {item.Source}");
        }

        if (candidate.LibraryMatch is { } match)
        {
            notes.Add(match.IsExact
                ? $"Same as library mod {match.ModName}: adopting imports nothing new."
                : $"Differs from library mod {match.ModName} in {match.DifferingFiles} file(s): adopting would import it from disk.");
        }

        if (candidate.Programs.Count > 0)
        {
            var names = string.Join(", ", candidate.Programs.Select(p => p.Path[(p.Path.LastIndexOf('/') + 1)..]));
            notes.Add($"Holds {candidate.Programs.Count} program file(s) ({names}): adopting leaves them where they are, and DAORework never installs programs.");
        }

        if (candidate.DeployedByVortex)
        {
            var folder = candidate.Kind == AdoptionCandidateKind.AddIn ? "AddIns" : "override";
            notes.Add($"Vortex manages files in {folder}. Purge this mod in Vortex before adopting it, or Vortex may undo the move.");
        }

        return new HandInstalledModRow(
            candidate.Name,
            $"{candidate.Id} · {kind} · {candidate.Files.Count} file(s) · {new ByteSize(candidate.SizeBytes)} · in {candidate.Path}",
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
        return new HandInstalledModRow(exclusion.Name, exclusion.Id, [reason]);
    }
}
