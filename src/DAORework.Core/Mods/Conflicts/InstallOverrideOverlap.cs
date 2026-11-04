using DAORework.Core.Mods.Library;

namespace DAORework.Core.Mods.Conflicts;

/// <summary>
/// The override files a plan deploys that share a name with a file in the install folder's override (ROADMAP P2.5-22).
/// The game finds override files by name, so it uses one copy. Pure: the caller reads the folder.
/// </summary>
public static class InstallOverrideOverlap
{
    /// <param name="deploy">What the plan deploys.</param>
    /// <param name="installOverridePaths">The files in the install folder's override, any separators.</param>
    public static IReadOnlyList<DeployedFile> Find(IReadOnlyList<DeployedFile> deploy, IReadOnlyList<string> installOverridePaths)
    {
        ArgumentNullException.ThrowIfNull(deploy);
        ArgumentNullException.ThrowIfNull(installOverridePaths);
        var names = installOverridePaths.Select(p => p[(p.LastIndexOfAny(['/', '\\']) + 1)..]).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return [.. deploy.Where(f => f.IsOverride && names.Contains(f.FileName))];
    }
}
