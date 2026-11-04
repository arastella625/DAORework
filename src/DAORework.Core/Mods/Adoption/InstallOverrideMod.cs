namespace DAORework.Core.Mods.Adoption;

/// <summary>What the install folder's override holds under one name: a folder, or the loose files together.</summary>
public sealed record InstallOverrideMod(string Name, int FileCount, long SizeBytes);
