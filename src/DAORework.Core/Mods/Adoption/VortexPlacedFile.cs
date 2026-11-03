namespace DAORework.Core.Mods.Adoption;

/// <summary>A file a Vortex record lists, relative to the documents folder with <c>/</c>, and the Vortex mod it came from.</summary>
public sealed record VortexPlacedFile(string Path, string Mod);
