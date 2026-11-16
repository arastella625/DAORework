namespace DAORework.Core.Mods.AddIns;

/// <summary>
/// What an add-in's row on Mods says (P2.5-23): its Priority from <c>AddIns.xml</c>, and whether the game uses its copy
/// of a file another add-in has too (<i>wins</i>), the other's (<i>loses</i>), or each in turn.
/// </summary>
/// <param name="Priority">Its Priority in <c>AddIns.xml</c>.</param>
/// <param name="Wins">Whether the game uses its copy of at least one shared file.</param>
/// <param name="Loses">Whether the game uses another add-in's copy of at least one shared file.</param>
/// <param name="Sentences">What its details say about the files it shares, each as diagnostics word it.</param>
public sealed record AddInMark(int Priority, bool Wins, bool Loses, IReadOnlyList<string> Sentences)
{
    /// <summary>Gets a value indicating whether it shares a file with another add-in.</summary>
    public bool Shares => Wins || Loses;

    /// <summary>Gets the tag, such as <i>Priority 100 · wins</i>.</summary>
    public string Text => (Wins, Loses) switch
    {
        (true, true) => $"Priority {Priority} · wins and loses",
        (true, false) => $"Priority {Priority} · wins",
        (false, true) => $"Priority {Priority} · loses",
        _ => $"Priority {Priority}",
    };
}
