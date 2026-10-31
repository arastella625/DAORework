using System.IO.Compression;

namespace DAORework.Infrastructure.Mods;

/// <summary>A <c>.zip</c> or <c>.dazip</c>, from disk or from inside another zip.</summary>
internal sealed class ZipModSource : ModSource
{
    private readonly ZipArchive _zip;
    private readonly Stream _stream;
    private readonly Dictionary<string, ZipArchiveEntry> _entries;

    public ZipModSource(string name, string sha256, Stream stream)
        : base(name, sha256)
    {
        _stream = stream;
        _zip = new ZipArchive(stream, ZipArchiveMode.Read);
        _entries = _zip.Entries
            .Where(e => !e.FullName.EndsWith('/') && !e.FullName.EndsWith('\\'))
            .GroupBy(e => e.FullName.Replace('\\', '/'), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
        Files = _entries.Keys.Order(StringComparer.Ordinal).ToList();
    }

    public override IReadOnlyList<string> Files { get; }

    public override long TotalBytes => _entries.Values.Sum(e => e.Length);

    public override Stream Open(string file) => _entries[file].Open();

    public override void Dispose()
    {
        _zip.Dispose();
        _stream.Dispose();
        base.Dispose();
    }
}
