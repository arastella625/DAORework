namespace DAORework.Core.Mods;

/// <summary>
/// Home's one headline, sentence and button (ADR-0050 §1), from the next step <see cref="NextMoves"/> weighs and the last
/// check of the game. What blocks everything comes first, then what stops the game, then the next step on the way, and
/// only then whether it is ready. Pure.
/// </summary>
public static class HomeHeadlines
{
    /// <param name="move">The next step, from <see cref="NextMoves.For"/>.</param>
    /// <param name="check">The last check of the game, or null while it is running.</param>
    public static HomeHeadline For(NextMove move, GameCheck? check)
    {
        ArgumentNullException.ThrowIfNull(move);
        return (move.Kind, check) switch
        {
            (NextMoveKind.SetUpTheGame, _) => new(HomeHeadlineKind.Welcome, "Welcome", "Create a profile in the header, then show DAORework where your game is.", move.Button, HomeButton.TakeNextMove),
            (NextMoveKind.SettleTheLastChange, _) => OneThingFirst(move),
            (_, { Failed: true }) => new(HomeHeadlineKind.NotReady, "Not ready yet", "The game couldn't be checked. Troubleshooting says why.", "See what's wrong", HomeButton.SeeProblems),
            (_, { Problems: > 0 } found) => new(HomeHeadlineKind.NotReady, "Not ready yet", $"{Problems(found.Problems)} to fix before the game can start.", "See what's wrong", HomeButton.SeeProblems),
            (not NextMoveKind.Play, _) => OneThingFirst(move),
            (_, null) => new(HomeHeadlineKind.Checking, "Checking your game", "It takes a moment. You can carry on meanwhile.", null, HomeButton.None),
            (_, { ModsElsewhere: true }) => new(HomeHeadlineKind.ReadyWithoutMods, "Ready to play, without your mods", "The game reads another documents folder, so your mods don't reach it.", "Game folders", HomeButton.GameFolders),
            _ => new(HomeHeadlineKind.Ready, "Ready to play", "Your mods are in place and the game can start.", "Play", HomeButton.Play),
        };
    }

    // The step itself is the sentence; a step whose card on Mods needs no button still gets one here, to take the player there.
    private static HomeHeadline OneThingFirst(NextMove move) =>
        new(HomeHeadlineKind.OneThingFirst, "One thing first", move.Title + ".", move.Button ?? GoToMods(move.Kind), HomeButton.TakeNextMove);

    private static string GoToMods(NextMoveKind kind) => kind == NextMoveKind.TurnOnMods ? "Turn them on" : "Go to Mods";

    private static string Problems(int count) => count == 1 ? "1 problem" : $"{count} problems";
}
