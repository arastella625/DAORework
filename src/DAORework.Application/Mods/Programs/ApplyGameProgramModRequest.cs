namespace DAORework.Application.Mods.Programs;

/// <summary>
/// Applies <paramref name="Planned"/>. <paramref name="ProgramAccepted"/> is the player's tick, "I trust this mod and
/// where I got it" (ADR-0033 §3): never set for them, and needed every time such a mod is turned on.
/// </summary>
public sealed record ApplyGameProgramModRequest(string InstallDirectory, PlannedGameProgramMod Planned, bool ProgramAccepted);
