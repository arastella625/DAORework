using DAORework.Application.Abstractions;
using DAORework.Core.Common;
using DAORework.Core.Looks;

namespace DAORework.Application.Looks;

/// <summary>Takes a look's background picture away, deleting the look's copy of it. The user's own file is untouched.</summary>
public sealed class RemoveLookBackground
{
    private readonly ILookStore _looks;
    private readonly IBackgroundImageImporter _importer;

    public RemoveLookBackground(ILookStore looks, IBackgroundImageImporter importer)
    {
        _looks = looks;
        _importer = importer;
    }

    public async Task<Result<Look>> ExecuteAsync(RemoveLookBackgroundRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!(await _looks.LoadAsync(request.LookId, cancellationToken)).TryGetValue(out var look, out var error))
        {
            return Result.Failure<Look>(error);
        }

        if (look.Background is null)
        {
            return Result.Success(look);
        }

        var updated = look with { Background = null };
        await _looks.SaveAsync(updated, cancellationToken);
        await _importer.RemoveAsync(look.Id, look.Background.FileName, cancellationToken);
        return Result.Success(updated);
    }
}
