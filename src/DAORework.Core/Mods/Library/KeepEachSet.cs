namespace DAORework.Core.Mods.Library;

/// <summary>One mod's copies, and what keeping one of them would do.</summary>
/// <param name="Group">What makes them copies, as <see cref="ModCopies"/> names it.</param>
/// <param name="Copies">The copies, newest first.</param>
/// <param name="Plan">Keeping the picked copy, or the one it would start on when the player keeps them all.</param>
public sealed record KeepEachSet(string Group, IReadOnlyList<LibraryEntry> Copies, KeepCopyPlan Plan)
{
    /// <summary>Gets the copies on in the selected folder, by id.</summary>
    public IReadOnlyList<string> On { get; init; } = [];

    /// <summary>Gets a value indicating whether the copies hold different game files, whichever is picked.</summary>
    public bool Differs { get; init; }

    /// <summary>Gets a value indicating whether the player chose to leave this set as it is.</summary>
    public bool KeepsAll { get; init; }

    /// <summary>Gets a value indicating whether this set's other copies will go.</summary>
    public bool Clears => !KeepsAll && Plan.Problems.Count == 0;
}
