namespace DAORework.Application.Mods.Library;

/// <param name="Picks">The player's picks by set: the copy to keep, or null to keep them all. A set with none starts on its default.</param>
/// <param name="DocumentsDirectory">The selected profile's documents folder, where a swap can be made.</param>
/// <param name="InstallDirectory">The install folder, whose override a swap's warnings look at.</param>
public sealed record PlanKeepEachRequest(IReadOnlyDictionary<string, string?> Picks, string? DocumentsDirectory, string? InstallDirectory);
