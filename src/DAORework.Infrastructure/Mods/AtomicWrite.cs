using System.IO.Abstractions;

namespace DAORework.Infrastructure.Mods;

/// <summary>Writes a whole file through a temporary file next to it, so readers see the old content or the new, never half.</summary>
internal static class AtomicWrite
{
    public static async Task TextAsync(IFileSystem fileSystem, string path, string content, CancellationToken cancellationToken)
    {
        var temp = path + ".tmp";
        await fileSystem.File.WriteAllTextAsync(temp, content, cancellationToken);
        fileSystem.File.Move(temp, path, overwrite: true);
    }
}
