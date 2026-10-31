using DAORework.Application.Abstractions;
using DAORework.Core.Common;
using DAORework.Core.Looks;

namespace DAORework.Application.Looks;

/// <summary>
/// Sets a look's background picture from a file the user picked (ADR-0017). A copy goes into the look's folder; the
/// picked file is only read. A first picture starts at the default tint, dim and blur; a replacement keeps the settings.
/// </summary>
public sealed class SetLookBackground
{
    private readonly ILookStore _looks;
    private readonly IBackgroundImageImporter _importer;

    public SetLookBackground(ILookStore looks, IBackgroundImageImporter importer)
    {
        _looks = looks;
        _importer = importer;
    }

    public async Task<Result<Look>> ExecuteAsync(SetLookBackgroundRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.SourcePath);
        if (!(await _looks.LoadAsync(request.LookId, cancellationToken)).TryGetValue(out var look, out var error))
        {
            return Result.Failure<Look>(error);
        }

        if (!(await _importer.ImportAsync(request.SourcePath, look.Id, cancellationToken)).TryGetValue(out var picture, out error))
        {
            return Result.Failure<Look>(error);
        }

        var background = look.Background is { } old
            ? old with { FileName = picture.FileName, Brightest = picture.Brightest.ToString() }
            : new LookBackground { FileName = picture.FileName, Brightest = picture.Brightest.ToString() };
        var updated = look with { Background = background };
        await _looks.SaveAsync(updated, cancellationToken);
        return Result.Success(updated);
    }
}
