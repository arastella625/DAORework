using SharpCompress.Archives;
using SharpCompress.Common;
using SharpCompress.Readers;

namespace DAORework.Infrastructure.Mods;

/// <summary>
/// A <c>.7z</c> or <c>.rar</c> download (P2.5-04), read through SharpCompress. Its paths go through the same
/// <c>ModPath</c> check as a zip's before anything is written. A solid archive, and any <c>.7z</c>, is read front to
/// back once, because opening its files one at a time would decompress it again for each.
/// </summary>
internal sealed class CompressedModSource : ModSource
{
    private readonly IArchive _archive;
    private readonly Dictionary<string, IArchiveEntry> _entries;

    public CompressedModSource(string name, string sha256, Stream stream)
        : base(name, sha256)
    {
        _archive = ArchiveFactory.OpenArchive(stream, new ReaderOptions { LeaveStreamOpen = false });
        if (_archive.Type is not (ArchiveType.SevenZip or ArchiveType.Rar))
        {
            _archive.Dispose();
            throw new InvalidDataException($"it is a {_archive.Type} archive, not a .7z or .rar.");
        }

        _entries = _archive.Entries
            .Where(e => !e.IsDirectory && e.Key is not null)
            .GroupBy(e => Normalize(e.Key!), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
        Files = _entries.Keys.Order(StringComparer.Ordinal).ToList();
    }

    public override IReadOnlyList<string> Files { get; }

    public override long TotalBytes => _entries.Values.Sum(e => e.Size);

    public override Stream Open(string file) => _entries[file].OpenEntryStream();

    public override async Task CopyEachAsync(Func<string, Stream, Task> copy, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(copy);
        if (!_archive.IsSolid && _archive.Type != ArchiveType.SevenZip)
        {
            await base.CopyEachAsync(copy, cancellationToken);
            return;
        }

        // Two entries whose names differ only in case are one file, as in a zip: the first one is kept.
        var copied = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using var reader = _archive.ExtractAllEntries();
        while (reader.MoveToNextEntry())
        {
            cancellationToken.ThrowIfCancellationRequested();
            var file = reader.Entry.IsDirectory || reader.Entry.Key is null ? null : Normalize(reader.Entry.Key);
            if (file is null || !copied.Add(file))
            {
                continue;
            }

            await using var input = reader.OpenEntryStream();
            await copy(file, input);
        }
    }

    public override void Dispose()
    {
        _archive.Dispose();
        base.Dispose();
    }

    private static string Normalize(string key) => key.Replace('\\', '/');
}
