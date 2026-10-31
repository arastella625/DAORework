using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DAORework.App.Services;
using DAORework.App.Themes;
using DAORework.Core.Looks;
using Microsoft.Extensions.Logging;

namespace DAORework.App.ViewModels;

/// <summary>
/// A look's picture (ADR-0017): choose one, take it away, and blend it in with fit, tint, veil and blur. Each change
/// shows and saves once the controls settle, and the veil cannot go below the floor that keeps text readable.
/// </summary>
public sealed partial class LookPictureViewModel : PageViewModel
{
    private static readonly string[] _patterns = ["*.png", "*.jpg", "*.jpeg", "*.webp"]; // What the importer takes.
    private static readonly TimeSpan _settle = TimeSpan.FromMilliseconds(300);

    private readonly LookPictureEditor _editor;
    private readonly IPathPicker _picker;
    private bool _loading;
    private int _changes;

    public LookPictureViewModel(LookPictureEditor editor, IPathPicker picker, ILogger<LookPictureViewModel> logger)
        : base(logger)
    {
        _editor = editor;
        _picker = picker;
    }

    /// <summary>Raised after the picture or its settings are saved, so the list holds the saved look.</summary>
    public event EventHandler<Look>? Changed;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasLook), nameof(HasPicture))]
    public partial Look? Look { get; set; }

    [ObservableProperty]
    public partial bool FillWindow { get; set; } = true;

    [ObservableProperty]
    public partial int Tint { get; set; }

    [ObservableProperty]
    public partial int Dim { get; set; }

    [ObservableProperty]
    public partial int Blur { get; set; }

    /// <summary>Gets the lowest veil, in percent, at which text stays readable over this picture.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FloorNote))]
    public partial int Floor { get; set; }

    public bool HasLook => Look is not null;

    public bool HasPicture => Look?.Background is not null;

    /// <summary>Gets the blur slider's top, as the slider takes it.</summary>
    public static double MaxBlur => LookBackground.MaxBlur;

    public string FloorNote => $"The veil stays at {Floor}% or more over this picture, so text on it stays readable.";

    /// <summary>Gets the save the latest change started; it finishes once the controls have been still for a moment.</summary>
    public Task Settled { get; private set; } = Task.CompletedTask;

    public void Edit(Look? look)
    {
        _loading = true;
        Look = look;
        var background = look?.Background ?? new LookBackground { FileName = string.Empty, Brightest = string.Empty };
        FillWindow = background.Fit == BackgroundFit.Fill;
        Tint = background.Tint;
        Dim = background.Dim;
        Blur = background.Blur;
        Floor = look is null ? 0 : _editor.FloorOf(look, Tint);
        _loading = false;
        ReportSuccess(string.Empty);
    }

    partial void OnFillWindowChanged(bool value) => Settle();

    partial void OnTintChanged(int value)
    {
        if (!_loading && Look is not null)
        {
            Floor = _editor.FloorOf(Look, value);
        }

        Settle();
    }

    partial void OnDimChanged(int value) => Settle();

    partial void OnBlurChanged(int value) => Settle();

    [RelayCommand]
    private Task ChooseAsync(CancellationToken cancellationToken) =>
        GuardAsync(
            async token =>
            {
                if (Look is null || await _picker.PickFileAsync("Choose a picture for this look", "Pictures", _patterns, token) is not { } path)
                {
                    return;
                }

                if (!(await _editor.SetAsync(Look.Id, path, token)).TryGetValue(out var look, out var error))
                {
                    ReportError(error);
                    return;
                }

                Accept(look);
                Edit(look);
                ReportSuccess("Added your picture. DAORework keeps its own copy; your file is not changed.");
            },
            cancellationToken);

    [RelayCommand]
    private Task RemoveAsync(CancellationToken cancellationToken) =>
        GuardAsync(
            async token =>
            {
                if (Look is null)
                {
                    return;
                }

                if (!(await _editor.RemoveAsync(Look.Id, token)).TryGetValue(out var look, out var error))
                {
                    ReportError(error);
                    return;
                }

                Accept(look);
                Edit(look);
                ReportSuccess("Took the picture away. Your own file is untouched.");
            },
            cancellationToken);

    [RelayCommand]
    private Task ApplyAsync(CancellationToken cancellationToken) =>
        GuardAsync(
            async token =>
            {
                if (Look is not { Background: not null } current)
                {
                    return;
                }

                var fit = FillWindow ? BackgroundFit.Fill : BackgroundFit.Fit;
                if (!(await _editor.AdjustAsync(current, fit, Tint, Dim, Blur, token)).TryGetValue(out var look, out var error))
                {
                    ReportError(error);
                    return;
                }

                Accept(look); // The controls keep what the user is doing now; the saved look may already be behind it.
                ReportSuccess(string.Empty);
            },
            cancellationToken);

    private void Accept(Look look)
    {
        Look = look;
        Changed?.Invoke(this, look);
    }

    // Saves once the controls have been still for a moment, so dragging a slider saves once, not at every step.
    private void Settle()
    {
        if (_loading || !HasPicture)
        {
            return;
        }

        var change = ++_changes;
        Settled = SettleAsync(change);
    }

    private async Task SettleAsync(int change)
    {
        await Task.Delay(_settle);
        if (change == _changes)
        {
            await ApplyCommand.ExecuteAsync(null);
        }
    }
}
