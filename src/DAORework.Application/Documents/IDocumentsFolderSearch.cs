namespace DAORework.Application.Documents;

/// <summary>Looks across the machine's drives for Dragon Age documents folders (ADR-0031). A last resort: it can take seconds.</summary>
public interface IDocumentsFolderSearch
{
    /// <summary>Gets every documents folder found, in a stable order. Empty when there is none.</summary>
    Task<IReadOnlyList<string>> FindAsync(CancellationToken cancellationToken = default);
}
