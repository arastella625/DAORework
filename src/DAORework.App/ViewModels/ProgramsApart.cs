using DAORework.Core.Mods.Planning;

namespace DAORework.App.ViewModels;

/// <summary>
/// A mod that runs inside the game is changed by itself, after its own preview (ADR-0033 §3): <i>Turn on selected</i>
/// leaves it out and names it. One that is on in the documents folder, added before DAORework knew what it is, is turned
/// off with the rest, as before.
/// </summary>
internal static class ProgramsApart
{
    /// <param name="kind">Turning on or off.</param>
    /// <param name="mods">What the player picked.</param>
    /// <param name="onInGame">Whether a mod of this kind is on in the game folder.</param>
    /// <returns>The mods for the documents folder, and those of this kind that go apart.</returns>
    public static (IReadOnlyList<LibraryModRow> Others, IReadOnlyList<LibraryModRow> Programs) Split(ModChangeKind kind, IReadOnlyList<LibraryModRow> mods, Func<string, bool> onInGame)
    {
        ArgumentNullException.ThrowIfNull(mods);
        ArgumentNullException.ThrowIfNull(onInGame);
        var programs = mods.Where(m => m.RunsInGame && (kind == ModChangeKind.Activate || onInGame(m.Id))).ToList();
        return ([.. mods.Except(programs)], programs);
    }

    /// <summary>Why each was left out of a change with other mods.</summary>
    public static IReadOnlyList<LeftOutMod> LeftOut(ModChangeKind kind, IReadOnlyList<LibraryModRow> programs)
    {
        ArgumentNullException.ThrowIfNull(programs);
        var why = kind == ModChangeKind.Activate
            ? "It runs its own program inside the game, so it's turned on by itself, after you say you trust it."
            : "It runs its own program inside the game, so it's turned off by itself.";
        return [.. programs.Select(m => new LeftOutMod(m.Title, why))];
    }
}
