using DAORework.Core.Common;
using DAORework.Core.Mods.Library;

namespace DAORework.Application.Abstractions;

/// <summary>
/// DAORework's mod library under app-data (ADR-0012, ROADMAP P2-07). Importing reads the download and copies its
/// files into a new folder of the library; it never writes anywhere else, and a library entry is never changed after.
/// </summary>
public interface IModLibrary
{
    /// <summary>Imports a <c>.dazip</c>, a <c>.zip</c> or an extracted folder. Importing the same download again returns the entry already there.</summary>
    Task<Result<LibraryImport>> ImportAsync(string sourcePath, CancellationToken cancellationToken = default);

    /// <summary>Every mod in the library, sorted by name.</summary>
    Task<IReadOnlyList<LibraryEntry>> ListAsync(CancellationToken cancellationToken = default);
}
