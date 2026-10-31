namespace DAORework.Core.Mods.Library;

/// <summary>
/// A mod as imported into DAORework's library (ADR-0012), which is never changed afterwards. For an add-in the
/// files are the <c>.dazip</c>'s own layout (<c>Manifest.xml</c>, <c>Contents/...</c>) and <see cref="AddInUid"/> is
/// its manifest's UID; for an override mod they are the loose files as the mod ships them.
/// </summary>
public sealed record LibraryMod(string Id, string Name, ModKind Kind, IReadOnlyList<LibraryModFile> Files, string? AddInUid = null);
