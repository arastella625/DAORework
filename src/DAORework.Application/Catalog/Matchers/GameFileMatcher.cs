using System.IO.Abstractions;
using DAORework.Core.Catalog;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Formats;
using DAORework.Core.Profiles;

namespace DAORework.Application.Catalog.Matchers;

/// <summary>
/// Finds a file in the install directory, such as DXVK's <c>bin_ship\d3d9.dll</c>. With a marker the file must also
/// contain it, and the string after it is the version; a file of the same name without the marker is someone else's
/// (another Direct3D wrapper, say) and is reported as unrecognized rather than as a match. A Windows binary that is
/// not 32-bit is flagged, because the game cannot load it.
/// </summary>
public sealed class GameFileMatcher : IFingerprintMatcher
{
    /// <summary>The largest file searched for a marker; DXVK's d3d9.dll is under 10 MB.</summary>
    public const long MaxMarkerFileBytes = 64L * 1024 * 1024;

    private readonly IFileSystem _fileSystem;

    public GameFileMatcher(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public FingerprintKind Kind => FingerprintKind.GameFile;

    public async Task<Result<IReadOnlyList<FingerprintMatch>>> MatchAsync(
        KnownItemFingerprint fingerprint,
        RuntimeProfile profile,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(fingerprint);
        ArgumentNullException.ThrowIfNull(profile);
        var install = profile.InstallDirectory;
        var relative = fingerprint.Value.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
        if (string.IsNullOrWhiteSpace(install) || !_fileSystem.File.Exists(Path.Combine(install, relative)))
        {
            return Result.Success<IReadOnlyList<FingerprintMatch>>([]);
        }

        var path = Path.Combine(install, relative);
        if (fingerprint.Marker is not null && _fileSystem.FileInfo.New(path).Length > MaxMarkerFileBytes)
        {
            return Failure(DiagnosticCodes.KnownItemUnrecognized, $"{relative} is larger than {MaxMarkerFileBytes / (1024 * 1024)} MB, too large to be this item's file.");
        }

        byte[] data;
        try
        {
            data = await ReadAsync(path, fingerprint.Marker is not null, cancellationToken);
        }
        catch (IOException exception)
        {
            return Failure(DiagnosticCodes.KnownItemUnreadable, $"{relative} could not be read ({exception.Message}).");
        }
        catch (UnauthorizedAccessException exception)
        {
            return Failure(DiagnosticCodes.KnownItemUnreadable, $"{relative} could not be read ({exception.Message}).");
        }

        string? version = null;
        if (fingerprint.Marker is not null && !BinaryMarker.TryFind(data, fingerprint.Marker, out version))
        {
            return Failure(DiagnosticCodes.KnownItemUnrecognized, $"{relative} is there but is not this item's file (another mod or wrapper uses the same name).");
        }

        return Result.Success<IReadOnlyList<FingerprintMatch>>([new FingerprintMatch(relative, version, WrongArchitecture(data, relative))]);
    }

    private async Task<byte[]> ReadAsync(string path, bool wholeFile, CancellationToken cancellationToken)
    {
        if (wholeFile)
        {
            return await _fileSystem.File.ReadAllBytesAsync(path, cancellationToken);
        }

        var header = new byte[PortableExecutable.HeaderBytesNeeded];
        await using var stream = _fileSystem.File.OpenRead(path);
        var read = await stream.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false, cancellationToken);
        return header[..read];
    }

    /// <summary>Describes a Windows binary the 32-bit game cannot load; null for a 32-bit binary or a file that is not one.</summary>
    private static string? WrongArchitecture(byte[] data, string relative)
    {
        if (!PortableExecutableMachine.Read(data).TryGetValue(out var machine, out _) || machine == PortableExecutableMachine.X86)
        {
            return null;
        }

        return $"{relative} is a {PortableExecutableMachine.Describe(machine)} build; the game is 32-bit and cannot load it.";
    }

    private static Result<IReadOnlyList<FingerprintMatch>> Failure(string code, string message) =>
        Result.Failure<IReadOnlyList<FingerprintMatch>>(new Error(code, message));
}
