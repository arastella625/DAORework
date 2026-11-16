using DAORework.Core.Common;
using DAORework.Core.Mods.AddIns;

namespace DAORework.Application.Abstractions;

/// <summary>
/// Reads the add-ins the game loads from a documents folder (P2.5-23): <c>Settings\AddIns.xml</c>, and the file names
/// in each add-in's folder, archives' tables of contents included. Never writes.
/// </summary>
public interface IDocumentsAddInsReader
{
    /// <returns>The add-ins, or an error when <c>AddIns.xml</c> can't be read. No <c>AddIns.xml</c> is no add-ins.</returns>
    Task<Result<DocumentsAddIns>> ReadAsync(string documentsDirectory, CancellationToken cancellationToken = default);
}
