using DAORework.Core.Diagnostics;

namespace DAORework.Core.Catalog;

/// <summary>Two catalog items that are known to break each other when both are installed, with the source that says so.</summary>
public sealed record KnownConflict(string First, string Second, DiagnosticSeverity Severity, string Reason, string Source);
