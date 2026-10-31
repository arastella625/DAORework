using DAORework.Core.Common;

namespace DAORework.Core.Mods;

public sealed class ModInventory
{
    /// <summary>The schema of the persisted document (SDD §8.4). Bump it together with a migrator.</summary>
    public const int CurrentSchemaVersion = 1;

    public int SchemaVersion { get; init; } = CurrentSchemaVersion;

    public required DateTimeOffset CreatedAtUtc { get; init; }

    public required string ProfileName { get; init; }

    public required string UserDocumentsDirectory { get; init; }

    public required bool IncludesHashes { get; init; }

    public required IReadOnlyList<ModFile> Items { get; init; }

    public int TotalFiles => Items.Count;

    public int UnreadableFileCount => Items.Count(x => x.ReadError is not null);

    public long TotalSizeBytes => Items.Sum(x => x.SizeBytes);

    public string TotalSizeDisplay => new ByteSize(TotalSizeBytes).ToString();

    public IReadOnlyList<string> DuplicateFileNames => Duplicates(x => x.FileName);

    public IReadOnlyList<string> DuplicateRelativePaths => Duplicates(x => x.RelativePath);

    private List<string> Duplicates(Func<ModFile, string> key) =>
        Items
            .GroupBy(key, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();
}
