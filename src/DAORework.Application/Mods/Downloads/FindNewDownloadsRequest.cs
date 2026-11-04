namespace DAORework.Application.Mods.Downloads;

/// <param name="Folder">A folder to look in instead of the saved or reported one.</param>
/// <param name="Asked">Whether the player asked outright, which is consent, so the setting and its date don't apply (ADR-0034 §8).</param>
public sealed record FindNewDownloadsRequest(string? Folder = null, bool Asked = false);
