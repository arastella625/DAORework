using System.IO.Abstractions;
using DAORework.Application.Abstractions;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Profiles;
using Microsoft.Extensions.Logging;

namespace DAORework.Application.Diagnostics.Rules;

/// <summary>
/// The game reads only the documents folder inside the Documents folder Windows reports (ADR-0046). When the profile
/// names another and the game's exists, the mods DAORework turns on never reach the game. Says so, naming both folders.
/// A junction or symbolic link on either path counts as the folder it points at. Moves and links nothing. Not on Linux,
/// where the game reads its Proton prefix (ADR-0031). Report only.
/// </summary>
public sealed class GameDocumentsFolderRule : IDiagnosticRule
{
    // Enough for any real setup; a loop of links stops here instead of recursing forever.
    private const int MaxLinks = 16;

    private readonly IFileSystem _fileSystem;
    private readonly IKnownFolders _knownFolders;
    private readonly IHostPlatform _platform;
    private readonly ILogger<GameDocumentsFolderRule> _logger;

    public GameDocumentsFolderRule(IFileSystem fileSystem, IKnownFolders knownFolders, IHostPlatform platform, ILogger<GameDocumentsFolderRule> logger)
    {
        _fileSystem = fileSystem;
        _knownFolders = knownFolders;
        _platform = platform;
        _logger = logger;
    }

    public Task<IReadOnlyList<DiagnosticIssue>> EvaluateAsync(RuntimeProfile profile, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(profile);
        var configured = profile.UserDocumentsDirectory;
        if (_platform.IsLinux || string.IsNullOrWhiteSpace(configured) || string.IsNullOrWhiteSpace(_knownFolders.MyDocuments))
        {
            return Task.FromResult<IReadOnlyList<DiagnosticIssue>>([]);
        }

        var games = GamePaths.DefaultDocumentsDirectory(_knownFolders.MyDocuments);
        IReadOnlyList<DiagnosticIssue> issues = _fileSystem.Directory.Exists(games) && !SamePath(RealPath(games), RealPath(configured))
            ?
            [
                new DiagnosticIssue(
                    DiagnosticSeverity.Warning,
                    DiagnosticCodes.DocumentsNotTheGames,
                    $"The game uses {games}, but this profile puts mods in {configured}, so the mods you turn on don't reach the game.",
                    $"Choose the game's folder in Settings > Game folders, move your saves and mods into it, or make the game's folder a link to yours. From the command line: profile set-path \"{profile.Name}\" documents \"{games}\""),
            ]
            : [];

        return Task.FromResult(issues);
    }

    private static bool SamePath(string left, string right) =>
        string.Equals(
            left.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
            right.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
            StringComparison.OrdinalIgnoreCase);

    /// <summary>The folder a path finally reaches, with every junction or symbolic link along it followed.</summary>
    private string RealPath(string path)
    {
        try
        {
            return Follow(_fileSystem.Path.GetFullPath(path), 0);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // A link that can't be read compares as written, which can only make the warning show, never hide it.
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
