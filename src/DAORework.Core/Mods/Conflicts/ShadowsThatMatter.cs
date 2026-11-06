using DAORework.Core.Mods.Library;

namespace DAORework.Core.Mods.Conflicts;

/// <summary>
/// Which files DAORework doesn't manage are worth warning about (P2.6-20, ADR-0012). A mod already on was warned about
/// when it went on, so only mods this change adds count. A file with the same content as the mod's changes nothing,
/// whichever copy the game picks. A file that could not be compared still warns. Pure.
/// </summary>
public static class ShadowsThatMatter
{
    private static readonly StringComparer _paths = StringComparer.OrdinalIgnoreCase;

    /// <summary>The documents-relative paths to hash: the unmanaged files that share a name with a file of a mod this change adds.</summary>
    public static IReadOnlyList<string> PathsToCompare(IReadOnlyList<UnmanagedShadow> shadows, IReadOnlyCollection<string> activeModIds)
    {
        ArgumentNullException.ThrowIfNull(shadows);
        ArgumentNullException.ThrowIfNull(activeModIds);
        return shadows
            .Where(s => !activeModIds.Contains(s.Managed.ModId, _paths))
            .SelectMany(s => s.UnmanagedPaths)
            .Select(DocumentsPath)
            .Distinct(_paths)
            .ToList();
    }

    /// <param name="shadows">Every file that shares a name with an unmanaged one.</param>
    /// <param name="activeModIds">The mods on before this change.</param>
    /// <param name="hashes">The SHA-256 of documents-relative paths; a path with none could not be compared.</param>
    public static IReadOnlyList<UnmanagedShadow> Keep(IReadOnlyList<UnmanagedShadow> shadows, IReadOnlyCollection<string> activeModIds, IReadOnlyDictionary<string, string> hashes)
    {
        ArgumentNullException.ThrowIfNull(shadows);
        ArgumentNullException.ThrowIfNull(activeModIds);
        ArgumentNullException.ThrowIfNull(hashes);
        return shadows
            .Where(s => !activeModIds.Contains(s.Managed.ModId, _paths) && !s.UnmanagedPaths.All(p => IsSame(s, p, hashes)))
            .ToList();
    }

    /// <summary>The files of mods this change adds whose every unmanaged copy has the same content, which turning them on takes over (ADR-0037).</summary>
    public static IReadOnlyList<UnmanagedShadow> Same(IReadOnlyList<UnmanagedShadow> shadows, IReadOnlyCollection<string> activeModIds, IReadOnlyDictionary<string, string> hashes)
    {
        ArgumentNullException.ThrowIfNull(shadows);
        ArgumentNullException.ThrowIfNull(activeModIds);
        ArgumentNullException.ThrowIfNull(hashes);
        return shadows
            .Where(s => !activeModIds.Contains(s.Managed.ModId, _paths) && s.UnmanagedPaths.All(p => IsSame(s, p, hashes)))
            .ToList();
    }

    private static bool IsSame(UnmanagedShadow shadow, string path, IReadOnlyDictionary<string, string> hashes) =>
        hashes.TryGetValue(DocumentsPath(path), out var hash) && hash.Equals(shadow.Managed.Source.Sha256, StringComparison.OrdinalIgnoreCase);

    private static string DocumentsPath(string overridePath) => ModLayout.OverridePrefix + overridePath.Replace('\\', '/');
}
