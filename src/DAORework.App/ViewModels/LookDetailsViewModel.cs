using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DAORework.App.Themes;
using DAORework.Application.Looks;
using DAORework.Core.Looks;
using Microsoft.Extensions.Logging;

namespace DAORework.App.ViewModels;

/// <summary>
/// Edits a custom look's name, base theme and accent (ADR-0017). Saving shows the look at once, so the user sees the
/// accent as the app will show it: the same hue, with its shades adjusted until the text is readable.
/// </summary>
public sealed partial class LookDetailsViewModel : PageViewModel
{
    private readonly UpdateLook _updateLook;
    private readonly LookPresenter _presenter;

    public LookDetailsViewModel(UpdateLook updateLook, LookPresenter presenter, ILogger<LookDetailsViewModel> logger)
        : base(logger)
    {
        _updateLook = updateLook;
        _presenter = presenter;
        BaseTheme = AppThemes.Default;
    }

    /// <summary>Raised after a look is saved, so the list can show its new name.</summary>
    public event EventHandler<Look>? Saved;

    public IReadOnlyList<AppTheme> Themes { get; } = AppThemes.All;

    /// <summary>Gets accents to start from. None is a red: red is kept for danger (ADR-0016 rule 3).</summary>
    public IReadOnlyList<string> SuggestedAccents { get; } = ["#8B5CF6", "#B9A3FF", "#4F8A6E", "#3BA3D9", "#A0703A", "#C9A227"];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasLook))]
    public partial Look? Look { get; set; }

    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    public partial AppTheme BaseTheme { get; set; }

    [ObservableProperty]
    public partial bool UseThemeAccent { get; set; } = true;

    [ObservableProperty]
    public partial string Accent { get; set; } = string.Empty;

    public bool HasLook => Look is not null;

    public void Edit(Look? look)
    {
        Look = look;
        Name = look?.Name ?? string.Empty;
        BaseTheme = AppThemes.Find(look?.BaseTheme) ?? AppThemes.Default;
        UseThemeAccent = look?.Accent is null;
        Accent = look?.Accent ?? string.Empty;
        ReportSuccess(string.Empty);
    }

    [RelayCommand]
    private void PickAccent(string accent)
    {
        UseThemeAccent = false;
        Accent = accent;
    }

    [RelayCommand]
    private Task SaveAsync(CancellationToken cancellationToken) =>
        GuardAsync(
            async token =>
            {
                if (Look is null)
                {
                    return;
                }

                var request = new UpdateLookRequest(Look.Id, Name, BaseTheme.Id, UseThemeAccent ? null : Accent);
                if (!(await _updateLook.ExecuteAsync(request, token)).TryGetValue(out var saved, out var error))
                {
                    ReportError(error);
                    return;
                }

                Look = saved;
                await _presenter.ShowAsync(saved.Id, token);
                ReportSuccess($"Saved {saved.Name}.");
                Saved?.Invoke(this, saved);
            },
            cancellationToken);
}
