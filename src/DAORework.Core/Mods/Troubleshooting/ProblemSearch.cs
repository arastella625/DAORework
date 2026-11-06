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

    public int SchemaVersion { get; init; } = CurrentSchemaVersion;

    /// <summary>Gets which round this is, from 1.</summary>
    public int Round { get; init; } = 1;

    /// <summary>Gets a value indicating whether the problem has gone in some round, which proves a suspect is the cause.</summary>
    public bool Proven { get; init; }

    /// <summary>Gets how the search ended, if it has.</summary>
    public ProblemSearchEnd End { get; init; }

    /// <summary>Gets the mod it came down to, once found.</summary>
    public string? Culprit { get; init; }

    /// <param name="on">What is on now, in its order.</param>
    public static ProblemSearch Start(IReadOnlyList<string> on)
    {
        ArgumentNullException.ThrowIfNull(on);
        return new ProblemSearch(on, on, Half(on));
    }

    /// <summary>What is on this round: everything but the half being tested, in the player's own order.</summary>
    public IReadOnlyList<string> Keeping() => [.. Original.Except(Off, StringComparer.OrdinalIgnoreCase)];

    /// <summary>About how many rounds are left after this one.</summary>
    public int RoundsLeft() => Suspects.Count < 2 ? 0 : (int)Math.Ceiling(Math.Log2(Suspects.Count)) - 1;

    /// <summary>The search after the player's answer: the next round, or how it ended.</summary>
    /// <param name="stillHappens">Whether the problem happened with this round's mods off.</param>
    public ProblemSearch Answer(bool stillHappens)
    {
        if (Suspects.Count == 1)
        {
            return stillHappens ? this with { Off = [], End = ProblemSearchEnd.NotFound } : Found(Suspects[0]);
        }

        var next = stillHappens ? [.. Suspects.Except(Off, StringComparer.OrdinalIgnoreCase)] : Off;
        var proven = Proven || !stillHappens;
        return next.Count == 1 && proven
            ? Found(next[0])
            : this with { Suspects = next, Off = Half(next), Round = Round + 1, Proven = proven };
    }

    /// <summary>The later half, so the mods that load last, which win their files, go first. One suspect alone is tested by itself.</summary>
    private static List<string> Half(IReadOnlyList<string> mods) => mods.Count == 1 ? [.. mods] : [.. mods.Skip((mods.Count + 1) / 2)];

    private ProblemSearch Found(string mod) => this with { Suspects = [mod], Off = [], End = ProblemSearchEnd.Found, Culprit = mod };
}
