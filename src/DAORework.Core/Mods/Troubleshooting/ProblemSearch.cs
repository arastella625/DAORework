namespace DAORework.Core.Mods.Troubleshooting;

/// <summary>
/// A search for the mod behind a problem (ADR-0042): each round turns off half of the mods that could be the cause, the
/// player says whether the problem still happens, and the half that couldn't be is cleared. Pure.
/// </summary>
/// <param name="Original">What was on when the search started, in its order.</param>
/// <param name="Suspects">What could still be the cause.</param>
/// <param name="Off">The suspects turned off this round.</param>
public sealed record ProblemSearch(IReadOnlyList<string> Original, IReadOnlyList<string> Suspects, IReadOnlyList<string> Off)
{
    public const int CurrentSchemaVersion = 1;

    private static readonly StringComparer _ids = StringComparer.OrdinalIgnoreCase;

    public int SchemaVersion { get; init; } = CurrentSchemaVersion;

    /// <summary>Gets which round this is, from 1.</summary>
    public int Round { get; init; } = 1;

    /// <summary>Gets a value indicating whether the problem has gone in some round, which proves a suspect is the cause.</summary>
    public bool Proven { get; init; }

    /// <summary>Gets a value indicating whether this is the check with no mods at all, before the rounds start.</summary>
    public bool PlainCheck { get; init; }

    /// <summary>Gets the problem, in the player's own words.</summary>
    public string? Problem { get; init; }

    /// <summary>Gets how the search ended, if it has.</summary>
    public ProblemSearchEnd End { get; init; }

    /// <summary>Gets the mod it came down to, once found.</summary>
    public string? Culprit { get; init; }

    /// <param name="on">What is on now, in its order.</param>
    /// <param name="leaveOn">What stays out of it, such as mods that only change looks.</param>
    public static ProblemSearch Start(IReadOnlyList<string> on, IReadOnlyList<string>? leaveOn = null)
    {
        ArgumentNullException.ThrowIfNull(on);
        List<string> suspects = [.. on.Except(leaveOn ?? [], _ids)];
        return new ProblemSearch(on, suspects, Half(suspects));
    }

    /// <summary>The check that comes first if the player wants it: everything off, to learn whether it's a mod at all.</summary>
    /// <param name="on">What is on now, in its order.</param>
    /// <param name="leaveOn">What the rounds after it leave out.</param>
    public static ProblemSearch WithPlainCheck(IReadOnlyList<string> on, IReadOnlyList<string>? leaveOn = null) =>
        Start(on, leaveOn) with { Off = [.. on], PlainCheck = true };

    /// <summary>What is on this round: everything but the half being tested, in the player's own order.</summary>
    public IReadOnlyList<string> Keeping() => [.. Original.Except(Off, _ids)];

    /// <summary>About how many rounds are left after this one.</summary>
    public int RoundsLeft() => Suspects.Count < 2 ? 0 : (int)Math.Ceiling(Math.Log2(Suspects.Count)) - (PlainCheck ? 0 : 1);

    /// <summary>The search after the player's answer: the next round, or how it ended.</summary>
    /// <param name="stillHappens">Whether the problem happened with this round's mods off.</param>
    public ProblemSearch Answer(bool stillHappens)
    {
        if (PlainCheck)
        {
            return stillHappens
                ? this with { Off = [], End = ProblemSearchEnd.NotFound, PlainCheck = false }
                : this with { Off = Half(Suspects), Round = 1, PlainCheck = false, Proven = Suspects.Count == Original.Count };
        }

        if (Suspects.Count == 1)
        {
            return stillHappens ? this with { Off = [], End = ProblemSearchEnd.NotFound } : Found(Suspects[0]);
        }

        var next = stillHappens ? [.. Suspects.Except(Off, _ids)] : Off;
        var proven = Proven || !stillHappens;
        return next.Count == 1 && proven
            ? Found(next[0])
            : this with { Suspects = next, Off = Half(next), Round = Round + 1, Proven = proven };
    }

    /// <summary>The later half, so the mods that load last, which win their files, go first. One suspect alone is tested by itself.</summary>
    private static List<string> Half(IReadOnlyList<string> mods) => mods.Count == 1 ? [.. mods] : [.. mods.Skip((mods.Count + 1) / 2)];

    private ProblemSearch Found(string mod) => this with { Suspects = [mod], Off = [], End = ProblemSearchEnd.Found, Culprit = mod };
}
