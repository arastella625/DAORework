using System.IO.Abstractions;
using System.Text.Json;

namespace DAORework.Infrastructure.Persistence;

/// <summary>
/// Writes a JSON document to a <c>*.tmp</c> file next to its target and moves it into place
/// (SDD §8.4), so a crash or a failed serialization never leaves a half-written or empty document.
/// </summary>
public static class AtomicJsonFile
{
    /// <summary>Writes <paramref name="document"/> to <paramref name="path"/>, replacing any existing file.</summary>
    public static Task WriteAsync<T>(IFileSystem fileSystem, string path, T document, CancellationToken cancellationToken = default) =>
        WriteAsync(fileSystem, path, document, overwrite: true, cancellationToken);

    /// <summary>
    /// Writes <paramref name="document"/> to <paramref name="path"/>. With <paramref name="overwrite"/> false the
    /// move fails with an <see cref="IOException"/> when the target already exists, which is what makes
    /// report names collision-free.
    /// </summary>
    public static async Task WriteAsync<T>(IFileSystem fileSystem, string path, T document, bool overwrite, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(fileSystem);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var tempPath = $"{path}.{fileSystem.Path.GetRandomFileName()}.tmp";
        try
        {
            await using (var stream = fileSystem.FileStream.New(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                await JsonSerializer.SerializeAsync(stream, document, JsonDefaults.Options, cancellationToken);
                await stream.FlushAsync(cancellationToken);
            }

            fileSystem.File.Move(tempPath, path, overwrite);
        }
        finally
        {
            if (fileSystem.File.Exists(tempPath))
            {
                fileSystem.File.Delete(tempPath);
            }
        }
    }
}
