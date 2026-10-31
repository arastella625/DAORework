using DAORework.Core.Common;
using DAORework.Core.Mods.Adoption;

namespace DAORework.Application.Abstractions;

/// <summary>Reads what is installed in a documents folder's <c>AddIns</c> and <c>override</c> folders for the adoption scan (ADR-0015). Read only.</summary>
public interface IHandInstalledModsReader
{
    Task<Result<HandInstalledSnapshot>> ReadAsync(string documentsDirectory, CancellationToken cancellationToken = default);
}
