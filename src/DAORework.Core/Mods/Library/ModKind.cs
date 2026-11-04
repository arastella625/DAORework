namespace DAORework.Core.Mods.Library;

/// <summary>
/// How a mod reaches the game (ADR-0012): loose files for the override folder, a <c>.dazip</c> add-in, or a program
/// that runs inside the game from <c>bin_ship</c> (ADR-0033), which nothing in the documents folder can hold.
/// </summary>
public enum ModKind
{
    Override,
    AddIn,
    GameProgram,
}
