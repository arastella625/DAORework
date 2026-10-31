using System.IO.Abstractions;
using DAORework.Core.Diagnostics;
using DAORework.Core.Formats;
using DAORework.Core.Profiles;

namespace DAORework.Application.Diagnostics.Rules;

/// <summary>
/// Reports whether daorigins.exe can use 4 GB of memory (the large-address-aware flag). Report only:
/// applying the flag is Phase 2 work (P2-08), behind backup and rollback.
/// </summary>
public sealed class LargeAddressAwareRule : IDiagnosticRule
{
    private readonly IFileSystem _fileSystem;

    public LargeAddressAwareRule(IFileSystem fileSystem)
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
        catch (IOException exception)
        {
            return [Unreadable(exception.Message)];
        }
        catch (UnauthorizedAccessException exception)
        {
            return [Unreadable(exception.Message)];
        }

        if (SteamStub.IsProtected(header.AsSpan(0, read)))
        {
            return []; // SteamProtectedExecutableRule reports Steam's protected copy (ADR-0023).
        }

        if (!PortableExecutable.IsLargeAddressAware(header.AsSpan(0, read)).TryGetValue(out var enabled, out var error))
        {
            return [Unreadable(error.Message)];
        }

        return enabled
            ? [new DiagnosticIssue(DiagnosticSeverity.Info, DiagnosticCodes.LargeAddressAwareEnabled, "The game executable can use 4 GB of memory (large-address-aware).")]
            : [new DiagnosticIssue(
                DiagnosticSeverity.Warning,
                DiagnosticCodes.LargeAddressAwareDisabled,
                "The game executable is limited to 2 GB of memory (large-address-aware flag not set).",
                "The game is 32-bit; with texture mods or long sessions it can run out of memory and crash. The community 4 GB patch sets this flag. DAORework applies it with a backup: fixes apply laa (and fixes remove laa undoes it).")];
    }

    private static DiagnosticIssue Unreadable(string reason) => new(
        DiagnosticSeverity.Warning,
        DiagnosticCodes.LargeAddressAwareUnreadable,
        $"Could not read the game executable's header: {reason}");
}
