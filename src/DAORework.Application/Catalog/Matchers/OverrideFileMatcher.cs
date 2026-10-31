using System.IO.Abstractions;
using DAORework.Core.Catalog;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Profiles;

namespace DAORework.Application.Catalog.Matchers;

/// <summary>
/// Finds an override mod by a file it ships, anywhere under <c>packages\core\override</c>: players put mods in
/// subfolders of their own naming, so only the file name is fixed.
/// </summary>
public sealed class OverrideFileMatcher : IFingerprintMatcher
{
    private readonly IFileSystem _fileSystem;

    public OverrideFileMatcher(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public FingerprintKind Kind => FingerprintKind.OverrideFile;

    public Task<Result<IReadOnlyList<FingerprintMatch>>> MatchAsync(
        KnownItemFingerprint fingerprint,
        RuntimeProfile profile,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(fingerprint);
        ArgumentNullException.ThrowIfNull(profile);
        var documents = profile.UserDocumentsDirectory;
        if (string.IsNullOrWhiteSpace(documents) || !_fileSystem.Directory.Exists(GamePaths.OverrideDirectory(documents)))
        {
            return Task.FromResult(Result.Success<IReadOnlyList<FingerprintMatch>>([]));
        }

        return Task.FromResult(Match(documents, Path.GetFileName(fingerprint.Value)));
    }

    private Result<IReadOnlyList<FingerprintMatch>> Match(string documents, string fileName)
    {
        var directory = GamePaths.OverrideDirectory(documents);
        try
        {
            IReadOnlyList<FingerprintMatch> matches = _fileSystem.Directory
                .EnumerateFiles(directory, fileName, SearchOption.AllDirectories)
                .Where(path => string.Equals(Path.GetFileName(path), fileName, StringComparison.OrdinalIgnoreCase))
                .Select(path => new FingerprintMatch(Path.GetRelativePath(documents, path)))
                .ToList();
            return Result.Success(matches);
        }
        catch (IOException exception)
        {
            return Unreadable(directory, exception.Message);
        }
        catch (UnauthorizedAccessException exception)
        {
            return Unreadable(directory, exception.Message);
        }
    }

    private static Result<IReadOnlyList<FingerprintMatch>> Unreadable(string path, string reason) =>
        Result.Failure<IReadOnlyList<FingerprintMatch>>(new Error(DiagnosticCodes.KnownItemUnreadable, $"{path} could not be searched ({reason})."));
}
