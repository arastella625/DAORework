namespace DAORework.Core.Mods.AddIns;

/// <summary>
/// The file names an add-in gives the game (P2.5-23): its loose files and the names inside its archives, apart for
/// its <c>core</c> folder, which the whole game sees, and its <c>module</c> folder, which only its campaign sees.
/// </summary>
public sealed record AddInContents(ListedAddIn AddIn, IReadOnlyList<string> CoreNames, IReadOnlyList<string> ModuleNames);
