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
    private NextMoveFacts? _facts;
    private bool _loaded;
    private bool _closed;
    private bool _seen;

    /// <summary>Gets or sets how a place is opened, set by Home.</summary>
    public Func<HomeTarget, CancellationToken, Task>? OpenAsync { get; set; }

    /// <summary>Gets or sets how the game is started, set by Home; true when it started.</summary>
    public Func<CancellationToken, Task<bool>>? PlayAsync { get; set; }

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
                _loaded = true;
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

    /// <summary>Each step's button goes where the step is done (ADR-0051 §1); <i>Play</i> finishes the guide once the game starts.</summary>
    [RelayCommand]
    private async Task TakeStepAsync(CancellationToken cancellationToken)
    {
        switch (Current?.Kind)
        {
            case GuideStepKind.Play when PlayAsync is { } play && await play(cancellationToken):
                await CloseAsync(finished: true, cancellationToken);
                break;
            case GuideStepKind.FindYourGame:
                await OpenTargetAsync(HomeTarget.GameFolders, cancellationToken);
                break;
            case GuideStepKind.AddMods or GuideStepKind.TurnThemOn:
                await OpenTargetAsync(HomeTarget.ModsLibrary, cancellationToken);
                break;
            default:
                break;
        }
    }

    private Task OpenTargetAsync(HomeTarget target, CancellationToken cancellationToken) =>
        OpenAsync?.Invoke(target, cancellationToken) ?? Task.CompletedTask;

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

    // Nothing shows until both the choice to close it and where the player is are known, so it never flashes up.
    private void Show()
    {
        var facts = _facts ?? new NextMoveFacts(false, false, false, 0, 0, 0);
        var steps = CozyGuide.For(facts);
        IsShown = _loaded && _facts is not null && CozyGuide.Shows(_closed, facts, _seen);
        _seen |= IsShown;
        Steps.Clear();
        foreach (var step in steps)
        {
            Steps.Add(step);
        }

        Current = steps[CozyGuide.Current(steps)];
    }
}
