using System.IO.Abstractions;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Profiles;

namespace DAORework.Application.Diagnostics.Rules;

/// <summary>Save-folder health: how many saves there are, whether any is empty (a failed save), and whether there are so many that loading slows down.</summary>
public sealed class SavesRule : IDiagnosticRule
{
    /// <summary>The game lists every save on the load screen; beyond this many the menus crawl.</summary>
    public const int ManySavesThreshold = 250;

    private const string SaveExtension = ".das";

    private readonly IFileSystem _fileSystem;

    public SavesRule(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public Task<IReadOnlyList<DiagnosticIssue>> EvaluateAsync(RuntimeProfile profile, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(profile);
        var documents = profile.UserDocumentsDirectory;
        if (string.IsNullOrWhiteSpace(documents) || !_fileSystem.Directory.Exists(documents))
        {
            return Task.FromResult<IReadOnlyList<DiagnosticIssue>>([]);
        }

        var characters = GamePaths.CharactersDirectory(documents);
        if (!_fileSystem.Directory.Exists(characters))
        {
            return Task.FromResult<IReadOnlyList<DiagnosticIssue>>(
                [new DiagnosticIssue(DiagnosticSeverity.Info, DiagnosticCodes.SavesNone, "No saved games yet (no Characters folder).")]);
        }

        return Task.FromResult<IReadOnlyList<DiagnosticIssue>>(Inspect(characters, cancellationToken));
    }

    private List<DiagnosticIssue> Inspect(string characters, CancellationToken cancellationToken)
    {
        var characterCount = _fileSystem.Directory.EnumerateDirectories(characters).Count();
        var saves = _fileSystem.Directory.EnumerateFiles(characters, "*" + SaveExtension, SearchOption.AllDirectories)
            .Select(path => _fileSystem.FileInfo.New(path))
            .ToList();
        cancellationToken.ThrowIfCancellationRequested();

        if (saves.Count == 0)
        {
            return [new DiagnosticIssue(DiagnosticSeverity.Info, DiagnosticCodes.SavesNone, "No saved games yet.")];
        }

        var size = new ByteSize(saves.Sum(s => s.Length));
        var issues = new List<DiagnosticIssue>
        {
            new(DiagnosticSeverity.Info, DiagnosticCodes.SavesFound, $"{saves.Count} saved game(s) for {characterCount} character(s), {size} in total."),
        };

        foreach (var empty in saves.Where(s => s.Length == 0))
        {
            issues.Add(new DiagnosticIssue(
                DiagnosticSeverity.Warning,
                DiagnosticCodes.SavesEmptyFile,
                $"Empty save file: {empty.FullName}",
                "The game did not finish writing this save. Load an earlier one and delete this file."));
        }

        if (saves.Count > ManySavesThreshold)
        {
            issues.Add(new DiagnosticIssue(
                DiagnosticSeverity.Warning,
                DiagnosticCodes.SavesMany,
                $"{saves.Count} saved games; the load screen slows down past about {ManySavesThreshold}.",
                "Back up and delete old saves (a later DAORework version backs them up for you)."));
        }

        return issues;
    }
}
