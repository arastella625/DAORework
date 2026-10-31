using System.IO.Abstractions;
using System.Security.Cryptography;
using System.Text;

namespace DAORework.Infrastructure.Mods;

/// <summary>An extracted mod folder; its identity is the hash of its sorted file list with each file's own hash.</summary>
internal sealed class FolderModSource : ModSource
{
    private readonly IFileSystem _fileSystem;
    private readonly string _root;

    private FolderModSource(IFileSystem fileSystem, string root, string sha256, IReadOnlyList<string> files)
        : base(fileSystem.Path.GetFileName(root.TrimEnd('/', '\\')), sha256)
    {
        _fileSystem = fileSystem;
        _root = root;
        Files = files;
    }

    public override IReadOnlyList<string> Files { get; }

    public override long TotalBytes => Files.Sum(f => _fileSystem.FileInfo.New(Full(f)).Length);

    public static async Task<FolderModSource> CreateAsync(IFileSystem fileSystem, string root, CancellationToken cancellationToken)
    {
        var files = fileSystem.Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
            .Select(f => fileSystem.Path.GetRelativePath(root, f).Replace('\\', '/'))
            .Order(StringComparer.Ordinal)
            .ToList();
        var listing = new StringBuilder();
        foreach (var file in files)
        {
            listing.Append(file).Append('\t').Append(await FileHashes.ComputeAsync(fileSystem, fileSystem.Path.Combine(root, file), cancellationToken)).Append('\n');
        }

        var sha = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(listing.ToString())));
        return new FolderModSource(fileSystem, root, sha, files);
    }

    public override Stream Open(string file) => _fileSystem.File.OpenRead(Full(file));

    private string Full(string file) => _fileSystem.Path.Combine(_root, file.Replace('/', _fileSystem.Path.DirectorySeparatorChar));
}
