using System.Collections.Specialized;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DAORework.Core.Diagnostics;
using DAORework.Core.Mods;

namespace DAORework.App.ViewModels;

/// <summary>
/// Home, the first tab (ADR-0026, P2.5-25): one card with a headline, a sentence and a button (ADR-0050 §1), the last change
/// with <i>Undo</i>, and at most three suggestions. It shows what the other tabs know; their buttons take the user there.
/// </summary>
public sealed partial class HomeViewModel : ObservableObject
{
    private (BackupRow Row, HomeTarget Where)? _last;
    private GameCheck? _check;
    private bool _hasProfile;

    public HomeViewModel(NextStepsViewModel nextSteps)
    {
        NextSteps = nextSteps;
        Show(profileName: null);
    }

    /// <summary>Gets the suggestions ("Worth a look"); P2.5-07's Next steps, moved here.</summary>
    public NextStepsViewModel NextSteps { get; }

    /// <summary>Gets the readiness strip, once <see cref="Watch"/> has given it the Mods tab; on Home only for <i>Find the problem mod</i>.</summary>
    [ObservableProperty]
    public partial SetupSummary? Setup { get; private set; }

    /// <summary>Gets the one next step, once <see cref="Watch"/> has given it the Mods tab (P2.6-27); the card says it.</summary>
    [ObservableProperty]
    public partial NextMoveCard? NextMove { get; private set; }

    /// <summary>Gets the question about new downloads and how many are waiting, once <see cref="Watch"/> has given it the Mods tab.</summary>
    [ObservableProperty]
    public partial NewDownloadsViewModel? NewDownloads { get; private set; }

    /// <summary>Gets or sets what a Home button does, set by the window: switch to a tab and open the right part.</summary>
    public Func<HomeTarget, BackupRow?, CancellationToken, Task>? NavigateAsync { get; set; }

    /// <summary>Gets or sets how the game is started, set by the window: the header's <i>Play</i>.</summary>
    public Func<CancellationToken, Task>? PlayAsync { get; set; }

    /// <summary>Gets Home's one card: its headline, sentence and button (P2.10-02).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsReady), nameof(HasProblems), nameof(HasButton))]
    public partial HomeHeadline Card { get; private set; } = HomeHeadlines.For(NextMoves.For(new NextMoveFacts(false, false, false, 0, 0, 0)), null);

    public bool IsReady => Card.Kind is HomeHeadlineKind.Ready or HomeHeadlineKind.ReadyWithoutMods;

    public bool HasProblems => Card.Kind == HomeHeadlineKind.NotReady;

    public bool HasButton => Card.Button is not null;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasLastChange))]
    public partial string? LastChange { get; set; }

    [ObservableProperty]
    public partial DateTimeOffset? LastChangeAt { get; set; }

    public bool HasLastChange => LastChange is not null;

    /// <summary>Follows the Mods and Fixes tabs, so the readiness strip and the last change stay current.</summary>
    public void Watch(ModsTabViewModel mods, FixesTabViewModel fixes)
    {
        ArgumentNullException.ThrowIfNull(mods);
        ArgumentNullException.ThrowIfNull(fixes);
        Setup = mods.Setup;
        NextMove = mods.NextMove;
        NextMove.Watch(fixes.Status);
        NextMove.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(NextMoveCard.Move))
            {
                ShowCard();
            }
        };
        NewDownloads = mods.NewDownloads;
        NotifyCollectionChangedEventHandler changed = (_, _) => ShowLastChange(mods.Changes.Backups, fixes.Status.Backups);
        mods.Changes.Backups.CollectionChanged += changed;
        fixes.Status.Backups.CollectionChanged += changed;
        ShowCard();
    }

    [RelayCommand]
    private Task SeeNewDownloadsAsync(CancellationToken cancellationToken) =>
        NavigateAsync?.Invoke(HomeTarget.ModsLibrary, null, cancellationToken) ?? Task.CompletedTask;

    /// <summary>Starts over for <paramref name="profileName"/>: the game is being checked, or there is nothing to check without a profile.</summary>
    public void Show(string? profileName)
    {
        _hasProfile = profileName is not null;
        _check = null;
        Setup?.GameReadsElsewhere = false;
        NextSteps.Clear();
        ShowCard();
    }

    /// <summary>
    /// Shows a finished check on the card, then the suggestions. A profile on a folder the game doesn't read can still
    /// start the game (ADR-0046), but nothing here may say all is well with its mods.
    /// </summary>
    public Task ShowReportAsync(string profileName, IReadOnlyList<DiagnosticIssue> issues, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(issues);
        _check = GameCheck.From(issues);
        Setup?.GameReadsElsewhere = _check.ModsElsewhere;
        ShowCard();
        return NextSteps.ShowAsync(profileName, issues, cancellationToken);
    }

    /// <summary>Says <i>Checking your game</i> until the check asked for now ends, so nothing says ready for a change not yet checked.</summary>
    public void ShowChecking()
    {
        _check = null;
        ShowCard();
    }

    /// <summary>Ends a check: one that showed no report couldn't run, and the card says so instead of checking for ever.</summary>
    public void ShowCheckEnded()
    {
        _check ??= GameCheck.CouldNotRun;
        ShowCard();
    }

    [RelayCommand]
    private Task TakeCardAsync(CancellationToken cancellationToken) => Card.Action switch
    {
        HomeButton.TakeNextMove when NextMove is not null => NextMove.TakeCommand.ExecuteAsync(null),
        HomeButton.SeeProblems => OpenAsync(HomeTarget.Diagnostics, cancellationToken),
        HomeButton.GameFolders => OpenAsync(HomeTarget.GameFolders, cancellationToken),
        HomeButton.Play => PlayAsync?.Invoke(cancellationToken) ?? Task.CompletedTask,
        _ => Task.CompletedTask,
    };

    [RelayCommand]
    private Task OpenAsync(HomeTarget target, CancellationToken cancellationToken) =>
        NavigateAsync is { } open ? open(target, null, cancellationToken) : Task.CompletedTask;

    [RelayCommand]
    private Task UndoLastAsync(CancellationToken cancellationToken) =>
        _last is { } last && NavigateAsync is { } open ? open(last.Where, last.Row, cancellationToken) : Task.CompletedTask;

    // Before the Mods tab is watched, only whether there is a profile is known.
    private void ShowCard() => Card = HomeHeadlines.For(NextMove?.Move ?? NextMoves.For(new NextMoveFacts(_hasProfile, false, false, 0, 0, 0)), _check);

    private void ShowLastChange(IEnumerable<BackupRow> mods, IEnumerable<BackupRow> fixes)
    {
        _last = mods.Select(row => (Row: row, Where: HomeTarget.ModsHistory))
            .Concat(fixes.Select(row => (Row: row, Where: HomeTarget.FixesHistory)))
            .OrderByDescending(entry => entry.Row.At)
            .Select(entry => ((BackupRow Row, HomeTarget Where)?)entry)
            .FirstOrDefault();
        LastChange = _last?.Row.Change ?? (_last is null ? null : "A change");
        LastChangeAt = _last?.Row.At;
    }
}
