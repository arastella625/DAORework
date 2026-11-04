using System.IO.Abstractions;
using DAORework.Application.Abstractions;
using DAORework.Application.Common;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Formats.Erf;
using Microsoft.Extensions.Options;

namespace DAORework.Infrastructure.Mods;

/// <summary>Lists the resources in a library mod's <c>.erf</c>, from its table of contents alone (ROADMAP P3-07).</summary>
public sealed class LibraryErfContents : IErfContentsReader
{
    private readonly IFileSystem _fileSystem;
    private readonly string _root;

    public LibraryErfContents(IFileSystem fileSystem, IOptions<DaoReworkOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _fileSystem = fileSystem;
        _root = _fileSystem.Path.GetFullPath(options.Value.LibraryDirectory);
    }

    public Result<IReadOnlyList<string>> ReadNames(string modId, string relativePath)
    {
        ArgumentNullException.ThrowIfNull(relativePath);
        var path = _fileSystem.Path.GetFullPath(_fileSystem.Path.Combine(_root, modId, relativePath.Replace('/', _fileSystem.Path.DirectorySeparatorChar)));
        if (!path.StartsWith(_root.TrimEnd(_fileSystem.Path.DirectorySeparatorChar) + _fileSystem.Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            return Failed($"{relativePath} of {modId} is not inside the library.");
        }

        try
        {
            using var stream = _fileSystem.File.OpenRead(path);
            return Result.Success<IReadOnlyList<string>>([.. ErfReader.ReadTableOfContents(stream).Entries.Select(e => e.Name)]);
        }
        catch (Exception exception) when (exception is InvalidDataException or IOException or UnauthorizedAccessException)
        {
            return Failed($"{relativePath} of {modId} couldn't be read: {exception.Message}");
        }
    }

    private static Result<IReadOnlyList<string>> Failed(string message) => Result.Failure<IReadOnlyList<string>>(new Error(DiagnosticCodes.ModErfUnreadable, message));
}
