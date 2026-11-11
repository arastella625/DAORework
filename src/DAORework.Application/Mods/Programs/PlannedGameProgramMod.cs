using DAORework.Core.Fixes;

namespace DAORework.Application.Mods.Programs;

/// <summary>
/// A plan for a mod that runs inside the game, over the install folder's record (ADR-0033 §4), with what to name under
/// Details: its 64-bit DLLs, and files that stay in the library because they sit outside its <c>bin_ship</c> folder.
/// </summary>
public sealed record PlannedGameProgramMod(string ModId, string ModName, FixPlan Plan, IReadOnlyList<string> Notes);
