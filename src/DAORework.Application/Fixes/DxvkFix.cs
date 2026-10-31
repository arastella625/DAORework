using DAORework.Application.Abstractions;
using DAORework.Core.Catalog;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Fixes;
using DAORework.Core.Mods.Library;

namespace DAORework.Application.Fixes;

/// <summary>
/// DXVK (ADR-0013): the 32-bit <c>d3d9.dll</c> from a release the user supplies, recognized by the known-items
/// catalog's fingerprint, placed in <c>bin_ship</c>. A <c>d3d9.dll</c> already there is replaced only when it is DXVK
/// too; one placed by hand is kept in the library first, so removing the new one puts it back.
/// </summary>
public sealed class DxvkFix : IManagedFix
{
    public const string DllPath = "bin_ship/d3d9.dll";
    public const string LibraryFileName = "d3d9.dll";

    private readonly IInstallFiles _files;
    private readonly IFixSourceReader _sources;
    private readonly IKnownItemsCatalogSource _catalog;

    public DxvkFix(IInstallFiles files, IFixSourceReader sources, IKnownItemsCatalogSource catalog)
    {
        _files = files;
        _sources = sources;
        _catalog = catalog;
    }

    public string Id => FixIds.Dxvk;

    public IReadOnlyList<string> Aliases { get; } = [];

    public async Task<Result<PreparedFix>> PrepareAsync(FixApplyContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (context.SourcePath is null)
        {
            return Result.Failure<PreparedFix>(new Error(
                DiagnosticCodes.FixSourceRequired,
                "DXVK is applied from a release you download: fixes apply dxvk <path>.",
                "Download dxvk-<version>.tar.gz from https://github.com/doitsujin/dxvk/releases and give its path, or the path of its x32\\d3d9.dll."));
        }

        if (!Marker().TryGetValue(out var marker, out var error)
            || !(await _sources.ReadDxvkDllAsync(context.SourcePath, cancellationToken)).TryGetValue(out var dll, out error)
            || !DxvkDll.Identify(dll, marker).TryGetValue(out var version, out error))
        {
            return Result.Failure<PreparedFix>(error);
        }

        var entry = Entry(dll, version);
        var existing = context.Library.FirstOrDefault(e => e.Id.Equals(entry.Id, StringComparison.OrdinalIgnoreCase));
        var contents = existing is null ? new Dictionary<string, byte[]> { [LibraryFileName] = dll } : new Dictionary<string, byte[]>();
        return await WithWhatIsThereAsync(context, marker, new PreparedFix(existing ?? entry, contents, new HashSet<string>(), []), cancellationToken);
    }

    private static FixEntry Entry(byte[] dll, string version)
    {
        var sha = FixHashes.Of(dll);
        return new FixEntry(FixIds.CreateEntryId(FixIds.Dxvk, version, sha), FixIds.Dxvk, version, [new FixFile(DllPath, new LibraryModFile(LibraryFileName, dll.LongLength, sha))], []);
    }

    /// <summary>Allows replacing a DXVK DLL already in bin_ship, keeping it when the library has no copy, and explains any other d3d9.dll.</summary>
    private async Task<Result<PreparedFix>> WithWhatIsThereAsync(FixApplyContext context, string marker, PreparedFix prepared, CancellationToken cancellationToken)
    {
        if (!(await _files.HashAsync(context.InstallDirectory, [DllPath], cancellationToken)).TryGetValue(out var hashes, out var error))
        {
            return Result.Failure<PreparedFix>(error);
        }

        if (!hashes.TryGetValue(DllPath, out var current) || context.Record.Files.Any(f => f.TargetPath.Equals(DllPath, StringComparison.OrdinalIgnoreCase)))
        {
            return Result.Success(prepared);
        }

        if (!(await _files.ReadAsync(context.InstallDirectory, DllPath, cancellationToken)).TryGetValue(out var there, out error))
        {
            return Result.Failure<PreparedFix>(error);
        }

        if (!DxvkDll.Identify(there, marker).TryGetValue(out var version, out _))
        {
            return Result.Success(prepared with
            {
                Notes = [$"{DllPath} is not DXVK: ReShade or another Direct3D wrapper uses the same name. DAORework does not replace it; move or rename it first if you want DXVK instead."],
            });
        }

        var inLibrary = context.Library.Any(e => e.Files.Any(f => f.Applied.Sha256.Equals(current, StringComparison.OrdinalIgnoreCase)));
        return Result.Success(prepared with
        {
            Replaceable = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { current },
            AlsoKeep = inLibrary ? [] : [new KeptEntry(Entry(there, version), new Dictionary<string, byte[]> { [LibraryFileName] = there })],
        });
    }

    private Result<string> Marker()
    {
        if (!_catalog.Load().TryGetValue(out var catalog, out var error))
        {
            return Result.Failure<string>(error);
        }

        var marker = catalog.Items
            .Where(i => i.Id == FixIds.Dxvk)
            .SelectMany(i => i.Fingerprints)
            .FirstOrDefault(f => f.Kind == FingerprintKind.GameFile && f.Marker is not null)?.Marker;
        return marker is null
            ? Result.Failure<string>(new Error(DiagnosticCodes.CatalogInvalid, "The known-items catalog has no DXVK fingerprint to recognize the DLL by."))
            : Result.Success(marker);
    }
}
