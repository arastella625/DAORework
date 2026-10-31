using DAORework.Core.Common;
using DAORework.Core.Mods.Adoption;
using Spectre.Console;

namespace DAORework.Cli.Rendering;

/// <summary>Writes the adoption scan (ADR-0015): each candidate with its id, then what cannot be adopted and why.</summary>
public sealed class AdoptionRenderer
{
    private readonly IAnsiConsole _console;

    public AdoptionRenderer(IAnsiConsole console)
    {
        _console = console;
    }

    public void RenderScan(AdoptionScan scan, bool listFiles)
    {
        ArgumentNullException.ThrowIfNull(scan);
        _console.WriteLine(scan.Candidates.Count == 0
            ? "Nothing installed by hand can be adopted."
            : $"Installed by hand, can be adopted ({scan.Candidates.Count}):");
        foreach (var candidate in scan.Candidates)
        {
            RenderCandidate(candidate, listFiles);
        }

        var official = scan.Exclusions.Count(e => e.Reason == AdoptionExclusionReason.Official);
        var others = scan.Exclusions.Where(e => e.Reason != AdoptionExclusionReason.Official).ToList();
        if (others.Count > 0)
        {
            _console.WriteLine($"Cannot be adopted ({others.Count}):");
            foreach (var exclusion in others)
            {
                _console.WriteLine($"  {exclusion.Id}  {exclusion.Name}: {Reason(exclusion.Reason)}");
            }
        }

        if (official > 0)
        {
            _console.WriteLine($"Official DLC ({official} add-in(s)) is never adopted or changed.");
        }

        _console.WriteLine(scan.Candidates.Count == 0 ? "Nothing was changed." : "Nothing was changed. To adopt: mods adopt <id>... (it shows the plan and asks first).");
    }

    /// <summary>The note for a candidate's program files (ADR-0018).</summary>
    private static string ProgramNote(int count, IEnumerable<string> names) =>
        $"Holds {count} program file(s) ({string.Join(", ", names)}): adopting leaves them where they are, and DAORework never installs programs.";

    private static string Kind(AdoptionCandidateKind kind) => kind switch
    {
        AdoptionCandidateKind.AddIn => "add-in",
        AdoptionCandidateKind.OverrideFolder => "override folder",
        _ => "loose override files, named when adopted",
    };

    /// <summary>Why something cannot be adopted, also used when it is named on the command line.</summary>
    public static string Reason(AdoptionExclusionReason reason) => reason switch
    {
        AdoptionExclusionReason.NoFolderInDocuments => "registered in AddIns.xml, but its folder is not in the documents folder (it is in the install folder, or missing)",
        AdoptionExclusionReason.NotRegistered => "a folder in AddIns with no entry in AddIns.xml, so the game does not load it",
        _ => "official DLC",
    };

    private void RenderCandidate(AdoptionCandidate candidate, bool listFiles)
    {
        _console.WriteLine($"  {candidate.Id}  {candidate.Name}  ({Kind(candidate.Kind)}, {candidate.Files.Count} file(s), {new ByteSize(candidate.SizeBytes)}, in {candidate.Path})");
        if (candidate.CatalogItem is { } item)
        {
            _console.WriteLine($"    Known as {item.Name}: {item.Source}");
        }

        if (candidate.LibraryMatch is { } match)
        {
            _console.WriteLine(match.IsExact
                ? $"    Same as library mod {match.ModName} ({match.ModId}): adopting imports nothing new."
                : $"    Differs from library mod {match.ModName} ({match.ModId}) in {match.DifferingFiles} file(s): adopting would import it from disk.");
        }

        if (candidate.Programs.Count > 0)
        {
            _console.WriteLine($"    {ProgramNote(candidate.Programs.Count, candidate.Programs.Select(p => p.Path[(p.Path.LastIndexOf('/') + 1)..]))}");
        }

        if (candidate.DeployedByVortex)
        {
            var folder = candidate.Kind == AdoptionCandidateKind.AddIn ? "AddIns" : "override";
            _console.WriteLine($"    Vortex manages files in {folder}. Purge this mod in Vortex before adopting it, or Vortex may undo the move.");
        }

        if (listFiles)
        {
            foreach (var file in candidate.Files)
            {
                _console.WriteLine($"    {file.Path}");
            }
        }
    }
}
