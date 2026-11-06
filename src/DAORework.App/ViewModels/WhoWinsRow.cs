namespace DAORework.App.ViewModels;

/// <summary>One set of mods with different files of the same name, in a <i>Manage</i> preview (P2.6-21): who wins, over what, and whether the player can swap it.</summary>
public sealed record WhoWinsRow(string Title, string Kinds, string WinnerModId, IReadOnlyList<string> OtherModIds, bool CanSwap);
