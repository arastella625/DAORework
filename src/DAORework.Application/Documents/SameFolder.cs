using System.IO.Abstractions;
using Microsoft.Extensions.Logging;

namespace DAORework.Application.Documents;

/// <summary>
/// Whether two paths are one folder: compared without letter case or a trailing separator, after every junction or
/// symbolic link along each is followed (ADR-0046). The documents rules share it, so they never disagree about it. A
/// relative path is compared as written: resolving it would depend on the folder DAORework was started from.
/// </summary>
public sealed class SameFolder
{
    // Enough for any real setup; a loop of links stops here instead of recursing forever.
    private const int MaxLinks = 16;

    private readonly IFileSystem _fileSystem;
    private readonly ILogger<SameFolder> _logger;

    public SameFolder(IFileSystem fileSystem, ILogger<SameFolder> logger)
    {
        _fileSystem = fileSystem;
        _logger = logger;
    }

    public bool AreSame(string left, string right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        return AsWritten(RealPath(left), RealPath(right));
    }

    private static bool AsWritten(string left, string right) =>
        string.Equals(
            left.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
            right.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
            StringComparison.OrdinalIgnoreCase);

    /// <summary>The folder a path finally reaches, with every junction or symbolic link along it followed.</summary>
    private string RealPath(string path)
    {
        if (!_fileSystem.Path.IsPathRooted(path))
        {
            return path;
        }

        try
        {
            return Follow(_fileSystem.Path.GetFullPath(path), 0);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // A link that can't be read compares as written: two paths only count as one folder when that is known.
            _logger.LogWarning(exception, "Couldn't follow the links in a documents folder; comparing it as written.");
            return path;
        }
    }

    private string Follow(string path, int links)
    {
        var parent = _fileSystem.Path.GetDirectoryName(path);
        var real = string.IsNullOrEmpty(parent) ? path : _fileSystem.Path.Combine(Follow(parent, links), _fileSystem.Path.GetFileName(path));
        var folder = _fileSystem.DirectoryInfo.New(real);

        // LinkTarget is null for a plain folder; asking a plain folder to resolve throws in some file systems.
        var target = links < MaxLinks && folder.Exists && folder.LinkTarget is not null
            ? folder.ResolveLinkTarget(returnFinalTarget: true)
            : null;
        return target is null ? real : Follow(_fileSystem.Path.GetFullPath(target.FullName), links + 1);
    }
}
