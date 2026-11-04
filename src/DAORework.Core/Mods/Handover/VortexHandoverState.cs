using DAORework.Core.Mods.Adoption;

namespace DAORework.Core.Mods.Handover;

/// <summary>Where the handover stands and what it is about (ADR-0028 §3). Pure: the game folder, the kept handover and what is on come in.</summary>
public sealed record VortexHandoverState(VortexHandoverStep Step, IReadOnlyList<VortexManagedMod> Managed, IReadOnlyList<VortexCopy> Copies)
{
    private static readonly StringComparer _names = StringComparer.OrdinalIgnoreCase;

    public static VortexHandoverState From(HandInstalledSnapshot snapshot, VortexHandover? handover, IReadOnlyCollection<string> activeModIds)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(activeModIds);
        var present = PresentFiles(snapshot);
        var managed = ManagedMods(snapshot, present);
        var copies = handover?.Copies ?? [];
        return new VortexHandoverState(StepOf(snapshot, handover, managed, present, activeModIds), managed, copies);
    }

    /// <summary>The mods Vortex can still put back, each once.</summary>
    public static IReadOnlyList<VortexManagedMod> ManagedMods(HandInstalledSnapshot snapshot, IReadOnlySet<string> present)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        return [.. snapshot.VortexRecords
            .SelectMany(record => record.Files.Where(f => record.IsLive(f.Mod)))
            .GroupBy(f => f.Mod, _names)
            .OrderBy(g => g.Key, _names)
            .Select(g => new VortexManagedMod(g.Key, VortexModNames.Display(g.Key), g.Count(f => present.Contains(f.Path))))];
    }

    /// <summary>Every file in <c>override</c> now, with the paths Vortex's records use.</summary>
    public static IReadOnlySet<string> PresentFiles(HandInstalledSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        return snapshot.OverrideFolders.Values.SelectMany(f => f).Concat(snapshot.LooseOverrideFiles).Select(f => f.Path).ToHashSet(_names);
    }

    private static VortexHandoverStep StepOf(
        HandInstalledSnapshot snapshot,
        VortexHandover? handover,
        IReadOnlyList<VortexManagedMod> managed,
        IReadOnlySet<string> present,
        IReadOnlyCollection<string> activeModIds)
    {
        if (handover is null)
        {
            return managed.Count > 0 ? VortexHandoverStep.Copy : VortexHandoverStep.NothingToDo;
        }

        var copied = handover.Copies.Select(c => c.VortexName).Concat(handover.LeftOut).ToHashSet(_names);
        if (managed.Any(m => !copied.Contains(m.VortexName)))
        {
            return VortexHandoverStep.Copy;
        }

        // Only what Vortex can still reach: it can't purge a mod it has lost.
        if (snapshot.VortexRecords.Any(r => r.Files.Any(f => r.IsLive(f.Mod))) || handover.PlacedPaths.Any(present.Contains))
        {
            return VortexHandoverStep.Purge;
        }

        var active = activeModIds.ToHashSet(_names);
        return handover.Copies.All(c => active.Contains(c.ModId)) ? VortexHandoverStep.Done : VortexHandoverStep.TurnOn;
    }
}
