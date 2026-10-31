namespace DAORework.Core.Mods.Planning;

/// <summary>An <c>AddInItem</c> the plan adds to or removes from <c>Settings\AddIns.xml</c>; nothing else in the file changes.</summary>
public sealed record AddInsListChange(string Uid, string ModId, bool Add);
