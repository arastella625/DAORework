using System.IO.Abstractions;
using DAORework.Application.Abstractions;
using DAORework.Application.Documents;
using DAORework.Core.Diagnostics;
using DAORework.Core.Documents;
using DAORework.Core.Profiles;

namespace DAORework.Application.Diagnostics.Rules;

/// <summary>
/// On Linux the game's documents folder lives in its Proton prefix, which exists only once the game has been started, and
/// which Steam can delete with everything in it (ADR-0031). Says so plainly. DAORework never creates a prefix. Report only.
/// </summary>
public sealed class ProtonPrefixRule : IDiagnosticRule
{
    private readonly IFileSystem _fileSystem;
    private readonly IHostPlatform _platform;
    private readonly IProtonPrefixLocator _prefixes;

    public ProtonPrefixRule(IFileSystem fileSystem, IHostPlatform platform, IProtonPrefixLocator prefixes)
    {
        _fileSystem = fileSystem;
        _platform = platform;
        _prefixes = prefixes;
    }

    public Task<IReadOnlyList<DiagnosticIssue>> EvaluateAsync(RuntimeProfile profile, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(profile);
        IReadOnlyList<DiagnosticIssue> none = [];
        if (!_platform.IsLinux)
        {
            return Task.FromResult(none);
        }

        var documents = profile.UserDocumentsDirectory;
        if (!string.IsNullOrWhiteSpace(documents) && ProtonPaths.IsInPrefix(documents) && !_fileSystem.Directory.Exists(documents))
        {
            return Task.FromResult<IReadOnlyList<DiagnosticIssue>>([Missing($"The game's Proton prefix has gone, and the documents folder and mods in it with it: {documents}.")]);
        }

        var isSteam = !string.IsNullOrWhiteSpace(profile.InstallDirectory) && ProtonPaths.IsSteamInstall(profile.InstallDirectory);
        return Task.FromResult(isSteam && _prefixes.FindPrefixes().Count == 0
            ? [Missing("The game has no Proton prefix yet, so it has no documents folder.")]
            : none);
    }

    private static DiagnosticIssue Missing(string message) =>
        new(DiagnosticSeverity.Warning, DiagnosticCodes.ProtonPrefixMissing, message, "Start the game once from Steam, then check again.");
}
