using DAORework.Application.Abstractions;
using DAORework.Core.Common;
using DAORework.Core.Mods.Downloads;

namespace DAORework.Application.Mods.Downloads;

/// <summary>Reads whether DAORework looks for new downloads, and where (ADR-0034 §1).</summary>
public sealed class GetNewDownloadsSettings
{
    private readonly INewDownloadsStore _store;

    public GetNewDownloadsSettings(INewDownloadsStore store)
    {
        _store = store;
    }

    public Task<Result<NewDownloadsSettings>> ExecuteAsync(CancellationToken cancellationToken = default) => _store.LoadAsync(cancellationToken);
}
