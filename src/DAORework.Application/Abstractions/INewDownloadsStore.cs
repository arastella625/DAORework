using DAORework.Core.Common;
using DAORework.Core.Mods.Downloads;

namespace DAORework.Application.Abstractions;

/// <summary><c>new-downloads.json</c> under app-data (ADR-0034 §9). Written atomically.</summary>
public interface INewDownloadsStore
{
    /// <summary>The settings; off, and nothing dealt with, when none were saved.</summary>
    Task<Result<NewDownloadsSettings>> LoadAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(NewDownloadsSettings settings, CancellationToken cancellationToken = default);
}
