using DAORework.Application.Abstractions;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Fixes;
using DAORework.Core.Mods.Library;

namespace DAORework.Application.Fixes;

/// <summary>
/// The 4 GB patch (ADR-0013): patches the game executable's bytes in memory and makes an entry holding the patched and
/// the unpatched exe, named by the unpatched exe's hash. A flag set by hand, or by another tool, is left alone.
/// </summary>
public sealed class LargeAddressAwareFix : IManagedFix
{
    public const string ExecutablePath = "bin_ship/daorigins.exe";
    public const string PatchedPath = "patched/daorigins.exe";
    public const string OriginalPath = "original/daorigins.exe";

    private readonly IInstallFiles _files;

    public LargeAddressAwareFix(IInstallFiles files)
    {
        _files = files;
    }

    public string Id => FixIds.LargeAddressAware;

    public IReadOnlyList<string> Aliases { get; } = ["laa", "4gb"];

    public async Task<Result<PreparedFix>> PrepareAsync(FixApplyContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (!(await _files.ReadAsync(context.InstallDirectory, ExecutablePath, cancellationToken)).TryGetValue(out var original, out var error))
        {
            return Result.Failure<PreparedFix>(error);
        }

        if (!LargeAddressAwarePatch.Apply(original).TryGetValue(out var patch, out error))
        {
            return Result.Failure<PreparedFix>(error);
        }

        return patch.WasAlreadySet ? AlreadySet(context) : Prepare(context.Library, original, patch);
    }

    private static Result<PreparedFix> AlreadySet(FixApplyContext context)
    {
        var applied = context.Library.FirstOrDefault(e => e.Fix == FixIds.LargeAddressAware && context.Record.ActiveModIds.Contains(e.LibraryId, StringComparer.OrdinalIgnoreCase));
        return applied is not null
            ? Result.Success(new PreparedFix(applied, new Dictionary<string, byte[]>(), new HashSet<string>(), []))
            : Result.Failure<PreparedFix>(new Error(
                DiagnosticCodes.FixNotNeeded,
                $"The 4 GB flag is already set in {ExecutablePath}; there is nothing to apply.",
                "It was set outside DAORework, by the 4GB Patch tool or a game that ships with it, and DAORework leaves it as it is."));
    }

    private static Result<PreparedFix> Prepare(IReadOnlyList<FixEntry> library, byte[] original, LargeAddressAwarePatchResult patch)
    {
        var originalSha = FixHashes.Of(original);
        var id = FixIds.CreateEntryId(FixIds.LargeAddressAware, null, originalSha);
        var existing = library.FirstOrDefault(e => string.Equals(e.Id, id, StringComparison.OrdinalIgnoreCase));
        var entry = existing ?? new FixEntry(
            id,
            FixIds.LargeAddressAware,
            null,
            [new FixFile(ExecutablePath, new LibraryModFile(PatchedPath, patch.Image.LongLength, FixHashes.Of(patch.Image)))],
            [new LibraryModFile(OriginalPath, original.LongLength, originalSha)]);
        var contents = existing is not null
            ? new Dictionary<string, byte[]>()
            : new Dictionary<string, byte[]> { [PatchedPath] = patch.Image, [OriginalPath] = original };
        string[] notes = patch.IsSigned
            ? ["daorigins.exe is digitally signed, and the patch invalidates the signature. Windows still runs it."]
            : [];
        return Result.Success(new PreparedFix(entry, contents, new HashSet<string>(StringComparer.OrdinalIgnoreCase) { originalSha }, notes));
    }
}
