namespace DAORework.Core.Mods.Adoption;

/// <summary>An <c>AddInItem</c> in <c>Settings\AddIns.xml</c>: its UID and the title the game shows for it.</summary>
public sealed record RegisteredAddIn(string Uid, string Title);
