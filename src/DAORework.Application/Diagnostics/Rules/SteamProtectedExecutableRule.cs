using System.IO.Abstractions;
using DAORework.Core.Diagnostics;
using DAORework.Core.Formats;
using DAORework.Core.Profiles;

namespace DAORework.Application.Diagnostics.Rules;

/// <summary>
/// Steam's copy of daorigins.exe is protected by SteamStub, and Steam refuses to start it once its bytes change
/// (ADR-0023). Says so for an untouched copy, which the 4 GB patch is not applied to, and warns when the copy carries the
/// 4 GB flag anyway, because Steam then stops the game with "Application load error 3:0000065432".
/// </summary>
public sealed class SteamProtectedExecutableRule : IDiagnosticRule
{
    private readonly IFileSystem _fileSystem;

    public SteamProtectedExecutableRule(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public async Task<IReadOnlyList<DiagnosticIssue>> EvaluateAsync(RuntimeProfile profile, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(profile);
        var path = profile.GameExecutablePath;
        if (string.IsNullOrWhiteSpace(path) || !_fileSystem.File.Exists(path))
        {
            return [];
        }

        var header = new byte[PortableExecutable.HeaderBytesNeeded];
        int read;
        try
        {
            await using var stream = _fileSystem.File.OpenRead(path);
            read = await stream.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false, cancellationToken);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return []; // LargeAddressAwareRule reports an unreadable executable.
        }

        var bytes = header.AsSpan(0, read);
        if (!SteamStub.IsProtected(bytes))
        {
            return [];
        }

        return PortableExecutable.IsLargeAddressAware(bytes).TryGetValue(out var patched, out _) && patched
            ? [new DiagnosticIssue(
                DiagnosticSeverity.Warning,
                DiagnosticCodes.SteamExecutablePatched,
                "Steam will not start the game: its protected daorigins.exe has been changed by the 4 GB patch.",
                "Steam stops a changed copy with \"Application load error 3:0000065432\". Remove the patch on the Fixes tab (fixes remove laa), or let Steam restore the file with Verify integrity of game files.")]
            : [new DiagnosticIssue(
                DiagnosticSeverity.Info,
                DiagnosticCodes.SteamExecutableProtected,
                "daorigins.exe is Steam's protected copy, so the 4 GB patch is not applied to it.",
                "Steam refuses to start a changed copy. DXVK helps with the game's memory crashes without touching the executable.")];
    }
}
