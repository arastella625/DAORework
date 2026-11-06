namespace DAORework.Core.Common;

/// <summary>An expected failure: a stable code from <see cref="Diagnostics.DiagnosticCodes"/>, a message, and an optional next step.</summary>
public sealed record Error(string Code, string Message, string? Recommendation = null)
{
    /// <summary>Gets the mod it came down to, when it came down to one, so a batch can go on without it.</summary>
    public string? ModId { get; init; }

    /// <summary>Gets what went wrong with that mod, without the wording around it.</summary>
    public string? Cause { get; init; }
}
