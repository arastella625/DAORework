namespace DAORework.Core.Mods.Library;

/// <summary>One mod's copies, and what keeping one of them would do.</summary>
/// <param name="Group">What makes them copies, as <see cref="ModCopies"/> names it.</param>
/// <param name="Copies">The copies, those that hold everything first, then newest first.</param>
/// <param name="Plan">Keeping the picked copy, or the one it would start on when the set stays as it is.</param>
public sealed record KeepEachSet(string Group, IReadOnlyList<LibraryEntry> Copies, KeepCopyPlan Plan)
{
    /// <summary>Gets the copies on in the selected folder, by id.</summary>
    public IReadOnlyList<string> On { get; init; } = [];

    /// <summary>Gets a value indicating whether no copy holds everything, so keeping any one loses something.</summary>
    public bool Differs { get; init; }

    /// <summary>Gets the copies that are only part of another, by id.</summary>
    public IReadOnlyList<string> Parts { get; init; } = [];

    /// <summary>Gets a value indicating whether this set stays as it is: the player chose that, or it differs and they haven't chosen.</summary>
    public bool KeepsAll { get; init; }

    /// <summary>Gets a value indicating whether this set's other copies will go.</summary>
    public bool Clears => !KeepsAll && Plan.Problems.Count == 0;
}
