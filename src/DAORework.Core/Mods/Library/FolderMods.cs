namespace DAORework.Core.Mods.Library;

/// <summary>What is on in one documents folder, and whether a change there didn't finish.</summary>
public sealed record FolderMods(string DocumentsDirectory, IReadOnlyList<string> ActiveModIds, bool Unfinished);
