namespace DAORework.Core.Mods.Conflicts;

/// <summary>What can go wrong when two mods hold one file, most serious first (P2.6-54).</summary>
public enum SharedFileImpact
{
    /// <summary>A script, table, conversation or the like, where a lost change can stop a quest.</summary>
    CouldBreakQuests,

    /// <summary>Anything DAORework can't rank either way, such as text or an archive.</summary>
    Other,

    /// <summary>A texture, model or animation: only how something looks.</summary>
    Looks,
}
