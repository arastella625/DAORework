using System.IO.Abstractions;
using DAORework.Application.Documents;
using DAORework.Core.Common;
using Microsoft.Extensions.Logging;

namespace DAORework.Infrastructure.KnownFolders;

/// <summary>
/// Looks for <c>BioWare\Dragon Age</c> on the fixed drives, in each root and in folders up to three deep. It reads
/// folder names only, skips the system's own folders and does not follow links (ADR-0032).
/// </summary>
public sealed class DriveDocumentsFolderSearch : IDocumentsFolderSearch
{
    // Three deep reaches D:\Users\Me\Documents, a profile kept on another drive.
    private const int MaxDepth = 3;

    private static readonly HashSet<string> _skippedAtDriveRoot = new(StringComparer.OrdinalIgnoreCase)
    {
        "Windows", "Program Files", "Program Files (x86)", "ProgramData", "$Recycle.Bin", "System Volume Information",
    };

    // Replaces the default, which would skip hidden and system folders: a moved Documents folder can be marked system.
    private static readonly EnumerationOptions _subfolders = new()
    {
        IgnoreInaccessible = true,
        AttributesToSkip = FileAttributes.ReparsePoint,
    };

    private readonly IFileSystem _fileSystem;
    private readonly ILogger<DriveDocumentsFolderSearch> _logger;

    public DriveDocumentsFolderSearch(IFileSystem fileSystem, ILogger<DriveDocumentsFolderSearch> logger)
    {
        _fileSystem = fileSystem;
        _logger = logger;
    }

    // Off the calling thread: walking a hard drive can take seconds, and the window calls this.
    public Task<IReadOnlyList<string>> FindAsync(CancellationToken cancellationToken = default) =>
        Task.Run<IReadOnlyList<string>>(() => Search(cancellationToken), cancellationToken);

    private List<string> Search(CancellationToken cancellationToken)
    {
        var found = new List<string>();
        foreach (var drive in _fileSystem.DriveInfo.GetDrives().Where(d => d.DriveType == DriveType.Fixed && d.IsReady))
        {
            Walk(drive.RootDirectory.FullName, 0, found, cancellationToken);
        }

        _logger.LogInformation("Drive search: found {Count} Dragon Age documents folder(s).", found.Count);
        return found.Order(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private void Walk(string folder, int depth, List<string> found, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var candidate = GamePaths.DefaultDocumentsDirectory(folder);
        if (_fileSystem.Directory.Exists(candidate))
        {
            found.Add(candidate);
        }

        if (depth == MaxDepth)
        {
            return;
        }

        foreach (var subfolder in Subfolders(folder, depth))
        {
            Walk(subfolder, depth + 1, found, cancellationToken);
        }
    }

    private List<string> Subfolders(string folder, int depth)
    {
        try
        {
            return _fileSystem.Directory.EnumerateDirectories(folder, "*", _subfolders)
                .Where(subfolder => depth > 0 || !_skippedAtDriveRoot.Contains(_fileSystem.Path.GetFileName(subfolder)))
                .ToList();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // A folder that cannot be listed is one place fewer to look, not a failed search.
            _logger.LogDebug(exception, "Drive search: could not list {Folder}.", folder);
            return [];
        }
    }
}
