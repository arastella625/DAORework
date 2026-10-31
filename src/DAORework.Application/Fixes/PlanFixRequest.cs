using DAORework.Core.Fixes;

namespace DAORework.Application.Fixes;

/// <summary>Apply or remove <paramref name="Fix"/> (its id or an alias) in an install folder; <paramref name="SourcePath"/> is a file the user supplies.</summary>
public sealed record PlanFixRequest(string InstallDirectory, string Fix, FixChangeKind Kind, string? SourcePath = null);
