using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DAORework.Application.Preferences;
using DAORework.Core.Mods;
using Microsoft.Extensions.Logging;

namespace DAORework.App.ViewModels;

/// <summary>
/// The cozy guide (ADR-0051, P2.9-03): Home's one card while it runs, a step at a time, each with one button that goes
/// where the step is done. It follows where the player is, so a step done anywhere shows as done. Skipping or finishing
/// it is remembered; Settings brings it back.
/// </summary>
public sealed partial class CozyGuideViewModel(GetPreferences getPreferences, SetGuideClosed setGuideClosed, ILogger<CozyGuideViewModel> logger)
    : PageViewModel(logger)
{
    private NextMoveFacts _facts = new(false, false, false, 0, 0, 0);
    private bool _closed;
    private bool _seen;

    /// <summary>Gets or sets what a step's button does, set by Home: open the place it names, or play.</summary>
    public Func<GuideStepKind, CancellationToken, Task>? TakeAsync { get; set; }

    /// <summary>Gets the four steps, each marked done or not.</summary>
    public ObservableCollection<GuideStep> Steps { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowsFind), nameof(ShowsAdd), nameof(ShowsSwitch), nameof(ShowsPlay))]
    public partial GuideStep? Current { get; private set; }

    [ObservableProperty]
    public partial bool IsShown { get; private set; }

    /// <summary>Gets or sets a value indicating whether the guide was finished in this session, so Home says so once.</summary>
    [ObservableProperty]
    public partial bool JustFinished { get; set; }

    public bool ShowsFind => Current?.Kind == GuideStepKind.FindYourGame;

    public bool ShowsAdd => Current?.Kind == GuideStepKind.AddMods;

    public bool ShowsSwitch => Current?.Kind == GuideStepKind.TurnThemOn;

    public bool ShowsPlay => Current?.Kind == GuideStepKind.Play;

    /// <summary>Reads whether the guide was closed. A preferences file that can't be read shows the guide.</summary>
    public Task LoadAsync(CancellationToken cancellationToken = default) =>
        GuardAsync(
            async token =>
            {
                _closed = (await getPreferences.ExecuteAsync(new GetPreferencesRequest(), token)).TryGetValue(out var preferences, out _) && preferences.GuideClosed;
                Show();
            },
            cancellationToken);

    /// <summary>Follows where the player is: a step done anywhere in the window shows as done here.</summary>
    public void Follow(NextMoveFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        _facts = facts;
        Show();
    }

    [RelayCommand]
    private async Task TakeStepAsync(CancellationToken cancellationToken)
    {
        if (Current is not { } step || TakeAsync is null)
        {
            return;
        }

        await TakeAsync(step.Kind, cancellationToken);
        if (step.Kind == GuideStepKind.Play)
        {
            await CloseAsync(finished: true, cancellationToken);
        }
    }

    [RelayCommand]
    private Task SkipAsync(CancellationToken cancellationToken) => CloseAsync(finished: false, cancellationToken);

    /// <summary>Settings › <i>Show the guide again</i>: back at the first step not done.</summary>
    [RelayCommand]
    private Task ShowAgainAsync(CancellationToken cancellationToken) =>
        GuardAsync(
            async token =>
            {
                await setGuideClosed.ExecuteAsync(closed: false, token);
                _closed = false;
                _seen = true;
                JustFinished = false;
                Show();
                ReportSuccess("The guide is back on Home.");
            },
            cancellationToken);

    private Task CloseAsync(bool finished, CancellationToken cancellationToken) =>
        GuardAsync(
            async token =>
            {
                await setGuideClosed.ExecuteAsync(closed: true, token);
                _closed = true;
                JustFinished = finished;
                Show();
            },
            cancellationToken);

    private void Show()
    {
        var steps = CozyGuide.For(_facts);
        IsShown = CozyGuide.Shows(_closed, _facts, _seen);
        _seen |= IsShown;
        Steps.Clear();
        foreach (var step in steps)
        {
            Steps.Add(step);
        }

        Current = steps[CozyGuide.Current(steps)];
    }
}
