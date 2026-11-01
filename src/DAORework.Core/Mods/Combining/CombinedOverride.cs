using DAORework.Core.Mods.Library;

namespace DAORework.Core.Mods.Combining;

/// <summary>
/// A file DAORework combined from several mods' copies (ADR-0025), deployed in their place: the file as kept in the
/// library, the mods it was combined from in load order, and how many entries it lists.
/// </summary>
public sealed record CombinedOverride(DeployedFile File, IReadOnlyList<string> FromModIds, int EntryCount)
{
    /// <summary>Gets the file name the combined file stands in for, such as <c>chargenmorphcfg.xml</c>.</summary>
    public string FileName => File.FileName;
}
