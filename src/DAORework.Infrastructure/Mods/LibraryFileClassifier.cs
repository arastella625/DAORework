using System.IO.Abstractions;
using System.Text.Json;
using DAORework.Core.Mods.Library;
using DAORework.Core.Mods.Safety;
using DAORework.Infrastructure.Persistence;

namespace DAORework.Infrastructure.Mods;

/// <summary>
/// Classes a library file as data or a program (ADR-0018) from its name and its first two bytes, so a renamed
/// executable is caught. An entry written before <c>mod.json</c> schema 3 is classified once, when it is next read,
/// and saved again; nothing but its files' kinds changes.
/// </summary>
internal sealed class LibraryFileClassifier(IFileSystem fileSystem, string root)
{
    public async Task<ModFileKind> ClassifyAsync(string fullPath, string relativePath, CancellationToken cancellationToken)
    {
        var start = new byte[2];
        var read = 0;
        if (fileSystem.File.Exists(fullPath))
        {
            await using var stream = fileSystem.File.OpenRead(fullPath);
            read = await stream.ReadAtLeastAsync(start, start.Length, throwOnEndOfStream: false, cancellationToken);
        }

        return ProgramFiles.Classify(relativePath, start.AsSpan(0, read));
    }

    /// <summary>The entry with every unclassified file classified, saved to <paramref name="metadata"/> when anything changed.</summary>
    public async Task<LibraryEntry> ClassifyUnclassifiedAsync(LibraryEntry entry, string metadata, CancellationToken cancellationToken)
    {
        if (entry.Mod.Files.All(f => f.Kind != ModFileKind.Unclassified))
        {
            return entry;
        }

        var files = new List<LibraryModFile>();
        foreach (var file in entry.Mod.Files)
        {
            files.Add(file.Kind != ModFileKind.Unclassified ? file : file with { Kind = await ClassifyAsync(LibraryPath(entry.Mod.Id, file.RelativePath), file.RelativePath, cancellationToken) });
        }

        var classified = entry with { Mod = entry.Mod with { Files = files } };
        await AtomicWrite.TextAsync(fileSystem, metadata, JsonSerializer.Serialize(classified, JsonDefaults.Options), cancellationToken);
        return classified;
    }

    public string LibraryPath(string modId, string relativePath) =>
        fileSystem.Path.Combine(root, modId, relativePath.Replace('/', fileSystem.Path.DirectorySeparatorChar));
}
