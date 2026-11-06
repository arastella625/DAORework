namespace DAORework.Core.Mods.Library;

/// <summary>The bigger mod a mod's files are all in (ADR-0041).</summary>
/// <param name="WholeId">The bigger mod.</param>
/// <param name="WholeName">Its name.</param>
/// <param name="ByName">Whether an archive was compared by its resources' names, so the content isn't proved the same.</param>
public sealed record ModInside(string WholeId, string WholeName, bool ByName);
