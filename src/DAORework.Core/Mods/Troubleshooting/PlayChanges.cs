namespace DAORework.Core.Mods.Troubleshooting;

/// <summary>What is different now from the last game that had other mods (ADR-0043).</summary>
/// <param name="Then">That game.</param>
/// <param name="Added">What is on now and wasn't then.</param>
/// <param name="Removed">What was on then and isn't now.</param>
/// <param name="OrderChanged">Whether the mods on both times are in another order.</param>
public sealed record PlayChanges(PlayRecord Then, IReadOnlyList<string> Added, IReadOnlyList<string> Removed, bool OrderChanged);
