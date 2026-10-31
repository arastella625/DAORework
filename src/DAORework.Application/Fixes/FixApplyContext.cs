using DAORework.Core.Fixes;
using DAORework.Core.Mods.Planning;

namespace DAORework.Application.Fixes;

/// <summary>What a fix is prepared from: the install folder, its record, the fixes in the library, and a file the user named.</summary>
public sealed record FixApplyContext(string InstallDirectory, DeploymentRecord Record, IReadOnlyList<FixEntry> Library, string? SourcePath);
