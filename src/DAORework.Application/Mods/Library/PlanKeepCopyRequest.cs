namespace DAORework.Application.Mods.Library;

/// <param name="KeepId">The copy to keep.</param>
/// <param name="DocumentsDirectory">The selected profile's documents folder, where a swap can be made.</param>
/// <param name="InstallDirectory">The install folder, whose override a swap's warnings look at.</param>
public sealed record PlanKeepCopyRequest(string KeepId, string? DocumentsDirectory, string? InstallDirectory);
