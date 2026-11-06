namespace DAORework.Core.Mods;

/// <summary>What the player should do next (docs/WORKFLOWS.md, P2.6-27).</summary>
public enum NextMoveKind
{
    SetUpTheGame,
    SettleTheLastChange,
    FinishVortexMove,
    TakeOverWhatIsInTheGame,
    ClearCopies,
    AddMods,
    TurnOnMods,
    Play,
}
