using System.IO.Abstractions;
using DAORework.Core.Catalog;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Formats;
using DAORework.Core.Profiles;

namespace DAORework.Application.Catalog.Matchers;

/// <summary>Recognizes the 4 GB patch by what it does: the large-address-aware flag set in the game executable's header.</summary>
public sealed class LargeAddressAwareMatcher : IFingerprintMatcher
{
    private readonly IFileSystem _fileSystem;

    public LargeAddressAwareMatcher(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public FingerprintKind Kind => FingerprintKind.LargeAddressAware;

    public async Task<Result<IReadOnlyList<FingerprintMatch>>> MatchAsync(
        KnownItemFingerprint fingerprint,
        RuntimeProfile profile,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(fingerprint);
        ArgumentNullException.ThrowIfNull(profile);
        var path = profile.GameExecutablePath;
        if (string.IsNullOrWhiteSpace(path) || !_fileSystem.File.Exists(path))
        {
            return Result.Success<IReadOnlyList<FingerprintMatch>>([]);
        }

        var header = new byte[PortableExecutable.HeaderBytesNeeded];
        int read;
        try
        {
            await using var stream = _fileSystem.File.OpenRead(path);
            read = await stream.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false, cancellationToken);
        }
        catch (IOException exception)
        {
            return Unreadable(path, exception.Message);
        }
        catch (UnauthorizedAccessException exception)
        {
            return Unreadable(path, exception.Message);
        }

        if (!PortableExecutable.IsLargeAddressAware(header.AsSpan(0, read)).TryGetValue(out var enabled, out var error))
        {
            return Unreadable(path, error.Message);
        }

        return Result.Success<IReadOnlyList<FingerprintMatch>>(
            enabled ? [new FingerprintMatch($"large-address-aware flag in {Path.GetFileName(path)}")] : []);
    }

    private static Result<IReadOnlyList<FingerprintMatch>> Unreadable(string path, string reason) =>
        Result.Failure<IReadOnlyList<FingerprintMatch>>(new Error(DiagnosticCodes.KnownItemUnreadable, $"{path} could not be read ({reason})."));
}
