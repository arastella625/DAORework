namespace DAORework.Core.Mods.Downloads;

/// <summary>A file dealt with, known by its path, size and changed time, so no file is read to tell (ADR-0034 §5).</summary>
public sealed record HandledDownload(string Path, long SizeBytes, DateTimeOffset ChangedUtc)
{
    public static HandledDownload From(DownloadFile file)
    {
        ArgumentNullException.ThrowIfNull(file);
        return new HandledDownload(file.Path, file.SizeBytes, file.ChangedUtc);
    }

    public bool Is(DownloadFile file)
    {
        ArgumentNullException.ThrowIfNull(file);
        return string.Equals(Path, file.Path, StringComparison.OrdinalIgnoreCase) && SizeBytes == file.SizeBytes && ChangedUtc == file.ChangedUtc;
    }
}
