using DAORework.Core.Mods.Conflicts;

namespace DAORework.Application.Mods.Changes;

/// <summary>What the load order shows beside its plan: each mod's name, and what mods share inside archives.</summary>
public sealed record LoadOrderDetails(IReadOnlyDictionary<string, string> Names, IReadOnlyList<PackedConflict> Packed);
