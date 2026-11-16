using DAORework.Core.Fixes;

namespace DAORework.Application.Mods.Programs;

/// <summary>Turn a mod that runs inside the game on (<see cref="FixChangeKind.Apply"/>) or off in an install folder.</summary>
public sealed record PlanGameProgramModRequest(string InstallDirectory, string ModId, FixChangeKind Kind);
