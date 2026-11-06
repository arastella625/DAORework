using DAORework.Core.Mods.Planning;

namespace DAORework.Core.Mods.Library;

/// <summary>Keeping one copy of a mod and removing the others from the Library (ADR-0039).</summary>
public sealed record KeepCopyPlan(LibraryEntry Kept, IReadOnlyList<LibraryEntry> Removing)
{
    /// <summary>Gets the space the removed copies take up.</summary>
    public long BytesFreed => Removing.Sum(e => e.SizeBytes);

    /// <summary>Gets the names of the game files that only the removed copies hold, which go for good.</summary>
    public IReadOnlyList<string> OnlyInRemoved { get; init; } = [];

    /// <summary>Gets what stops it.</summary>
    public IReadOnlyList<PlanProblem> Problems { get; init; } = [];

    /// <summary>Gets the selected folder's new order when a removed copy is on there, or null when nothing in the game changes.</summary>
    public IReadOnlyList<string>? SwappedOrder { get; init; }

    /// <summary>Gets the name of the copy that gives up its place, when one does.</summary>
    public string? SwappedOut { get; init; }
}
