using DAORework.Core.Mods.Troubleshooting;

namespace DAORework.App.ViewModels;

/// <summary>What <i>Find the problem mod</i> says at each step (ADR-0042, docs/VOICE.md).</summary>
internal static class ProblemSearchWords
{
    public static string Headline(ProblemSearch? search, Func<string, string> nameOf) => search switch
    {
        null => "Find the problem mod",
        { End: ProblemSearchEnd.Found, Culprit: { } mod } => $"It looks like {nameOf(mod)}",
        { End: ProblemSearchEnd.NotFound, Suspects.Count: > 1 } => "It doesn't look like your mods",
        { End: ProblemSearchEnd.NotFound } => "It doesn't look like one of your mods",
        { PlainCheck: true } => "The plain game check: no mods at all",
        { Suspects.Count: 1 } => $"Round {search.Round}: one last check",
        _ => $"Round {search.Round} of about {search.Round + search.RoundsLeft()}: {Counted.Of(search.Suspects.Count, "mod")} could be the one",
    };

    public static string Body(ProblemSearch? search) => search switch
    {
        null => "Something going wrong in your game? This turns off half your mods at a time, and you play to see if the problem still happens, until one is left. Everything goes back at the end.",
        { End: ProblemSearchEnd.Found } => "The problem went away with it off. Two mods that only clash together can look like this too, so if it comes back, look at what it shares with others.",
        { End: ProblemSearchEnd.NotFound, Suspects.Count: > 1 } => "The problem happened with your mods off, so it's probably the game itself.",
        { End: ProblemSearchEnd.NotFound } => "The problem happened with each of them off. It could be two mods together, or the game itself.",
        _ => "Play the game, then say what happened.",
    };

    public static string StillHappens(ProblemSearch? search) => search switch
    {
        { PlainCheck: true } => "Yes, still happening. So it isn't your mods",
        { Suspects.Count: 1 } => "Yes, still happening. So it isn't this mod",
        { } s => $"Yes, still happening. The problem is in the {s.Suspects.Count - s.Off.Count} still on",
        _ => string.Empty,
    };

    public static string ItsGone(ProblemSearch? search) => search switch
    {
        { PlainCheck: true } => "No, it's gone. So it's one of your mods",
        { Suspects.Count: 1 } => "No, it's gone. So it's this mod",
        { } s => $"No, it's gone. The problem is in the {s.Off.Count} we turned off",
        _ => string.Empty,
    };

    public static string RoundTitle(ProblemSearch next) =>
        next.PlainCheck ? "Turn off every mod" : $"Round {next.Round}: turn off {Counted.Of(next.Off.Count, "mod")}";

    public static string Progress(ProblemSearch? search) =>
        search is null ? string.Empty : $"{search.Original.Count - search.Suspects.Count} ruled out, {search.Suspects.Count} still suspects";
}
