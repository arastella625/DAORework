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
        var builder = new FixPlanBuilder(state);
        foreach (var file in entry.Files)
        {
            builder.Apply(entry, file);
        }

        return builder.Finish(FixChangeKind.Apply, entry.Id, added: entry.LibraryId, removed: null);
    }

    /// <summary>Removes the entry the record lists as <paramref name="entryLibraryId"/>; <paramref name="library"/> holds the originals to put back.</summary>
    public static FixPlan PlanRemove(string entryLibraryId, InstallState state, IReadOnlyList<FixEntry> library)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entryLibraryId);
        ArgumentNullException.ThrowIfNull(state);
        var builder = new FixPlanBuilder(state);
        foreach (var recorded in state.Record.Files.Where(f => string.Equals(f.ModId, entryLibraryId, StringComparison.OrdinalIgnoreCase)))
        {
            builder.Remove(recorded, library);
        }

        var entryId = entryLibraryId[(entryLibraryId.LastIndexOf('/') + 1)..];
        return builder.Finish(FixChangeKind.Remove, entryId, added: null, removed: entryLibraryId);
    }
}
