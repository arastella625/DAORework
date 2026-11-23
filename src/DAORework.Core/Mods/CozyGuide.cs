namespace DAORework.Core.Mods;

/// <summary>
/// The guide to a first game with mods, the owner's cozy guide (ADR-0051, P2.9-03). Its first three steps are P2.6-36's
/// first steps, done when <see cref="FirstSteps"/> says so, so a player who added mods before opening it starts at
/// <i>Turn them on</i>. Its words are docs/VOICE.md's: never <i>simply</i> or <i>just</i>, and never blaming. Pure.
/// </summary>
public static class CozyGuide
{
    /// <summary>What finishing the guide says, once.</summary>
    public const string Finished = "You're set. Every change can be undone in History.";

    public static IReadOnlyList<GuideStep> For(NextMoveFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        var first = FirstSteps.For(facts);
        return
        [
            new(
                GuideStepKind.FindYourGame,
                first[0].Name,
                facts.HasProfile
                    ? "Your profile points DAORework at your game. If it's the wrong copy, Game folders changes it."
                    : "Create a profile in the header, then show DAORework where your game is.",
                "Game folders",
                first[0].IsDone),
            new(
                GuideStepKind.AddMods,
                first[1].Name,
                "Mods come as .dazip, .zip, .7z or .rar files, from sites such as Nexus Mods. Adding one keeps it in your Library and doesn't change your game yet.",
                "Add mods",
                first[1].IsDone),
            new(
                GuideStepKind.TurnThemOn,
                first[2].Name,
                "Turning a mod on shows a preview first. Every change can be undone in History.",
                "Go to your mods",
                first[2].IsDone),
            new(
                GuideStepKind.Play,
                "Play",
                "Play starts the game with your mods. If something goes wrong, Find the problem mod narrows down which one.",
                "Play",
                IsDone: false),
        ];
    }

    /// <summary>The step to show: the first one not done.</summary>
    public static int Current(IReadOnlyList<GuideStep> steps)
    {
        ArgumentNullException.ThrowIfNull(steps);
        var next = steps.ToList().FindIndex(s => !s.IsDone);
        return next < 0 ? steps.Count - 1 : next;
    }

    /// <summary>
    /// Whether Home shows it (ADR-0051 §3): until it is skipped or finished, while no mod is on, and on to <i>Play</i>
    /// once a player who saw it turns a mod on.
    /// </summary>
    public static bool Shows(bool closed, NextMoveFacts facts, bool seenThisSession)
    {
        ArgumentNullException.ThrowIfNull(facts);
        return !closed && (facts.On == 0 || seenThisSession);
    }
}
