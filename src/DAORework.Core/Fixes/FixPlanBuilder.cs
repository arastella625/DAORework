using DAORework.Core.Mods.Conflicts;
using DAORework.Core.Mods.Library;
using DAORework.Core.Mods.Planning;

namespace DAORework.Core.Fixes;

/// <summary>Collects a fix plan's operations and its next record, one target file at a time (<see cref="FixPlanner"/>).</summary>
internal sealed class FixPlanBuilder
{
    private readonly InstallState _state;
    private readonly List<PlanOperation> _operations = [];
    private readonly List<RecordedFile> _recorded = [];
    private readonly HashSet<string> _touched = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<string> _missing = [];
    private readonly List<string> _changedSince = [];
    private readonly List<PlanBlocker> _blockers = [];
    private int _unchanged;

    public FixPlanBuilder(InstallState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        _state = state;
    }

    public void Apply(FixEntry entry, FixFile file)
    {
        var path = file.TargetPath;
        _touched.Add(path);
        var current = Current(path);
        var recorded = OwnedAsRecorded(path, current);
        if (Same(current, file.Applied.Sha256))
        {
            _unchanged++;
            if (recorded is not null)
            {
                _recorded.Add(recorded with { ModId = entry.LibraryId });
            }

            return;
        }

        if (recorded is null && current is not null && !_state.Replaceable.Any(h => Same(h, current)))
        {
            _blockers.Add(new PlanBlocker(PlanBlockerKind.UnmanagedFileInTheWay, path, $"{path} is not a file this fix can replace; DAORework leaves it alone."));
            return;
        }

        var source = new DeployedFile(entry.LibraryId, file.Applied, path, IsOverride: false);
        _operations.Add(new PlanOperation(current is null ? PlanAction.Copy : PlanAction.Replace, path, entry.LibraryId, source, current));
        _recorded.Add(new RecordedFile(path, file.Applied.Sha256, entry.LibraryId) { BeforeSha256 = recorded is null ? current : recorded.BeforeSha256 });
    }

    public void Remove(RecordedFile recorded, IReadOnlyList<FixEntry> library)
    {
        var path = recorded.TargetPath;
        _touched.Add(path);
        var current = Current(path);
        if (OwnedAsRecorded(path, current) is null)
        {
            return;
        }

        if (recorded.BeforeSha256 is null)
        {
            _operations.Add(new PlanOperation(PlanAction.Delete, path, recorded.ModId, null, current));
            return;
        }

        var original = Find(library, recorded.BeforeSha256, path);
        if (original is null)
        {
            _blockers.Add(new PlanBlocker(PlanBlockerKind.FixOriginalMissing, path, $"The file {path} replaced is no longer in DAORework's library, so it cannot be put back. Use fixes rollback instead."));
            return;
        }

        _operations.Add(new PlanOperation(PlanAction.Replace, path, recorded.ModId, original, current));
    }

    public FixPlan Finish(FixChangeKind kind, string entryId, string? added, string? removed)
    {
        var record = _state.Record;
        var files = record.Files.Where(f => !_touched.Contains(f.TargetPath)).Concat(_recorded).ToList();
        var owners = files.Select(f => f.ModId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var active = record.ActiveModIds.Where(id => owners.Contains(id) && !Same(id, removed)).ToList();
        if (added is not null && owners.Contains(added) && !active.Contains(added, StringComparer.OrdinalIgnoreCase))
        {
            active.Add(added);
        }

        var change = new ModActivationPlan(record.ActiveModIds, active, _operations, [], _unchanged, _missing, [.. _state.Blockers, .. _blockers], new ConflictReport([], [], [], [], []))
        {
            CurrentRecord = record,
            NextRecord = record with { ActiveModIds = active, Files = files },
        };
        return new FixPlan(kind, entryId, change, _changedSince);
    }

    private static bool Same(string? a, string? b) => a is not null && b is not null && string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

    private static DeployedFile? Find(IReadOnlyList<FixEntry> library, string sha256, string targetPath) =>
        library
            .SelectMany(e => e.Files.Select(f => f.Applied).Concat(e.Kept).Select(file => (Entry: e, File: file)))
            .Where(c => Same(c.File.Sha256, sha256))
            .Select(c => new DeployedFile(c.Entry.LibraryId, c.File, targetPath, IsOverride: false))
            .FirstOrDefault();

    private string? Current(string path) => _state.Hashes.TryGetValue(path, out var sha) ? sha : null;

    /// <summary>The record's entry for the path while the file is still what DAORework left; otherwise notes why not and returns null.</summary>
    private RecordedFile? OwnedAsRecorded(string path, string? current)
    {
        var recorded = _state.Record.Files.FirstOrDefault(f => string.Equals(f.TargetPath, path, StringComparison.OrdinalIgnoreCase));
        if (recorded is null || Same(current, recorded.Sha256))
        {
            return recorded;
        }

        (current is null ? _missing : _changedSince).Add(path);
        return null;
    }
}
