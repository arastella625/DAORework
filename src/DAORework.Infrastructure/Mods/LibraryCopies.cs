using System.IO.Abstractions;
using DAORework.Application.Abstractions;
using DAORework.Core.Mods.Planning;

namespace DAORework.Infrastructure.Mods;

/// <summary>
/// The Library as a source of files for one backup or one restore (ADR-0015, ADR-0045). A library file never changes after
/// import, so its hash can stand in for a saved copy. Each copy is read before it is trusted, and what was found is
/// remembered only for this one job, since the Library can change between jobs.
/// </summary>
internal sealed class LibraryCopies(IFileSystem fileSystem, IModLibrary library, string libraryRoot)
{
    private readonly Dictionary<string, string?> _found = new(StringComparer.OrdinalIgnoreCase);
    private ILookup<string, string>? _byHash;

    /// <summary>The targets a backup can leave to the Library: files DAORework placed, still recorded with that hash, with a good copy there.</summary>
    public async Task<IReadOnlySet<string>> HeldAsync(IReadOnlyList<BackupTarget> targets, DeploymentRecord? record, CancellationToken cancellationToken)
    {
        var held = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var placed = (record?.Files ?? []).Select(f => Key(f.TargetPath, f.Sha256)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var target in targets)
        {
            if (target.ExpectedSha256 is { } sha && placed.Contains(Key(target.TargetPath, sha)) && await FindAsync(sha, cancellationToken) is not null)
            {
                held.Add(target.TargetPath);
            }
        }

        return held;
    }

    /// <summary>A file in the library with this SHA-256, read to be sure, or null when there is none.</summary>
    public async Task<string?> FindAsync(string sha256, CancellationToken cancellationToken)
    {
        if (_found.TryGetValue(sha256, out var known))
        {
            return known;
        }

        _byHash ??= await IndexAsync(cancellationToken);
        string? found = null;
        foreach (var path in _byHash[sha256].Where(fileSystem.File.Exists))
        {
            if (FileHashes.Same(await FileHashes.ComputeAsync(fileSystem, path, cancellationToken), sha256))
            {
                found = path;
                break;
            }
        }

        _found[sha256] = found;
        return found;
    }

    private static string Key(string targetPath, string sha256) => $"{targetPath}|{sha256}";

    private async Task<ILookup<string, string>> IndexAsync(CancellationToken cancellationToken)
    {
        var entries = await library.ListAsync(cancellationToken);
        return entries
            .SelectMany(e => e.Mod.Files.Select(f => (f.Sha256, Path: fileSystem.Path.Combine(libraryRoot, e.Mod.Id, f.RelativePath.Replace('/', fileSystem.Path.DirectorySeparatorChar)))))
            .ToLookup(f => f.Sha256, f => f.Path, StringComparer.OrdinalIgnoreCase);
    }
}
