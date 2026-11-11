using DAORework.Application.Abstractions;
using DAORework.Application.Fixes;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Fixes;
using DAORework.Core.Mods.Library;
using DAORework.Core.Mods.Planning;
using DAORework.Core.Mods.Safety;

namespace DAORework.Application.Mods.Programs;

/// <summary>
/// Works out turning a mod that runs inside the game on or off (ADR-0033): its files go into <c>bin_ship</c> on the fix
/// path's record, journal and backups, after section 2's checks. Turning it off deletes each recorded file that still
/// has its hash; one the player edited, such as an <c>.ini</c>, stays and is named. Writes nothing.
/// </summary>
public sealed class PlanGameProgramMod
{
    private readonly IDeploymentRecordStore _records;
    private readonly IApplyJournalStore _journals;
    private readonly IModLibrary _library;
    private readonly ReadInstallState _state;
    private readonly ReadGameProgramFiles _files;

    public PlanGameProgramMod(IDeploymentRecordStore records, IApplyJournalStore journals, IModLibrary library, ReadInstallState state, ReadGameProgramFiles files)
    {
        _records = records;
        _journals = journals;
        _library = library;
        _state = state;
        _files = files;
    }

    public async Task<Result<PlannedGameProgramMod>> ExecuteAsync(PlanGameProgramModRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var install = request.InstallDirectory;
        if (await _journals.LoadAsync(install, cancellationToken) is not null)
        {
            return Failure(DiagnosticCodes.ModApplyUnfinished, $"An earlier change in {install} did not finish.", "Finish it or undo it first.");
        }

        if (!(await _records.LoadAsync(install, cancellationToken)).TryGetValue(out var record, out var error))
        {
            return Result.Failure<PlannedGameProgramMod>(error);
        }

        var library = await _library.ListAsync(cancellationToken);
        string NameOf(string id) => library.FirstOrDefault(e => e.Mod.Id.Equals(id, StringComparison.OrdinalIgnoreCase))?.Mod.Name ?? id;
        if (request.Kind == FixChangeKind.Remove)
        {
            return await PlanOffAsync(install, record, request.ModId, NameOf(request.ModId), cancellationToken);
        }

        var entry = library.FirstOrDefault(e => e.Mod.Id.Equals(request.ModId, StringComparison.OrdinalIgnoreCase));
        return entry is null
            ? Failure(DiagnosticCodes.ModNotInLibrary, $"{request.ModId} isn't in your Library.", "Add the mod first.")
            : await PlanOnAsync(install, record, entry.Mod, NameOf, cancellationToken);
    }

    private static Result<PlannedGameProgramMod> Failure(string code, string message, string? recommendation = null) =>
        Result.Failure<PlannedGameProgramMod>(new Error(code, message, recommendation));

    private static List<string> Notes(LibraryMod mod, int placed, IReadOnlyList<string> sixtyFourBit)
    {
        var notes = new List<string>();
        if (sixtyFourBit.Count > 0)
        {
            notes.Add($"64-bit, so the 32-bit game can't load them itself: {string.Join(", ", sixtyFourBit)}.");
        }

        var kept = mod.Files.Count - placed;
        if (kept > 0)
        {
            notes.Add($"{kept} {(kept == 1 ? "file" : "files")} outside its {GamePaths.BinShipDirectoryName} folder, such as a readme, stay in your Library.");
        }

        return notes;
    }

    private async Task<Result<PlannedGameProgramMod>> PlanOnAsync(string install, DeploymentRecord record, LibraryMod mod, Func<string, string> nameOf, CancellationToken cancellationToken)
    {
        if (mod.Kind != ModKind.GameProgram)
        {
            return Failure(DiagnosticCodes.ModSafetyProgramRefused, $"{mod.Name} isn't a mod that runs inside the game.", "Turn it on from the Mods page as usual.");
        }

        // The game folder first: it is cheap to read, and a path that's taken makes reading and scanning the mod pointless.
        var targets = GameProgramLayout.Targets(mod.Files);
        if (!(await _state.ExecuteAsync(install, record, [.. targets.Select(t => t.TargetPath)], new HashSet<string>(), cancellationToken)).TryGetValue(out var state, out var error))
        {
            return Result.Failure<PlannedGameProgramMod>(error);
        }

        if (GameProgramRules.CheckTargets(mod.Id, mod.Name, targets, state, nameOf) is { } taken)
        {
            return Result.Failure<PlannedGameProgramMod>(taken);
        }

        if (!(await _files.ExecuteAsync(mod, targets, cancellationToken)).TryGetValue(out var facts, out error)
            || !GameProgramRules.CheckFiles(mod.Name, facts).TryGetValue(out var sixtyFourBit, out error))
        {
            return Result.Failure<PlannedGameProgramMod>(error);
        }

        var builder = new InstallPlanBuilder(state);
        foreach (var target in targets)
        {
            builder.Apply(mod.Id, target.TargetPath, target.File);
        }

        var plan = builder.Finish(FixChangeKind.Apply, mod.Id, added: mod.Id, removed: null);
        return Result.Success(new PlannedGameProgramMod(mod.Id, mod.Name, plan, Notes(mod, targets.Count, sixtyFourBit)));
    }

    private async Task<Result<PlannedGameProgramMod>> PlanOffAsync(string install, DeploymentRecord record, string modId, string name, CancellationToken cancellationToken)
    {
        if (!record.ActiveModIds.Contains(modId, StringComparer.OrdinalIgnoreCase) || FixIds.IsFixEntry(modId))
        {
            return Failure(DiagnosticCodes.ModNotActive, $"{name} isn't on in {install}.");
        }

        var files = record.Files.Where(f => f.ModId.Equals(modId, StringComparison.OrdinalIgnoreCase)).ToList();
        if (!(await _state.ExecuteAsync(install, record, [.. files.Select(f => f.TargetPath)], new HashSet<string>(), cancellationToken)).TryGetValue(out var state, out var error))
        {
            return Result.Failure<PlannedGameProgramMod>(error);
        }

        // Nothing of a mod like this replaced another file (ADR-0033 §2), so there is never an original to put back.
        var builder = new InstallPlanBuilder(state);
        foreach (var file in files)
        {
            builder.Remove(file, (_, _) => null);
        }

        return Result.Success(new PlannedGameProgramMod(modId, name, builder.Finish(FixChangeKind.Remove, modId, added: null, removed: modId), []));
    }
}
