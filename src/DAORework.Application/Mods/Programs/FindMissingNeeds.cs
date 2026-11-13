using System.IO.Abstractions;
using DAORework.Application.Abstractions;
using DAORework.Application.Catalog;
using DAORework.Application.Diagnostics.Rules;
using DAORework.Application.Profiles;
using DAORework.Core.Catalog;
using DAORework.Core.Common;
using DAORework.Core.Mods.Safety;

namespace DAORework.Application.Mods.Programs;

/// <summary>
/// What a mod in the Library needs that isn't in the game (ADR-0033 §5): the catalog's <c>needs</c> for the entry its
/// download is, checked against the profile's game. Warnings only, for its preview. Only reads.
/// </summary>
public sealed class FindMissingNeeds
{
    private readonly GetProfile _getProfile;
    private readonly IModLibrary _library;
    private readonly IKnownItemsCatalogSource _catalog;
    private readonly DetectKnownItems _detect;
    private readonly IFileSystem _fileSystem;

    public FindMissingNeeds(GetProfile getProfile, IModLibrary library, IKnownItemsCatalogSource catalog, DetectKnownItems detect, IFileSystem fileSystem)
    {
        _getProfile = getProfile;
        _library = library;
        _catalog = catalog;
        _detect = detect;
        _fileSystem = fileSystem;
    }

    /// <returns>A warning for each missing need; none when the catalog doesn't know the mod or it needs nothing.</returns>
    public async Task<Result<IReadOnlyList<string>>> ExecuteAsync(string profileName, string modId, CancellationToken cancellationToken = default)
    {
        if (!_catalog.Load().TryGetValue(out var catalog, out var error))
        {
            return Result.Failure<IReadOnlyList<string>>(error);
        }

        var entry = (await _library.ListAsync(cancellationToken)).FirstOrDefault(e => e.Mod.Id.Equals(modId, StringComparison.OrdinalIgnoreCase));
        if (entry is null || KnownDownloads.Check(catalog, entry).Item is not { Needs.Count: > 0 } item)
        {
            return Result.Success<IReadOnlyList<string>>([]);
        }

        if (!(await _getProfile.ExecuteAsync(new GetProfileRequest(profileName), cancellationToken)).TryGetValue(out var found, out error))
        {
            return Result.Failure<IReadOnlyList<string>>(error);
        }

        // These mods go into the install folder, whose bin_ship/daorigins.exe the fixes change too; a profile pointed only
        // at the folder has no executable of its own yet, and the 4 GB patch would read as missing.
        var profile = string.IsNullOrWhiteSpace(found.GameExecutablePath) && !string.IsNullOrWhiteSpace(found.InstallDirectory)
            ? found with { GameExecutablePath = GamePaths.GameExecutable(found.InstallDirectory) }
            : found;
        var needed = item.Needs!.Select(n => n.Item).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (!(await _detect.ExecuteAsync(new DetectKnownItemsRequest(profile, needed), cancellationToken)).TryGetValue(out var scan, out error))
        {
            return Result.Failure<IReadOnlyList<string>>(error);
        }

        // Steam's protected copy is the one the 4 GB patch can't go on; the rule that says so in diagnostics reads it here too.
        var steamProtected = (await new SteamProtectedExecutableRule(_fileSystem).EvaluateAsync(profile, cancellationToken)).Count > 0;
        var missing = scan.Missing.Select(i => i.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return Result.Success(NeedsCheck.Warnings(catalog, item, entry.Mod.Name, missing, steamProtected));
    }
}
