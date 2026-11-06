namespace DAORework.Core.Mods;

/// <summary>Where the player is: what the window already knows.</summary>
/// <param name="HasProfile">Whether a profile is selected.</param>
/// <param name="ChangeUnfinished">Whether a change didn't finish.</param>
/// <param name="VortexMoveUnderway">Whether the move from Vortex has started and isn't done.</param>
/// <param name="FoundInGame">How many mods are in the game and not looked after.</param>
/// <param name="InLibrary">How many mods the Library shows.</param>
/// <param name="On">How many mods are on.</param>
/// <param name="CopiesToClear">How many mods are held more than once where keeping one loses nothing.</param>
public sealed record NextMoveFacts(bool HasProfile, bool ChangeUnfinished, bool VortexMoveUnderway, int FoundInGame, int InLibrary, int On, int CopiesToClear = 0);
