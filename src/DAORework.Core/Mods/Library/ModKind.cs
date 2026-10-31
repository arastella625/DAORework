namespace DAORework.Core.Mods.Library;

/// <summary>How a mod reaches the game (ADR-0012): loose files for the override folder, or a <c>.dazip</c> add-in.</summary>
public enum ModKind
{
    Override,
    AddIn,
}
