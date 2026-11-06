using DAORework.Core.Common;
using DAORework.Core.Mods.Library;

namespace DAORework.Core.Mods.Adoption;

/// <summary>
/// Whether a download looks like something already held, from its file names alone (ADR-0039): an add-in by its UID, an
/// override mod when every game file it holds has the name of a file held elsewhere. The game finds override files by
/// name (ADR-0012), so folders are no difference. What is in the game and not managed comes first. Names can't prove
/// two mods are the same, so this only ever says <i>looks like</i>. Pure.
/// </summary>
public static class AlreadyHeld
{
    private const string AddInsPrefix = "Contents/addins/";

    private static readonly StringComparer _names = StringComparer.OrdinalIgnoreCase;

    /// <param name="entryNames">The download's file names, with <c>/</c>.</param>
    /// <param name="found">What was found in the game.</param>
    /// <param name="library">The library's mods.</param>
    /// <returns>What it looks like, or null when nothing does, or when the names can't say.</returns>
    public static HeldElsewhere? Find(IReadOnlyList<string> entryNames, IReadOnlyList<AdoptionCandidate> found, IReadOnlyList<LibraryMod> library)
    {
        ArgumentNullException.ThrowIfNull(entryNames);
        ArgumentNullException.ThrowIfNull(found);
        ArgumentNullException.ThrowIfNull(library);

        var notManaged = found.Where(c => c.LibraryMatch is not { IsExact: true }).ToList();
        if (ArchiveClassifier.IsAddInLayout(entryNames))
        {
            return UidOf(entryNames) is { } uid ? FindAddIn(uid, notManaged, library) : null;
        }

        // A .dazip inside a zip keeps its names to itself, so nothing can be said until it is added.
        if (entryNames.Any(n => n.EndsWith(".dazip", StringComparison.OrdinalIgnoreCase)))
        {
            return null;
        }

        var names = GameFileNames(entryNames);
        return names.Count == 0 ? null : FindOverride(names, notManaged, library);
    }

    private static HeldElsewhere? FindAddIn(string uid, List<AdoptionCandidate> notManaged, IReadOnlyList<LibraryMod> library)
    {
        var inGame = notManaged.FirstOrDefault(c => c.Kind == AdoptionCandidateKind.AddIn && _names.Equals(FileName(c.Path), uid));
        if (inGame is not null)
        {
            return new HeldElsewhere(inGame.Name, inGame);
        }

        return library.FirstOrDefault(m => _names.Equals(m.AddInUid, uid)) is { } mod ? new HeldElsewhere(mod.Name, null) : null;
    }

    private static HeldElsewhere? FindOverride(HashSet<string> names, List<AdoptionCandidate> notManaged, IReadOnlyList<LibraryMod> library)
    {
        var inGame = notManaged
            .Where(c => c.Kind != AdoptionCandidateKind.AddIn)
            .Where(c => names.IsSubsetOf(c.Files.Select(f => FileName(f.Path)).ToHashSet(_names)))
            .MinBy(c => c.Files.Count);
        if (inGame is not null)
        {
            return new HeldElsewhere(inGame.Name, inGame);
        }

        var closest = library
            .Where(m => m.Kind == ModKind.Override)
            .Where(m => names.IsSubsetOf(GameFileNames(m.Files.Select(f => f.RelativePath))))
            .MinBy(m => m.Files.Count);
        return closest is null ? null : new HeldElsewhere(closest.Name, null);
    }

    /// <summary>The names that say what a mod is: not its readmes, nor the file every hair and head mod ships (ADR-0025).</summary>
    private static HashSet<string> GameFileNames(IEnumerable<string> paths) =>
        paths.Where(p => !NonModFiles.Is(p))
            .Select(FileName)
            .Where(n => !_names.Equals(n, GamePaths.ChargenMorphFileName))
            .ToHashSet(_names);

    private static string? UidOf(IReadOnlyList<string> entryNames)
    {
        var inside = entryNames.FirstOrDefault(n => n.StartsWith(AddInsPrefix, StringComparison.OrdinalIgnoreCase))?[AddInsPrefix.Length..];
        return inside?.Contains('/', StringComparison.Ordinal) == true ? inside[..inside.IndexOf('/', StringComparison.Ordinal)] : null;
    }

    private static string FileName(string path) => path[(path.LastIndexOf('/') + 1)..];
}
