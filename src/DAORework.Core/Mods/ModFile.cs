namespace DAORework.Core.Mods;

/// <summary>One file in a mod folder. When <see cref="ReadError"/> is set, the size and hash could not be read and are not to be trusted.</summary>
public sealed record ModFile
{
    public required string SourceFolder { get; init; }

    public required string RootPath { get; init; }

    public required string FullPath { get; init; }

    public required string RelativePath { get; init; }

    public required string FileName { get; init; }

    public required string Extension { get; init; }

    public long SizeBytes { get; init; }

    public DateTimeOffset LastModifiedUtc { get; init; }

    public string? Sha256Hash { get; init; }

    public string? ReadError { get; init; }
}
