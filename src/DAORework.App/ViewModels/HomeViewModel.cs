using System.Collections.Specialized;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DAORework.Core.Diagnostics;

namespace DAORework.App.ViewModels;

/// <summary>
/// Home, the first tab (ADR-0026, P2.5-25): whether the game is ready to play, the readiness strip, the last change with
/// <i>Undo</i>, and at most three suggestions. It shows what the other tabs know; their buttons take the user there.
/// </summary>
public sealed partial class HomeViewModel : ObservableObject
{
    private (BackupRow Row, HomeTarget Where)? _last;

    public HomeViewModel(NextStepsViewModel nextSteps)
    {
        NextSteps = nextSteps;
        Show(profileName: null);
    }

    /// <summary>Gets the suggestions ("Worth a look"); P2.5-07's Next steps, moved here.</summary>
    public NextStepsViewModel NextSteps { get; }

    /// <summary>Gets the readiness strip, once <see cref="Watch"/> has given it the Mods tab.</summary>
    [ObservableProperty]
    public partial SetupSummary? Setup { get; private set; }

    /// <summary>Gets or sets what a Home button does, set by the window: switch to a tab and open the right part.</summary>
    public Func<HomeTarget, BackupRow?, CancellationToken, Task>? NavigateAsync { get; set; }

    /// <summary>Gets or sets how the game is checked, set by the window: the Diagnostics tab's check.</summary>
    public Func<CancellationToken, Task>? CheckAsync { get; set; }

    [ObservableProperty]
    public partial string ReadyTitle { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ReadyBody { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NotReady))]
    public partial bool IsReady { get; set; }

    /// <summary>Gets or sets whether the game has a profile but has not been checked yet.</summary>
    [ObservableProperty]
    public partial bool CanCheck { get; set; }

    [ObservableProperty]
    public partial bool HasProblems { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasLastChange))]
    public partial string? LastChange { get; set; }

    [ObservableProperty]
    public partial DateTimeOffset? LastChangeAt { get; set; }

    public bool NotReady => !IsReady;

    public bool HasLastChange => LastChange is not null;

    /// <summary>Follows the Mods and Fixes tabs, so the readiness strip and the last change stay current.</summary>
    public void Watch(ModsTabViewModel mods, FixesTabViewModel fixes)
    {
        ArgumentNullException.ThrowIfNull(mods);
        ArgumentNullException.ThrowIfNull(fixes);
        Setup = new SetupSummary(mods);
        NotifyCollectionChangedEventHandler changed = (_, _) => ShowLastChange(mods.Changes.Backups, fixes.Status.Backups);
        mods.Changes.Backups.CollectionChanged += changed;
        fixes.Status.Backups.CollectionChanged += changed;
    }

    /// <summary>Starts over for <paramref name="profileName"/>: not checked yet, or nothing to check without a profile.</summary>
    public void Show(string? profileName)
    {
        IsReady = false;
        HasProblems = false;
        CanCheck = profileName is not null;
        ReadyTitle = profileName is null ? "Welcome" : "Not checked yet";
        ReadyBody = profileName is null
            ? "Create a profile in the header and point it at your game to get started."
            : "Check the game to see whether it is ready to play.";
        NextSteps.Clear();
    }

    /// <summary>Shows a finished check: ready, or how many problems stand in the way; then the suggestions.</summary>
    public Task ShowReportAsync(string profileName, IReadOnlyList<DiagnosticIssue> issues, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(issues);
        var errors = issues.Count(i => i.Severity == DiagnosticSeverity.Error);
        IsReady = errors == 0;
        HasProblems = errors > 0;
        CanCheck = false;
        ReadyTitle = IsReady ? "Ready to play" : "Not ready yet";
        ReadyBody = IsReady
            ? $"{profileName} is set up and the game can start."
            : $"{Counted.Of(errors, "problem")} to fix before the game can start.";
        return NextSteps.ShowAsync(profileName, issues, cancellationToken);
    }

    [RelayCommand]
    private Task CheckGameAsync(CancellationToken cancellationToken) => CheckAsync?.Invoke(cancellationToken) ?? Task.CompletedTask;

    [RelayCommand]
    private Task OpenAsync(HomeTarget target, CancellationToken cancellationToken) =>
        NavigateAsync is { } open ? open(target, null, cancellationToken) : Task.CompletedTask;

    [RelayCommand]
    private Task UndoLastAsync(CancellationToken cancellationToken) =>
        _last is { } last && NavigateAsync is { } open ? open(last.Where, last.Row, cancellationToken) : Task.CompletedTask;

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
