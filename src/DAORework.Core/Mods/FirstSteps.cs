namespace DAORework.Core.Mods;

/// <summary>
/// The way to a first game with mods, for a player who has read nothing (docs/WORKFLOWS.md, P2.6-36). It says where they
/// are and goes once they're there, so there is no tutorial to dismiss and nothing to remember. Pure.
/// </summary>
public static class FirstSteps
{
    public static IReadOnlyList<FirstStep> For(NextMoveFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        var hasMods = facts.InLibrary > 0 || facts.FoundInGame > 0;
        return
        [
            new("Find your game", facts.HasProfile),
            new("Add mods", hasMods),
            new("Turn them on", facts.On > 0),
        ];
    }

    /// <summary>Whether the steps are still worth showing: not once a mod is on.</summary>
    public static bool AreNeeded(NextMoveFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        return facts.On == 0;
    }
}
