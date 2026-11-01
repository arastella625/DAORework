using System.IO.Abstractions;
using System.Security.Cryptography;
using System.Text;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Mods.Combining;
using DAORework.Core.Mods.Library;

namespace DAORework.Infrastructure.Mods;

/// <summary>
/// Reading a library mod's file as text, and keeping combined files (ADR-0025) in <c>library/_combined/&lt;sha256&gt;/</c>:
/// each version once, never overwritten, so a backup or an interrupted change always finds the one it wrote.
/// </summary>
internal sealed class LibraryCombinedFiles(IFileSystem fileSystem, string root)
{
    /// <summary>The largest file read as text: far past any real <c>chargenmorphcfg.xml</c>.</summary>
    private const long MaxTextBytes = 16 * 1024 * 1024;

    public async Task<Result<string>> ReadTextAsync(string modId, string relativePath, CancellationToken cancellationToken)
    {
        var path = fileSystem.Path.GetFullPath(fileSystem.Path.Combine(root, modId, relativePath.Replace('/', fileSystem.Path.DirectorySeparatorChar)));
        var inside = fileSystem.Path.GetFullPath(root).TrimEnd(fileSystem.Path.DirectorySeparatorChar) + fileSystem.Path.DirectorySeparatorChar;
        if (!path.StartsWith(inside, StringComparison.OrdinalIgnoreCase))
        {
            return Failed($"{relativePath} of {modId} is not inside the library.");
        }

        try
        {
            if (fileSystem.FileInfo.New(path).Length > MaxTextBytes)
            {
                return Failed($"{relativePath} of {modId} is larger than {new ByteSize(MaxTextBytes)}.");
            }

            return Result.Success(await fileSystem.File.ReadAllTextAsync(path, cancellationToken));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return Failed($"{relativePath} of {modId} could not be read from the library: {exception.Message}");
        }
    }

    public async Task<Result<LibraryModFile>> StoreAsync(string fileName, string text, CancellationToken cancellationToken)
    {
        var bytes = new UTF8Encoding(false).GetBytes(text);
        var sha = Convert.ToHexStringLower(SHA256.HashData(bytes));
        var folder = fileSystem.Path.Combine(root, CombinedFiles.ModId, sha);
        var path = fileSystem.Path.Combine(folder, fileName);
        try
        {
            if (!fileSystem.File.Exists(path) || !FileHashes.Same(await FileHashes.ComputeAsync(fileSystem, path, cancellationToken), sha))
            {
                fileSystem.Directory.CreateDirectory(folder);
                var temp = path + ".tmp";
                await fileSystem.File.WriteAllBytesAsync(temp, bytes, cancellationToken);
                fileSystem.File.Move(temp, path, overwrite: true);
            }

            return Result.Success(new LibraryModFile($"{sha}/{fileName}", bytes.Length, sha));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return Result.Failure<LibraryModFile>(new Error(DiagnosticCodes.ModImportFailed, $"The combined {fileName} could not be kept in the library: {exception.Message}"));
        }
    }

    private static Result<string> Failed(string message) => Result.Failure<string>(new Error(DiagnosticCodes.FileReadFailed, message));
}
