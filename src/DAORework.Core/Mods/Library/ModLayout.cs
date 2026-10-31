using DAORework.Core.Common;
using DAORework.Core.Diagnostics;

namespace DAORework.Core.Mods.Library;

/// <summary>
/// Maps a library mod's files to where they go in the documents folder (ADR-0012). Override files go to DAORework's
/// own subfolder, <c>packages/core/override/DAORework/&lt;modId&gt;/</c>; an add-in's <c>Contents</c> goes where the
/// game's DAUpdater would put it, except that its override files also go to the owned subfolder. Files outside an
/// add-in's <c>Contents</c> (its <c>Manifest.xml</c>, a readme) are not deployed.
/// </summary>
public static class ModLayout
{
    /// <summary>The folder under <c>packages/core/override</c> that holds every override file DAORework deploys.</summary>
    public const string OwnedOverrideFolder = "DAORework";

    public const string OverridePrefix = "packages/core/override/";

    private const string ContentsPrefix = "Contents/";

    public static Result<IReadOnlyList<DeployedFile>> Map(LibraryMod mod)
    {
        ArgumentNullException.ThrowIfNull(mod);
        var files = new List<DeployedFile>();
        foreach (var file in mod.Files)
        {
            if (!ModPath.IsSafe(file.RelativePath))
            {
                return Result.Failure<IReadOnlyList<DeployedFile>>(new Error(
                    DiagnosticCodes.ModPathInvalid,
                    $"Mod '{mod.Id}' has a file path that would leave its folder: '{file.RelativePath}'."));
            }

            var target = mod.Kind == ModKind.Override ? Owned(mod.Id, file.RelativePath) : AddInTarget(mod.Id, file.RelativePath);
            if (target is not null)
            {
                files.Add(new DeployedFile(mod.Id, file, target, target.StartsWith(OverridePrefix, StringComparison.OrdinalIgnoreCase)));
            }
        }

        return Result.Success<IReadOnlyList<DeployedFile>>(files);
    }

    private static string Owned(string modId, string relative) => $"{OverridePrefix}{OwnedOverrideFolder}/{modId}/{relative}";

    private static string? AddInTarget(string modId, string relative)
    {
        if (!relative.StartsWith(ContentsPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var inside = relative[ContentsPrefix.Length..];
        if (inside.StartsWith(OverridePrefix, StringComparison.OrdinalIgnoreCase))
        {
            return Owned(modId, inside[OverridePrefix.Length..]);
        }

        // The game's folder is "AddIns"; dazips write "addins". Windows does not care, but the record should read the same.
        const string addIns = "addins/";
        return inside.StartsWith(addIns, StringComparison.OrdinalIgnoreCase)
            ? $"{GamePaths.AddInsDirectoryName}/{inside[addIns.Length..]}"
            : inside;
    }
}
