using System.IO.Abstractions;
using System.Security.Cryptography;
using System.Text;
using DAORework.Core.Mods.Adoption;

namespace DAORework.Infrastructure.Mods;

/// <summary>
/// A hand-installed mod as a source to import (ADR-0015): its files in the documents folder under the library paths
/// <see cref="AdoptionLayout"/> gives them, and for an add-in the manifest made from its <c>AddInItem</c>. Its identity
/// is the hash of the sorted list of library paths with each file's hash, as for an extracted folder.
/// </summary>
internal sealed class CandidateModSource : ModSource
{
    private readonly IFileSystem _fileSystem;
    private readonly Dictionary<string, string> _disk;
    private readonly byte[]? _manifest;

    private CandidateModSource(IFileSystem fileSystem, string name, string sha256, Dictionary<string, string> disk, byte[]? manifest)
        : base(name, sha256)
    {
        _fileSystem = fileSystem;
        _disk = disk;
        _manifest = manifest;
        Files = [.. disk.Keys.Concat(manifest is null ? [] : [AdoptionLayout.ManifestPath]).Order(StringComparer.Ordinal)];
    }

    public override IReadOnlyList<string> Files { get; }

    public override long TotalBytes => _disk.Values.Sum(p => _fileSystem.FileInfo.New(p).Length) + (_manifest?.Length ?? 0);

    /// <summary>Lists and hashes the candidate's files; <paramref name="manifest"/> is the add-in's <c>Manifest.xml</c>, or null for an override mod.</summary>
    public static async Task<CandidateModSource> CreateAsync(
        IFileSystem fileSystem,
        string documentsDirectory,
        AdoptionCandidate candidate,
        string name,
        string? manifest,
        CancellationToken cancellationToken)
    {
        var disk = AdoptionLayout.Sources(candidate).ToDictionary(s => s.Key, s => DocumentsPaths.Resolve(fileSystem, documentsDirectory, s.Value), StringComparer.Ordinal);
        var manifestBytes = manifest is null ? null : Encoding.UTF8.GetBytes(manifest);
        var listing = new StringBuilder();
        foreach (var (path, full) in disk.OrderBy(d => d.Key, StringComparer.Ordinal))
        {
            listing.Append(path).Append('\t').Append(await FileHashes.ComputeAsync(fileSystem, full, cancellationToken)).Append('\n');
        }

        if (manifestBytes is not null)
        {
            listing.Append(AdoptionLayout.ManifestPath).Append('\t').Append(Convert.ToHexStringLower(SHA256.HashData(manifestBytes))).Append('\n');
        }

        var sha = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(listing.ToString())));
        return new CandidateModSource(fileSystem, name, sha, disk, manifestBytes);
    }

    public override Stream Open(string file) =>
        file == AdoptionLayout.ManifestPath && _manifest is not null ? new MemoryStream(_manifest, writable: false) : _fileSystem.File.OpenRead(_disk[file]);
}
