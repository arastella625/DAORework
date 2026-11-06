using DAORework.Core.Common;
using DAORework.Core.Mods.Troubleshooting;

namespace DAORework.Application.Abstractions;

/// <summary>Keeps which mods a documents folder was last played with (ADR-0043).</summary>
public interface IPlayHistoryStore
{
    /// <summary>The games so far, empty when there are none.</summary>
    Task<Result<PlayHistory>> LoadAsync(string documentsDirectory, CancellationToken cancellationToken = default);

    Task SaveAsync(string documentsDirectory, PlayHistory history, CancellationToken cancellationToken = default);
}
