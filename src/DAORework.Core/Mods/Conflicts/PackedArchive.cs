namespace DAORework.Core.Mods.Conflicts;

/// <summary>What one <c>.erf</c> in an active mod holds, by resource name.</summary>
public sealed record PackedArchive(string ModId, IReadOnlyList<string> Resources);
