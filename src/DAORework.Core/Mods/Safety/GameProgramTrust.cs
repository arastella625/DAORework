namespace DAORework.Core.Mods.Safety;

/// <summary>What the player is told before saying yes to a mod that runs inside the game (ADR-0033 §3), worded once for the CLI and the window.</summary>
public static class GameProgramTrust
{
    /// <summary>Shown above the question whenever turning one on would put files in the game folder.</summary>
    public const string Warning =
        "This mod runs its own program inside the game, with the same access to your computer as you have. Your antivirus found nothing, but DAORework can't tell what it does.";

    /// <summary>The tick box in the window. It is never ticked for the player, and asked every time.</summary>
    public const string Question = "I trust this mod and where I got it";
}
