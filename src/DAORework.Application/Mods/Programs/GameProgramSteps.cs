namespace DAORework.Application.Mods.Programs;

/// <summary>
/// Planning, checking what it needs, and applying a mod that runs inside the game (ADR-0033), grouped so the window and
/// the command line stay within five dependencies (ENGINEERING_STANDARDS §3).
/// </summary>
public sealed record GameProgramSteps(PlanGameProgramMod Plan, FindMissingNeeds Needs, ApplyGameProgramMod Apply);
