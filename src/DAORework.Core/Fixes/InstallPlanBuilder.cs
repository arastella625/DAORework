using DAORework.Core.Mods.Conflicts;
using DAORework.Core.Mods.Library;
using DAORework.Core.Mods.Planning;

namespace DAORework.Core.Fixes;

/// <summary>
/// Collects a plan for the install folder and its next record, one target file at a time, under ADR-0012's ownership
/// rule: for a fix (<see cref="FixPlanner"/>), and for a mod that runs inside the game (ADR-0033 §4), which shares the
/// fix path's record, journal and backups. An entry is named by its library id and places library files.
/// </summary>
public sealed class InstallPlanBuilder
{
    private readonly InstallState _state;
    private readonly List<PlanOperation> _operations = [];
    private readonly List<RecordedFile> _recorded = [];
    private readonly HashSet<string> _touched = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<string> _missing = [];
    private readonly List<string> _changedSince = [];
    private readonly List<PlanBlocker> _blockers = [];
    private int _unchanged;

    public InstallPlanBuilder(InstallState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        _state = state;
    }

    /// <summary>Places <paramref name="file"/> from the entry <paramref name="libraryId"/> at <paramref name="path"/>, relative to the install folder.</summary>
    public void Apply(string libraryId, string path, LibraryModFile file)
    {
        ArgumentNullException.ThrowIfNull(file);
        _touched.Add(path);
        var current = Current(path);
        var recorded = OwnedAsRecorded(path, current);
        if (Same(current, file.Sha256))
        {
            _unchanged++;
            if (recorded is not null)
            {
                _recorded.Add(recorded with { ModId = libraryId });
            }

            return;
        }

        if (recorded is null && current is not null && !_state.Replaceable.Any(h => Same(h, current)))
        {
            _blockers.Add(new PlanBlocker(PlanBlockerKind.UnmanagedFileInTheWay, path, $"{path} is not a file this fix can replace; DAORework leaves it alone."));
            return;
        }

        var source = new DeployedFile(libraryId, file, path, IsOverride: false);
        _operations.Add(new PlanOperation(current is null ? PlanAction.Copy : PlanAction.Replace, path, libraryId, source, current));
        _recorded.Add(new RecordedFile(path, file.Sha256, libraryId) { BeforeSha256 = recorded is null ? current : recorded.BeforeSha256 });
    }

    /// <summary>Takes away a recorded file, putting back the one it replaced from <paramref name="findOriginal"/> (by hash and path).</summary>
    public void Remove(RecordedFile recorded, Func<string, string, DeployedFile?> findOriginal)
    {
        ArgumentNullException.ThrowIfNull(recorded);
        ArgumentNullException.ThrowIfNull(findOriginal);
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

        var original = findOriginal(recorded.BeforeSha256, path);
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
