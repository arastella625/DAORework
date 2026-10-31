using DAORework.Core.Common;
using DAORework.Core.Mods.Adoption;
using DAORework.Core.Mods.Library;

namespace DAORework.Application.Abstractions;

/// <summary>
/// Imports a hand-installed mod into the library from where its files are (ADR-0015), as <c>mods import</c> imports a
/// folder, with <see cref="AdoptionLayout"/>'s paths and an adopted origin. An add-in's library copy gets a
/// <c>Manifest.xml</c> holding its <c>AddInItem</c> from <c>AddIns.xml</c> as it is. Reads the documents folder; writes
/// only the library.
/// </summary>
public interface IAdoptionImporter
{
    Task<Result<LibraryEntry>> ImportAsync(string documentsDirectory, AdoptionCandidate candidate, string name, CancellationToken cancellationToken = default);
}
