using DAORework.Core.Common;

namespace DAORework.Core.Mods;

/// <summary>A snapshot of one mod folder. <see cref="SizeBytes"/> covers readable files only; <see cref="UnreadableFileCount"/> says how many were not.</summary>
public sealed class ModFolderSummary
{
    public required string Name { get; init; }

    public required string Path { get; init; }

    public bool Exists { get; init; }

    public int FileCount { get; init; }

    public int FolderCount { get; init; }

    public long SizeBytes { get; init; }

    public int UnreadableFileCount { get; init; }

    public string SizeDisplay => new ByteSize(SizeBytes).ToString();
}
