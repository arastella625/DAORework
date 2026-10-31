using DAORework.Application.Abstractions;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Looks;

namespace DAORework.Application.Looks;

/// <summary>
/// Changes how a look's picture is blended in. Each value is kept to its range, and the dim never goes below the floor,
/// so text stays readable over any picture (ADR-0017); the editor shows the floor and says why.
/// </summary>
public sealed class AdjustLookBackground
{
    private readonly ILookStore _looks;

    public AdjustLookBackground(ILookStore looks)
    {
        _looks = looks;
    }

    public async Task<Result<Look>> ExecuteAsync(AdjustLookBackgroundRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!(await _looks.LoadAsync(request.LookId, cancellationToken)).TryGetValue(out var look, out var error))
        {
            return Result.Failure<Look>(error);
        }

        if (look.Background is null)
        {
            return Result.Failure<Look>(new Error(DiagnosticCodes.LookBackgroundMissing, $"The look '{look.Name}' has no background picture to adjust."));
        }

        var floor = Math.Clamp(request.DimFloor, 0, 100);
        var background = look.Background with
        {
            Fit = request.Fit,
            Tint = Math.Clamp(request.Tint, 0, 100),
            Dim = Math.Clamp(request.Dim, floor, 100),
            Blur = Math.Clamp(request.Blur, 0, LookBackground.MaxBlur),
        };
        var updated = look with { Background = background };
        await _looks.SaveAsync(updated, cancellationToken);
        return Result.Success(updated);
    }
}
