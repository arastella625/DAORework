namespace DAORework.Core.Mods.Adoption;

/// <summary>A file in the install folder's override.</summary>
/// <param name="Path">The file's path below the override folder, with forward slashes.</param>
/// <param name="SizeBytes">The file's size.</param>
public sealed record InstallOverrideFile(string Path, long SizeBytes);
