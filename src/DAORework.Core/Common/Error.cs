namespace DAORework.Core.Common;

/// <summary>An expected failure: a stable code from <see cref="Diagnostics.DiagnosticCodes"/>, a message, and an optional next step.</summary>
public sealed record Error(string Code, string Message, string? Recommendation = null);
