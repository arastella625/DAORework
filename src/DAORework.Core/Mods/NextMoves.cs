namespace DAORework.Core.Mods;

/// <summary>
/// One next step, worked out from where the player is, so nobody has to know the order. What blocks everything comes
/// first, then what stands in the way of a clean game, then the ordinary path to <i>Play</i>. Pure.
/// </summary>
public static class NextMoves
{
    public static NextMove For(NextMoveFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        return facts switch
        {
            { HasProfile: false } => new(NextMoveKind.SetUpTheGame, "Find your game", "Create a profile above, then show DAORework where your game is.", "Game folders"),
            { ChangeUnfinished: true } => new(NextMoveKind.SettleTheLastChange, "Your last change didn't finish", "Finish it or undo it before making another.", "Open History"),
            { VortexMoveUnderway: true } => new(NextMoveKind.FinishVortexMove, "Finish moving from Vortex", "Your mods are part way there. The card below says what's next.", null),
            { FoundInGame: > 0 } => new(NextMoveKind.TakeOverWhatIsInTheGame, TakeOverTitle(facts.FoundInGame), "DAORework can't turn these off or undo them until it looks after them. You'll see a preview first.", "Take them over"),
            { InLibrary: 0 } => new(NextMoveKind.AddMods, "Add your first mod", "Adding a mod doesn't change your game until you turn it on.", "Add mods"),
            { On: 0 } => new(NextMoveKind.TurnOnMods, "Turn on the mods you want", "Tick them below, or use the switch on each one.", null),
            _ => new(NextMoveKind.Play, "You're ready to play", "Your mods are in place.", null),
        };
    }

    private static string TakeOverTitle(int found) => found == 1 ? "1 mod is in your game" : $"{found} mods are in your game";
}
