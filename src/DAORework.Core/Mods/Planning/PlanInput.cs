using DAORework.Core.Mods.Combining;
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
    IReadOnlyCollection<string> RegisteredAddInUids)
{
    /// <summary>Gets the files combined from several mods' copies (ADR-0025), deployed in place of those copies.</summary>
    public IReadOnlyList<CombinedOverride> Combined { get; init; } = [];

    /// <summary>Gets why copies that would have been combined were not, such as a copy that cannot be read.</summary>
    public IReadOnlyList<string> CombineProblems { get; init; } = [];

    /// <summary>Gets the files in the install folder's override (P2.5-22), which the game loads too.</summary>
    public IReadOnlyList<string> InstallOverridePaths { get; init; } = [];

    /// <summary>Gets why the install folder's override could not be read, if it could not.</summary>
    public string? InstallOverrideProblem { get; init; }
}
