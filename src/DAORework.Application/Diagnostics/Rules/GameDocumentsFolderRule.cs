using System.IO.Abstractions;
using DAORework.Application.Abstractions;
using DAORework.Application.Documents;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Profiles;

namespace DAORework.Application.Diagnostics.Rules;

/// <summary>
/// The game reads only the documents folder inside the Documents folder Windows reports (ADR-0046). When the profile
/// names another and the game's exists, the mods DAORework turns on never reach the game. Says so, naming both folders.
/// A junction or symbolic link on either path counts as the folder it points at (<see cref="SameFolder"/>). Moves and links nothing. Not on Linux,
/// where the game reads its Proton prefix (ADR-0031). Report only.
/// </summary>
public sealed class GameDocumentsFolderRule : IDiagnosticRule
{
    private readonly IFileSystem _fileSystem;
    private readonly IKnownFolders _knownFolders;
    private readonly IHostPlatform _platform;
    private readonly SameFolder _sameFolder;

    public GameDocumentsFolderRule(IFileSystem fileSystem, IKnownFolders knownFolders, IHostPlatform platform, SameFolder sameFolder)
    {
        _fileSystem = fileSystem;
        _knownFolders = knownFolders;
        _platform = platform;
        _sameFolder = sameFolder;
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
        IReadOnlyList<DiagnosticIssue> issues = _fileSystem.Directory.Exists(games) && !_sameFolder.AreSame(games, configured)
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
}
