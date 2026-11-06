using DAORework.Core.Mods.Planning;

namespace DAORework.Core.Mods.Library;

/// <summary>Keeping one copy of every mod held more than once (ADR-0039). <see cref="Sets"/> come in the order they are shown.</summary>
public sealed record KeepEachPlan(IReadOnlyList<KeepEachSet> Sets)
{
    /// <summary>Gets the sets that will be cleared.</summary>
    public IReadOnlyList<KeepEachSet> Clearing => [.. Sets.Where(s => s.Clears)];

    /// <summary>Gets the copies that go, across every set being cleared.</summary>
    public IReadOnlyList<LibraryEntry> Removing => [.. Clearing.SelectMany(s => s.Plan.Removing)];

    /// <summary>Gets the space the removed copies take up.</summary>
    public long BytesFreed => Clearing.Sum(s => s.Plan.BytesFreed);

    /// <summary>Gets what stops every set, which is a change that didn't finish.</summary>
    public IReadOnlyList<PlanProblem> Problems { get; init; } = [];

    /// <summary>Gets the selected folder's new order with every swap made, or null when nothing in the game changes.</summary>
    public IReadOnlyList<string>? SwappedOrder { get; init; }
}
