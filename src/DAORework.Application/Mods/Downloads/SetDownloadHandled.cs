using DAORework.Application.Abstractions;
using DAORework.Core.Common;
using DAORework.Core.Mods.Downloads;

namespace DAORework.Application.Mods.Downloads;

/// <summary>Notes a download as dealt with, so it isn't offered again (ADR-0034 §5).</summary>
public sealed class SetDownloadHandled
{
    private readonly INewDownloadsStore _store;

    public SetDownloadHandled(INewDownloadsStore store)
    {
        _store = store;
    }

    public async Task<Result> ExecuteAsync(DownloadFile file, CancellationToken cancellationToken = default)
    {
        if (!(await _store.LoadAsync(cancellationToken)).TryGetValue(out var settings, out var error))
        {
            return Result.Failure(error);
        }

        await _store.SaveAsync(settings with { Handled = [.. settings.Handled, HandledDownload.From(file)] }, cancellationToken);
        return Result.Success();
    }
}
