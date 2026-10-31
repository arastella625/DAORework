using DAORework.Application.Abstractions;
using DAORework.Core.Common;

namespace DAORework.Application.Looks;

/// <summary>Loads a custom look and its picture so the App can show it (ADR-0017).</summary>
public sealed class LoadLook
{
    private readonly ILookStore _looks;
    private readonly IBackgroundImageImporter _pictures;

    public LoadLook(ILookStore looks, IBackgroundImageImporter pictures)
    {
        _looks = looks;
        _pictures = pictures;
    }

    public async Task<Result<LoadedLook>> ExecuteAsync(LoadLookRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!(await _looks.LoadAsync(request.LookId, cancellationToken)).TryGetValue(out var look, out var error))
        {
            return Result.Failure<LoadedLook>(error);
        }

        if (look.Background is null)
        {
            return Result.Success(new LoadedLook(look, null, null));
        }

        var picture = await _pictures.ReadAsync(look.Id, look.Background.FileName, cancellationToken);
        return Result.Success(picture.TryGetValue(out var bytes, out var pictureError)
            ? new LoadedLook(look, bytes, null)
            : new LoadedLook(look, null, pictureError));
    }
}
