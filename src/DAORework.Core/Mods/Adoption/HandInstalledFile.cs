namespace DAORework.Core.Mods.Adoption;

/// <summary>A file in the documents folder DAORework did not place: its path relative to the folder, with <c>/</c>, and its size.</summary>
public sealed record HandInstalledFile(string Path, long SizeBytes);
