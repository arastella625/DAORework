namespace DAORework.Application.Abstractions;

/// <summary>A file to scan: its name as the player knows it (its path in the mod), where it is on disk, and whether it is a program.</summary>
public sealed record ScanTarget(string Name, string Path, bool IsProgram);
