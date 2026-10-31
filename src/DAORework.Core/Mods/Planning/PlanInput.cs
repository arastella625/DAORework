using DAORework.Core.Mods.Library;

namespace DAORework.Core.Mods.Planning;

/// <summary>
/// What the planner needs, gathered by the caller: the deployment record, the mods to activate (lowest priority first),
/// the override files the user placed by hand, the SHA-256 of every file now at a recorded or target path (a path that
/// is missing has no entry), and the UIDs <c>Settings\AddIns.xml</c> registers now.
/// </summary>
public sealed record PlanInput(
    DeploymentRecord Current,
    IReadOnlyList<LibraryMod> Mods,
    IReadOnlyList<string> UnmanagedOverridePaths,
    IReadOnlyDictionary<string, string> DiskHashes,
    IReadOnlyCollection<string> RegisteredAddInUids);
