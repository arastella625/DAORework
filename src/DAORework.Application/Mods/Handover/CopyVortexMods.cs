using DAORework.Application.Abstractions;
using DAORework.Application.Mods.Adoption;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Mods.Adoption;
using DAORework.Core.Mods.Handover;
using DAORework.Core.Mods.Library;

namespace DAORework.Application.Mods.Handover;

/// <summary>
/// Step 1 of the handover (ADR-0028 §3): each mod Vortex still manages is added to the library as a real copy, through
/// the same import as <i>Add mods</i>, from its staging folder or, when that can't be read, from the files Vortex
/// placed. Nothing in the game changes, and nothing of Vortex's is written. It can be run again: copies already made are
/// kept, and a mod that failed is tried again.
/// </summary>
public sealed class CopyVortexMods
{
    private readonly IHandInstalledModsReader _reader;
    private readonly IModLibrary _library;
    private readonly IVortexHandoverStore _store;
    private readonly IAdoptionImporter _importer;
    private readonly ScanForAdoption _scan;

    public CopyVortexMods(IHandInstalledModsReader reader, IModLibrary library, IVortexHandoverStore store, IAdoptionImporter importer, ScanForAdoption scan)
    {
        _reader = reader;
        _library = library;
        _store = store;
        _importer = importer;
        _scan = scan;
    }

    public async Task<Result<VortexCopyReport>> ExecuteAsync(string documentsDirectory, CancellationToken cancellationToken = default)
    {
        if (!(await _reader.ReadAsync(documentsDirectory, cancellationToken)).TryGetValue(out var snapshot, out var error)
            || !(await _store.LoadAsync(documentsDirectory, cancellationToken)).TryGetValue(out var kept, out error))
        {
            return Result.Failure<VortexCopyReport>(error);
        }

        var run = new Run(
            (kept?.Copies ?? []).ToDictionary(c => c.VortexName, StringComparer.OrdinalIgnoreCase),
            (await _library.ListAsync(cancellationToken)).ToDictionary(e => e.Mod.Id, StringComparer.OrdinalIgnoreCase),
            (kept?.LeftOut ?? []).ToHashSet(StringComparer.OrdinalIgnoreCase));
        foreach (var mod in VortexHandoverState.ManagedMods(snapshot, VortexHandoverState.PresentFiles(snapshot)).Where(run.IsNew))
        {
            run.Add(mod, await CopyAsync(documentsDirectory, snapshot, mod, cancellationToken));
        }

        await _store.KeepAddInsListAsync(documentsDirectory, cancellationToken);
        var (order, unsettled) = Arranged(snapshot, run.Copied, run.Entries);
        var handover = new VortexHandover([.. order.Select(name => run.Copied[name])], Placed(snapshot, kept)) { LeftOut = [.. run.LeftOut] };
        await _store.SaveAsync(documentsDirectory, handover, cancellationToken);
        return Result.Success(new VortexCopyReport(handover.Copies, run.Failed, [.. run.LeftOut.Select(VortexModNames.Display)], [.. unsettled.Select(VortexModNames.Display)]));
    }

    private static (IReadOnlyList<string> Order, IReadOnlyList<string> Unsettled) Arranged(
        HandInstalledSnapshot snapshot,
        Dictionary<string, VortexCopy> copied,
        Dictionary<string, LibraryEntry> entries)
    {
        var winners = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var record in snapshot.VortexRecords)
        {
            foreach (var file in record.Files)
            {
                winners[file.Path[record.RelativeFolder.Length..]] = file.Mod;
            }
        }

        var mods = copied.Values.OrderBy(c => c.VortexName, StringComparer.OrdinalIgnoreCase)
            .Select(c => (c.VortexName, Files: entries.TryGetValue(c.ModId, out var entry) ? entry.Mod.Files.Select(f => f.RelativePath).ToList() : (IReadOnlyList<string>)[]))
            .ToList();
        return VortexLoadOrder.Arrange(mods, winners);
    }

    private static List<string> Placed(HandInstalledSnapshot snapshot, VortexHandover? kept) =>
        [.. (kept?.PlacedPaths ?? []).Concat(snapshot.VortexRecords.SelectMany(r => r.Files.Select(f => f.Path))).Distinct(StringComparer.OrdinalIgnoreCase)];

    private async Task<Result<LibraryEntry>> CopyAsync(string documentsDirectory, HandInstalledSnapshot snapshot, VortexManagedMod mod, CancellationToken cancellationToken)
    {
        var staging = snapshot.VortexRecords.First(r => r.IsLive(mod.VortexName)).StagingPath;
        var imported = await _library.ImportAsync(Path.Combine(staging, mod.VortexName), cancellationToken);
        if (imported.TryGetValue(out var import, out var error))
        {
            return Result.Success(import.Entry);
        }

        // Only a folder that can't be read falls back. A refusal, such as a detection, stands.
        if (error.Code != DiagnosticCodes.ModImportFailed)
        {
            return Result.Failure<LibraryEntry>(error);
        }

        if (!(await _scan.ExecuteAsync(documentsDirectory, cancellationToken)).TryGetValue(out var scan, out var scanError))
        {
            return Result.Failure<LibraryEntry>(scanError);
        }

        return scan.Candidates.FirstOrDefault(c => c.Id == AdoptionIds.VortexMod(mod.VortexName)) is { } candidate
            ? await _importer.ImportAsync(documentsDirectory, candidate, mod.Name, cancellationToken)
            : Result.Failure<LibraryEntry>(error);
    }

    /// <summary>What one run has so far: the copies, kept from before and new, what the library holds, and what didn't go in.</summary>
    private sealed record Run(Dictionary<string, VortexCopy> Copied, Dictionary<string, LibraryEntry> Entries, HashSet<string> LeftOut)
    {
        public List<(string Name, Error Error)> Failed { get; } = [];

        public bool IsNew(VortexManagedMod mod) => !Copied.ContainsKey(mod.VortexName) && !LeftOut.Contains(mod.VortexName);

        public void Add(VortexManagedMod mod, Result<LibraryEntry> import)
        {
            if (!import.TryGetValue(out var entry, out var error))
            {
                Failed.Add((mod.Name, error));
            }
            else if (entry.Mod.Kind == ModKind.GameProgram)
            {
                LeftOut.Add(mod.VortexName);
            }
            else
            {
                Copied[mod.VortexName] = new VortexCopy(mod.VortexName, entry.Mod.Id);
                Entries[entry.Mod.Id] = entry;
            }
        }
    }
}
