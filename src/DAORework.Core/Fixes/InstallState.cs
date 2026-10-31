using DAORework.Core.Mods.Planning;

namespace DAORework.Core.Fixes;

/// <summary>
/// What the fix planner needs from the install folder: its record, the current hash of each path in play (absent when
/// there is no file), the hashes the caller has identified as safe to replace (the unpatched game executable, a DXVK
/// <c>d3d9.dll</c>), and any reason found up front that nothing can be written (ADR-0013).
/// </summary>
public sealed record InstallState(
    DeploymentRecord Record,
    IReadOnlyDictionary<string, string> Hashes,
    IReadOnlySet<string> Replaceable,
    IReadOnlyList<PlanBlocker> Blockers);
