using DAORework.Application.Looks;
using DAORework.Core.Common;
using DAORework.Core.Looks;

namespace DAORework.App.Themes;

/// <summary>
/// Changes a look's picture and shows the result at once (ADR-0017). The veil is never saved below the floor the
/// look's own colors need, worked out here because only the App has the theme's colors.
/// </summary>
public sealed class LookPictureEditor
{
    private readonly SetLookBackground _setBackground;
    private readonly RemoveLookBackground _removeBackground;
    private readonly AdjustLookBackground _adjustBackground;
    private readonly LookPresenter _presenter;
    private readonly ThemeSwitcher _switcher;

    public LookPictureEditor(
        SetLookBackground setBackground, RemoveLookBackground removeBackground, AdjustLookBackground adjustBackground, LookPresenter presenter, ThemeSwitcher switcher)
    {
        _setBackground = setBackground;
        _removeBackground = removeBackground;
        _adjustBackground = adjustBackground;
        _presenter = presenter;
        _switcher = switcher;
    }

    /// <returns>The lowest veil, in percent, that keeps <paramref name="look"/>'s text readable at <paramref name="tint"/>; 0 without a picture.</returns>
    public int FloorOf(Look look, int tint)
    {
        ArgumentNullException.ThrowIfNull(look);
        if (look.Background is not { } background)
        {
            return 0;
        }

        var baseTheme = AppThemes.Find(look.BaseTheme) ?? AppThemes.Default;
        var (dictionary, _) = LookAppearance.Build(_switcher.DictionaryOf(baseTheme), look);
        return PictureFloor.Of(background, tint, dictionary);
    }

    public Task<Result<Look>> SetAsync(string lookId, string sourcePath, CancellationToken cancellationToken = default) =>
        ShowAsync(_setBackground.ExecuteAsync(new SetLookBackgroundRequest(lookId, sourcePath), cancellationToken), cancellationToken);

    public Task<Result<Look>> RemoveAsync(string lookId, CancellationToken cancellationToken = default) =>
        ShowAsync(_removeBackground.ExecuteAsync(new RemoveLookBackgroundRequest(lookId), cancellationToken), cancellationToken);

    public Task<Result<Look>> AdjustAsync(Look look, BackgroundFit fit, int tint, int dim, int blur, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(look);
        var request = new AdjustLookBackgroundRequest(look.Id, fit, tint, dim, blur, FloorOf(look, tint));
        return ShowAsync(_adjustBackground.ExecuteAsync(request, cancellationToken), cancellationToken);
    }

    private async Task<Result<Look>> ShowAsync(Task<Result<Look>> change, CancellationToken cancellationToken)
    {
        var result = await change;
        if (result.TryGetValue(out var look, out _))
        {
            await _presenter.ShowAsync(look.Id, cancellationToken);
        }

        return result;
    }
}
