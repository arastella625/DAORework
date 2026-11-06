using DAORework.Application.Abstractions;
using DAORework.Core.Mods.Combining;
using DAORework.Core.Mods.Conflicts;
using DAORework.Core.Mods.Library;
using DAORework.Core.Mods.Planning;

namespace DAORework.Application.Mods.Changes;

/// <summary>
/// Hashes the unmanaged override files a change's new mods share a name with (P2.6-20), so a file with the same content
/// isn't warned about. A change that adds no mod, such as a new order, reads nothing more. When a file can't be read
/// the plan goes on without its hash, and the warning stays.
/// </summary>
internal static class UnmanagedCopies
{
    /// <summary>The snapshot's hashes with those of the files to compare added.</summary>
    public static async Task<IReadOnlyDictionary<string, string>> WithHashesAsync(
        IDocumentsScanner scanner,
        string documents,
        DeploymentRecord record,
        IReadOnlyList<LibraryMod> targets,
        IReadOnlyList<CombinedOverride> combined,
        DocumentsSnapshot disk,
        CancellationToken cancellationToken)
    {
        // A mod that can't be mapped stops the plan, which says why.
        if (!ConflictScanner.Scan(targets, disk.UnmanagedOverridePaths, combined).TryGetValue(out var report, out _))
        {
            return disk.Hashes;
        }

        var paths = ShadowsThatMatter.PathsToCompare(report.UnmanagedShadows, record.ActiveModIds);
        if (paths.Count == 0 || !(await scanner.ScanAsync(documents, paths, cancellationToken)).TryGetValue(out var compared, out _))
        {
            return disk.Hashes;
        }

        var hashes = new Dictionary<string, string>(disk.Hashes, StringComparer.OrdinalIgnoreCase);
        foreach (var (path, hash) in compared.Hashes)
        {
            hashes[path] = hash;
        }

        return hashes;
    }
}
