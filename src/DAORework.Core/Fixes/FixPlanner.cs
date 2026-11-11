using DAORework.Core.Mods.Library;

namespace DAORework.Core.Fixes;

/// <summary>
/// Plans applying or removing one fix in the install folder (ADR-0013), under ADR-0012's ownership rule: a file is
/// replaced or deleted only while it still hashes to what DAORework recorded, or when the caller has identified it as
/// the file the fix is made for. Removing a fix puts back each file it replaced from the library copy with that hash.
/// </summary>
public static class FixPlanner
{
    public static FixPlan PlanApply(FixEntry entry, InstallState state)
    {
        ArgumentNullException.ThrowIfNull(entry);
        var builder = new InstallPlanBuilder(state);
        foreach (var file in entry.Files)
        {
            builder.Apply(entry.LibraryId, file.TargetPath, file.Applied);
        }

        return builder.Finish(FixChangeKind.Apply, entry.Id, added: entry.LibraryId, removed: null);
    }

    /// <summary>Removes the entry the record lists as <paramref name="entryLibraryId"/>; <paramref name="library"/> holds the originals to put back.</summary>
    public static FixPlan PlanRemove(string entryLibraryId, InstallState state, IReadOnlyList<FixEntry> library)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entryLibraryId);
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(library);
        var builder = new InstallPlanBuilder(state);
        foreach (var recorded in state.Record.Files.Where(f => string.Equals(f.ModId, entryLibraryId, StringComparison.OrdinalIgnoreCase)))
        {
            builder.Remove(recorded, (sha256, path) => Find(library, sha256, path));
        }

        var entryId = entryLibraryId[(entryLibraryId.LastIndexOf('/') + 1)..];
        return builder.Finish(FixChangeKind.Remove, entryId, added: null, removed: entryLibraryId);
    }

    /// <summary>A file the library holds with <paramref name="sha256"/>, as applied or kept by any fix entry, to put back at <paramref name="targetPath"/>.</summary>
    private static DeployedFile? Find(IReadOnlyList<FixEntry> library, string sha256, string targetPath) =>
        library
            .SelectMany(e => e.Files.Select(f => f.Applied).Concat(e.Kept).Select(file => (Entry: e, File: file)))
            .Where(c => string.Equals(c.File.Sha256, sha256, StringComparison.OrdinalIgnoreCase))
            .Select(c => new DeployedFile(c.Entry.LibraryId, c.File, targetPath, IsOverride: false))
            .FirstOrDefault();
}
