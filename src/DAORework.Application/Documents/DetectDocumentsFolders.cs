using System.IO.Abstractions;
using DAORework.Application.Abstractions;
using DAORework.Core.Common;
using DAORework.Core.Documents;

namespace DAORework.Application.Documents;

/// <summary>
/// Finds where the Dragon Age documents folder is, or could be: the Documents folder Windows reports (what the game
/// uses), the classic <c>%UserProfile%\Documents</c>, and <c>%OneDrive%\Documents</c>. OneDrive's Known Folder Move
/// silently changes the first, which is how saves and mods end up "missing" after a redirection.
/// </summary>
public sealed class DetectDocumentsFolders
{
    private readonly IFileSystem _fileSystem;
    private readonly IKnownFolders _knownFolders;

    public DetectDocumentsFolders(IFileSystem fileSystem, IKnownFolders knownFolders)
    {
        _fileSystem = fileSystem;
        _knownFolders = knownFolders;
    }

    public Task<Result<DocumentsDetection>> ExecuteAsync(DetectDocumentsFoldersRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var candidates = Roots()
            .DistinctBy(r => r.Root, StringComparer.OrdinalIgnoreCase)
            .Select(r => Describe(r.Root, r.Source))
            .OrderByDescending(c => c.Exists)
            .ThenBy(c => c.Source)
            .ToList();

        return Task.FromResult(Result.Success(new DocumentsDetection(candidates)));
    }

    /// <summary>Whether a path lives under a OneDrive folder, read off the path itself.</summary>
    public static bool IsUnderOneDrive(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        // "OneDrive" for personal accounts, "OneDrive - <organisation>" for work and school accounts.
        return path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .Any(segment => segment.Equals("OneDrive", StringComparison.OrdinalIgnoreCase)
                || segment.StartsWith("OneDrive - ", StringComparison.OrdinalIgnoreCase));
    }

    // A list rather than an iterator: the compiler-generated iterator would touch System.Environment, which
    // the architecture tests forbid in this layer.
    private List<(string Root, DocumentsFolderSource Source)> Roots()
    {
        var roots = new List<(string Root, DocumentsFolderSource Source)>
        {
            (_knownFolders.MyDocuments, DocumentsFolderSource.KnownFolder),
            (Path.Combine(_knownFolders.UserProfile, "Documents"), DocumentsFolderSource.UserProfile),
        };

        if (!string.IsNullOrWhiteSpace(_knownFolders.OneDrive))
        {
            roots.Add((Path.Combine(_knownFolders.OneDrive, "Documents"), DocumentsFolderSource.OneDrive));
        }

        return roots;
    }

    private DocumentsFolder Describe(string documentsRoot, DocumentsFolderSource source)
    {
        var path = GamePaths.DefaultDocumentsDirectory(documentsRoot);
        return new DocumentsFolder(path, source, _fileSystem.Directory.Exists(path), IsUnderOneDrive(path));
    }
}
