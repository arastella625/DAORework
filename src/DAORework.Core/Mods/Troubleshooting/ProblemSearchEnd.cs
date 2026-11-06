namespace DAORework.Core.Mods.Troubleshooting;

/// <summary>How a search for the problem mod ended (ADR-0042).</summary>
public enum ProblemSearchEnd
{
    /// <summary>It is still going.</summary>
    None,

    /// <summary>It came down to one mod.</summary>
    Found,

    /// <summary>The problem happened with every mod off in turn.</summary>
    NotFound,
}
