namespace DAORework.Core.Mods.Library;

/// <summary>
/// Where one library file goes: <see cref="TargetPath"/> is relative to the documents folder, with <c>/</c> separators.
/// <see cref="IsOverride"/> marks files under <c>packages/core/override</c>, which the game finds by file name alone.
/// </summary>
public sealed record DeployedFile(string ModId, LibraryModFile Source, string TargetPath, bool IsOverride)
{
    /// <summary>Gets the file name, which is what the game looks override files up by.</summary>
    public string FileName => TargetPath[(TargetPath.LastIndexOf('/') + 1)..];
}
