namespace DAORework.Core.Mods.Library;

/// <summary>One file of a mod in DAORework's library, relative to the mod's root with <c>/</c> separators.</summary>
public sealed record LibraryModFile(string RelativePath, long SizeBytes, string Sha256)
{
    /// <summary>Gets whether it is data or a program (ADR-0018), as classified at import.</summary>
    public ModFileKind Kind { get; init; }
}
