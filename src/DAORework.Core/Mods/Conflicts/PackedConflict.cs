namespace DAORework.Core.Mods.Conflicts;

/// <summary>A resource two or more active mods hold, at least one of them inside an archive (ROADMAP P3-07).</summary>
public sealed record PackedConflict(string FileName, IReadOnlyList<string> ModIds);
