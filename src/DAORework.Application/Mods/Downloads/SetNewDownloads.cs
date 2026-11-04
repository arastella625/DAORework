using DAORework.Application.Abstractions;
using DAORework.Core.Common;
using DAORework.Core.Mods.Downloads;

namespace DAORework.Application.Mods.Downloads;

/// <summary>
/// Turns picking up downloads on or off, and names the folder (ADR-0034 §1 and §2). Only files that arrive after it is
/// turned on are offered, so turning it on notes the moment. Answering at all means Home doesn't ask again.
/// </summary>
public sealed class SetNewDownloads
{
    private readonly INewDownloadsStore _store;
    private readonly TimeProvider _clock;

    public SetNewDownloads(INewDownloadsStore store, TimeProvider clock)
    {
        _store = store;
        _clock = clock;
    }

    public async Task<Result<NewDownloadsSettings>> ExecuteAsync(bool enabled, string? folder, CancellationToken cancellationToken = default)
    {
        if (!(await _store.LoadAsync(cancellationToken)).TryGetValue(out var settings, out var error))
        {
            return Result.Failure<NewDownloadsSettings>(error);
        }

        var changed = settings with
        {
            Enabled = enabled,
            Folder = folder,
            EnabledAtUtc = enabled && !settings.Enabled ? _clock.GetUtcNow() : settings.EnabledAtUtc,
            Asked = true,
        };
        await _store.SaveAsync(changed, cancellationToken);
        return Result.Success(changed);
    }
}
