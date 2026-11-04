using DAORework.Core.Mods.Library;

namespace DAORework.Core.Mods.Downloads;

/// <summary>
/// What counts as a new mod in the downloads folder (ADR-0034 §4), from names only. An archive passes if it holds a
/// <c>.dazip</c>, an add-in's <c>Manifest.xml</c> and <c>Contents/</c>, or a file of the game's own data. <c>.dds</c> does
/// not count on its own, because every game on Nexus uses it: a mod missed is better than offering a player's other games.
/// Pure.
/// </summary>
public static class ModDownloadRule
{
    private static readonly HashSet<string> _dragonAgeData = new(StringComparer.OrdinalIgnoreCase)
    {
        ".erf", ".gda", ".tlk", ".ncs", ".uti", ".utc", ".mmh", ".msh", ".mao",
    };

    /// <summary>Gets the kinds of file <i>Add mods</i> takes.</summary>
    public static IReadOnlyList<string> Extensions { get; } = [".dazip", ".zip", ".7z", ".rar"];

    public static bool IsDownload(string path) => Extensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase);

    /// <summary>Whether <paramref name="path"/> is a mod by its name alone, so its archive need not be opened.</summary>
    public static bool IsDazip(string path) => Path.GetExtension(path).Equals(".dazip", StringComparison.OrdinalIgnoreCase);

    public static bool LooksLikeMod(IReadOnlyList<string> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        return entries.Any(IsDataOrDazip) || ArchiveClassifier.IsAddInLayout(entries);
    }

    /// <summary>Whether a file is new: after the player turned this on (when that matters), and not dealt with already.</summary>
    public static bool IsNew(DownloadFile file, DateTimeOffset? since, IReadOnlyList<HandledDownload> handled)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentNullException.ThrowIfNull(handled);
        var arrived = file.CreatedUtc > file.ChangedUtc ? file.CreatedUtc : file.ChangedUtc;
        return (since is null || arrived > since) && !handled.Any(h => h.Is(file));
    }

    private static bool IsDataOrDazip(string entry)
    {
        var extension = Path.GetExtension(entry);
        return _dragonAgeData.Contains(extension)
            || extension.Equals(".dazip", StringComparison.OrdinalIgnoreCase)
            || entry.EndsWith("chargenmorphcfg.xml", StringComparison.OrdinalIgnoreCase);
    }
}
