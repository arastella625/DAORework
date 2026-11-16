namespace DAORework.Core.Mods.AddIns;

/// <summary>
/// An add-in the game loads: an enabled <c>AddInItem</c> in <c>Settings\AddIns.xml</c> (P2.5-23).
/// </summary>
/// <param name="Uid">Its UID, which is also its folder's name in <c>AddIns</c>.</param>
/// <param name="Title">The title the game shows for it.</param>
/// <param name="ExtendedModule">The campaign it adds to (<c>ExtendedModuleUID</c>), such as <c>Single Player</c>.</param>
/// <param name="Priority">Its <c>Priority</c>. When add-ins share a file, the lower number wins (<see cref="AddInRanking"/>).</param>
/// <param name="Position">Where its entry is in <c>AddIns.xml</c>, from 0. On equal priorities, the earlier entry wins.</param>
/// <param name="IsOfficial">Whether it is BioWare's own content (<c>BioWare="1"</c>).</param>
public sealed record ListedAddIn(string Uid, string Title, string ExtendedModule, int Priority, int Position, bool IsOfficial);
