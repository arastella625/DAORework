using DAORework.Application.Abstractions;
using DAORework.Core.Common;
using DAORework.Core.Looks;

namespace DAORework.Application.Looks;

/// <summary>
/// Renames a look and changes its base theme and accent. The accent must be a color and not a red (ADR-0016 rule 3);
/// the App derives the readable shades from it against the base theme when the look is shown.
/// </summary>
public sealed class UpdateLook
{
    private readonly ILookStore _looks;

    public UpdateLook(ILookStore looks)
    {
        _looks = looks;
    }

    public async Task<Result<Look>> ExecuteAsync(UpdateLookRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.BaseTheme);
        if (!LookNames.ValidName(request.Name).TryGetValue(out var name, out var error))
        {
            return Result.Failure<Look>(error);
        }

        if (!ValidAccent(request.Accent).TryGetValue(out var accent, out error))
        {
            return Result.Failure<Look>(error);
        }

        if (!(await _looks.LoadAsync(request.LookId, cancellationToken)).TryGetValue(out var look, out error))
        {
            return Result.Failure<Look>(error);
        }

        var updated = look with { Name = name, BaseTheme = request.BaseTheme, Accent = accent };
        await _looks.SaveAsync(updated, cancellationToken);
        return Result.Success(updated);
    }

    private static Result<string?> ValidAccent(string? accent)
    {
        if (accent is null)
        {
            return Result.Success<string?>(null);
        }

        if (!LookColor.Parse(accent).TryGetValue(out var color, out var error)
            || !AccentRules.Allowed(color).TryGetValue(out color, out error))
        {
            return Result.Failure<string?>(error);
        }

        return Result.Success<string?>(color.ToString());
    }
}
