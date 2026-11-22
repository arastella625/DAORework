using DAORework.Core.Diagnostics;

namespace DAORework.Core.Mods;

/// <summary>
/// What the last check of the game found that Home's headline needs: what stops the game, whether its mods reach it, and
/// whether the check could run at all.
/// </summary>
public sealed record GameCheck(int Problems, bool ModsElsewhere, bool Failed = false)
{
    /// <summary>Gets a check that ended without a report, such as a profile that can't be read.</summary>
    public static GameCheck CouldNotRun { get; } = new(0, false, Failed: true);

    /// <summary>Reads a check's issues: each error stops the game, and a documents folder the game doesn't read keeps the mods out (ADR-0046).</summary>
    public static GameCheck From(IEnumerable<DiagnosticIssue> issues)
    {
        ArgumentNullException.ThrowIfNull(issues);
        var list = issues.ToList();
        return new GameCheck(
            list.Count(i => i.Severity == DiagnosticSeverity.Error),
            list.Any(i => i.Code == DiagnosticCodes.DocumentsNotTheGames));
    }
}
