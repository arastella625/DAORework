namespace DAORework.Core.Mods.Planning;

/// <summary>A file DAORework placed: its path relative to the documents folder (<c>/</c> separators), its hash then, and its mod.</summary>
public sealed record RecordedFile(string TargetPath, string Sha256, string ModId)
{
    /// <summary>
    /// Gets the hash of the file this one replaced, or null when nothing was there. Only managed fixes (ADR-0013) replace
    /// files DAORework did not place, and removing the fix puts that file back.
    /// </summary>
    public string? BeforeSha256 { get; init; }
}
