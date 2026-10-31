namespace DAORework.Core.Diagnostics;

public sealed record DiagnosticIssue(
    DiagnosticSeverity Severity,
    string Code,
    string Message,
    string? Recommendation = null);
