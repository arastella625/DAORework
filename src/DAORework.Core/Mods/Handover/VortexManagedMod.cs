namespace DAORework.Core.Mods.Handover;

/// <summary>A mod Vortex still manages (ADR-0028 §2): its folder name, the name to show, and how many of its files are in the game.</summary>
public sealed record VortexManagedMod(string VortexName, string Name, int FilesInGame);
