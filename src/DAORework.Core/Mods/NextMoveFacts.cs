namespace DAORework.Core.Mods;

/// <summary>Where the player is: what the window already knows.</summary>
public sealed record NextMoveFacts(bool HasProfile, bool ChangeUnfinished, bool VortexMoveUnderway, int FoundInGame, int InLibrary, int On);
