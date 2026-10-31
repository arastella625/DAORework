using System.IO.Abstractions;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Profiles;

namespace DAORework.Application.Diagnostics.Rules;

/// <summary>Reports whether the override and AddIns folders exist under an existing documents directory. Both states are normal.</summary>
public sealed class ModFoldersRule : IDiagnosticRule
{
    private readonly IFileSystem _fileSystem;

    public ModFoldersRule(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public Task<IReadOnlyList<DiagnosticIssue>> EvaluateAsync(RuntimeProfile profile, CancellationToken cancellationToken = default)
    {
        var documents = profile.UserDocumentsDirectory;

        if (string.IsNullOrWhiteSpace(documents) || !_fileSystem.Directory.Exists(documents))
        {
            return Task.FromResult<IReadOnlyList<DiagnosticIssue>>([]);
        }

        IReadOnlyList<DiagnosticIssue> issues =
        [
            Folder(
                GamePaths.OverrideDirectory(documents),
                DiagnosticCodes.OverrideDirectoryFound,
                DiagnosticCodes.OverrideDirectoryNotFound,
                "Override folder exists.",
                "Override folder was not found. This is normal for a vanilla install."),
            Folder(
                GamePaths.AddInsDirectory(documents),
                DiagnosticCodes.AddInsDirectoryFound,
                DiagnosticCodes.AddInsDirectoryNotFound,
                "AddIns folder exists.",
                "AddIns folder was not found. This is normal if no add-in mods are installed."),
        ];

        return Task.FromResult(issues);
    }

    private DiagnosticIssue Folder(string path, string foundCode, string notFoundCode, string foundMessage, string notFoundMessage) =>
        _fileSystem.Directory.Exists(path)
            ? new DiagnosticIssue(DiagnosticSeverity.Info, foundCode, foundMessage)
            : new DiagnosticIssue(DiagnosticSeverity.Warning, notFoundCode, notFoundMessage);
}
