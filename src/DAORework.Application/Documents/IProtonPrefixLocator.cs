namespace DAORework.Application.Documents;

/// <summary>Finds the game's Proton prefix in the Steam libraries (ADR-0031). Empty on a machine that has none. Only reads.</summary>
public interface IProtonPrefixLocator
{
    /// <summary>Each prefix on the machine: there can be one per library.</summary>
    IReadOnlyList<string> FindPrefixes();

    /// <summary>The Dragon Age documents folders inside the prefixes, whatever the prefix calls its user and Documents folders.</summary>
    IReadOnlyList<string> FindDocumentsFolders();
}
