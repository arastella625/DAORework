using DAORework.Application.Abstractions;
using DAORework.Application.Profiles;
using DAORework.Core.Common;
using DAORework.Core.Saves;

namespace DAORework.Application.Saves;

/// <summary>A profile's save backups, newest first.</summary>
public sealed class ListSaveBackups
{
    private readonly GetProfile _getProfile;
    private readonly ISaveBackupStore _store;

    public ListSaveBackups(GetProfile getProfile, ISaveBackupStore store)
    {
        _getProfile = getProfile;
        _store = store;
    }

    public async Task<Result<IReadOnlyList<SaveBackup>>> ExecuteAsync(ListSaveBackupsRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!(await _getProfile.ExecuteAsync(new GetProfileRequest(request.ProfileName), cancellationToken)).TryGetValue(out var profile, out var error))
        {
            return Result.Failure<IReadOnlyList<SaveBackup>>(error);
        }

        return Result.Success(await _store.ListAsync(profile.Name, cancellationToken));
    }
}
