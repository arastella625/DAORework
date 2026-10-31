using DAORework.Application.Abstractions;
using DAORework.Core.Common;
using DAORework.Core.Looks;

namespace DAORework.Application.Looks;

/// <summary>Makes a new custom look from a built-in theme (ADR-0017: <i>Customize</i> makes an editable copy).</summary>
public sealed class CreateLook
{
    private readonly ILookStore _looks;

    public CreateLook(ILookStore looks)
    {
        _looks = looks;
    }

    public async Task<Result<Look>> ExecuteAsync(CreateLookRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.BaseTheme); // The App passes one of its own theme ids.
        if (!LookNames.ValidName(request.Name).TryGetValue(out var name, out var error))
        {
            return Result.Failure<Look>(error);
        }

        var look = new Look { Id = LookNames.NewId(), Name = name, BaseTheme = request.BaseTheme };
        await _looks.SaveAsync(look, cancellationToken);
        return Result.Success(look);
    }
}
