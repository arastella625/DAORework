namespace DAORework.Core.Mods.Downloads;

/// <summary>A finished file in the downloads folder (ADR-0034 §4), by what the folder says of it. Nothing has read its contents.</summary>
public sealed record DownloadFile(string Path, long SizeBytes, DateTimeOffset CreatedUtc, DateTimeOffset ChangedUtc)
{
    /// <summary>Gets the file's name, with any separators.</summary>
    public string FileName => Path[(Path.LastIndexOfAny(['/', '\\']) + 1)..];
}
