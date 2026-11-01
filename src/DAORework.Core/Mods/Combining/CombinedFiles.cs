using DAORework.Core.Common;
using DAORework.Core.Mods.Library;
using DAORework.Core.Mods.Safety;

namespace DAORework.Core.Mods.Combining;

/// <summary>
/// Where combined files live and which mod files they stand in for (ADR-0025). A combined file is kept in the library
/// under <c>_combined/&lt;sha256&gt;/</c> and deployed to <c>override/DAORework/_combined/</c>; its record id is
/// <see cref="ModId"/>, which can never be a mod's, since a mod id always ends in a hash. Pure.
/// </summary>
public static class CombinedFiles
{
    /// <summary>The record id and library folder of every combined file.</summary>
    public const string ModId = "_combined";

    /// <summary>The words a plan uses for <see cref="ModId"/>.</summary>
    public const string DisplayName = "combined files";

    /// <summary>Gets where a combined file goes in the documents folder.</summary>
    public static string TargetPath(string fileName) => $"{ModLayout.OverridePrefix}{ModLayout.OwnedOverrideFolder}/{ModId}/{fileName}";

    /// <summary>The combined file as the planner deploys it, from the library file it is kept as.</summary>
    public static DeployedFile Deployed(LibraryModFile stored)
    {
        ArgumentNullException.ThrowIfNull(stored);
        return new DeployedFile(ModId, stored, TargetPath(stored.RelativePath[(stored.RelativePath.LastIndexOf('/') + 1)..]), IsOverride: true);
    }

    /// <summary>The override files named <c>chargenmorphcfg.xml</c> the mods would deploy, in load order (lowest priority first).</summary>
    public static Result<IReadOnlyList<DeployedFile>> ChargenMorphCopies(IReadOnlyList<LibraryMod> orderedMods)
    {
        ArgumentNullException.ThrowIfNull(orderedMods);
        var copies = new List<DeployedFile>();
        foreach (var mod in orderedMods)
        {
            if (!ModLayout.Map(mod).TryGetValue(out var files, out var error))
            {
                return Result.Failure<IReadOnlyList<DeployedFile>>(error);
            }

            copies.AddRange(files.Where(f => f.IsOverride && !ProgramFiles.IsProgram(f.Source)
                && f.FileName.Equals(GamePaths.ChargenMorphFileName, StringComparison.OrdinalIgnoreCase)));
        }

        return Result.Success<IReadOnlyList<DeployedFile>>(copies);
    }
}
