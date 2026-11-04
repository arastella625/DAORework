using System.IO.Abstractions;
using DAORework.Application.Abstractions;
using DAORework.Core.Common;
using DAORework.Core.Documents;

namespace DAORework.Application.Documents;

/// <summary>
/// Finds where the Dragon Age documents folder is, or could be: the Documents folder Windows reports (what the game
/// uses), the classic <c>%UserProfile%\Documents</c>, and <c>%OneDrive%\Documents</c>. OneDrive's Known Folder Move
/// silently changes the first, which is how saves and mods end up "missing" after a redirection. The folder can also
/// be anywhere else, so the folders the profiles name come next, then the game's Proton prefix on Linux (ADR-0031), and
/// a search of the drives last (ADR-0032).
/// </summary>
public sealed class DetectDocumentsFolders
{
    private readonly IFileSystem _fileSystem;
    private readonly IKnownFolders _knownFolders;
    private readonly ProfileDocumentsFolders _profileFolders;
    private readonly IDocumentsFolderSearch _search;
    private readonly IProtonPrefixLocator _prefixes;

    public DetectDocumentsFolders(
        IFileSystem fileSystem,
        IKnownFolders knownFolders,
        ProfileDocumentsFolders profileFolders,
        IDocumentsFolderSearch search,
        IProtonPrefixLocator prefixes)
    {
        _prefixes = prefixes;
        _fileSystem = fileSystem;
        _knownFolders = knownFolders;
        _profileFolders = profileFolders;
        _search = search;
    }

    public async Task<Result<DocumentsDetection>> ExecuteAsync(DetectDocumentsFoldersRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var candidates = Roots().ConvertAll(r => Describe(GamePaths.DefaultDocumentsDirectory(r.Root), r.Source));

        // Only ones that exist: unlike the usual places, the game will never create a folder a profile names.
        candidates.AddRange((await _profileFolders.ReadAsync(cancellationToken)).Select(path => Describe(path, DocumentsFolderSource.Profile)).Where(c => c.Exists));

        candidates.AddRange(_prefixes.FindDocumentsFolders().Select(path => Describe(path, DocumentsFolderSource.ProtonPrefix)));

        // Detection runs on every diagnose, so the drives are walked only when nothing cheaper found a folder.
        if (!candidates.Exists(c => c.Exists))
        {
            candidates.AddRange((await _search.FindAsync(cancellationToken)).Select(path => Describe(path, DocumentsFolderSource.DriveSearch)));
        }

        var ordered = candidates
            .DistinctBy(c => c.Path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(c => c.Exists)
            .ThenBy(c => c.Source)
            .ToList();

        return Result.Success(new DocumentsDetection(ordered));
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
        var roots = new List<(string Root, DocumentsFolderSource Source)>();
        if (!string.IsNullOrWhiteSpace(_knownFolders.MyDocuments))
        {
            roots.Add((_knownFolders.MyDocuments, DocumentsFolderSource.KnownFolder));
        }

        if (!string.IsNullOrWhiteSpace(_knownFolders.UserProfile))
        {
            roots.Add((Path.Combine(_knownFolders.UserProfile, "Documents"), DocumentsFolderSource.UserProfile));
        }

        if (!string.IsNullOrWhiteSpace(_knownFolders.OneDrive))
        {
            roots.Add((Path.Combine(_knownFolders.OneDrive, "Documents"), DocumentsFolderSource.OneDrive));
        }

        return roots;
    }

    private DocumentsFolder Describe(string path, DocumentsFolderSource source) =>
        new(path, source, _fileSystem.Directory.Exists(path), IsUnderOneDrive(path));
}
