using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DAORework.Application.Abstractions;
using DAORework.Application.Mods.Apply;
using DAORework.Application.Mods.Changes;
using DAORework.Application.Profiles;
using DAORework.Core.Mods.Planning;
using DAORework.Core.Mods.Troubleshooting;
using Microsoft.Extensions.Logging;

namespace DAORework.App.ViewModels;

/// <summary>
/// <i>Find the problem mod</i> (ADR-0042): each round is an ordinary preview and <i>Apply</i>, and the search moves on
/// only once that is done, so a cancelled preview leaves it where it was. It is kept between rounds, since a round means
/// playing the game.
/// </summary>
public sealed partial class ProblemSearchViewModel : PageViewModel
{
    private readonly GetProfile _getProfile;
    private readonly PlanTurnOn _plan;
    private readonly ChangeInProgress _apply;
    private readonly IProblemSearchStore _store;
    private (string Documents, ModActivationPlan Plan, ProblemSearch? Next, string Done)? _pending;

    public ProblemSearchViewModel(GetProfile getProfile, PlanTurnOn plan, ChangeInProgress apply, IProblemSearchStore store, ILogger<ProblemSearchViewModel> logger)
        : base(logger)
    {
        _getProfile = getProfile;
        _plan = plan;
        _apply = apply;
        _store = store;
    }

    /// <summary>Gets or sets what is on now, in its order.</summary>
    public Func<IReadOnlyList<string>>? ActiveMods { get; set; }

    /// <summary>Gets or sets what runs after a round was applied, such as reading the lists again.</summary>
    public Func<CancellationToken, Task>? AfterChangeAsync { get; set; }

    /// <summary>Gets the round being previewed.</summary>
    public PlanPreview Preview { get; } = new();

    [ObservableProperty]
    public partial string? ProfileName { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsIdle), nameof(IsTesting), nameof(IsFinished), nameof(Found), nameof(Headline), nameof(Body), nameof(PutBackWithoutLabel))]
    public partial ProblemSearch? Search { get; private set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
    public partial bool CanApply { get; set; }

    public bool IsIdle => Search is null;

    public bool IsTesting => Search is { End: ProblemSearchEnd.None };

    public bool IsFinished => Search is { End: not ProblemSearchEnd.None };

    public bool Found => Search is { End: ProblemSearchEnd.Found };

    public string Headline => Search switch
    {
        null => "Find the problem mod",
        { End: ProblemSearchEnd.Found, Culprit: { } mod } => $"It looks like {Preview.NameOf(mod)}",
        { End: ProblemSearchEnd.NotFound } => "It doesn't look like one of your mods",
        { Suspects.Count: 1 } => $"Round {Search.Round}: one last check",
        _ => $"Round {Search.Round}: {Counted.Of(Search.Suspects.Count, "mod")} could be the one",
    };

    public string Body => Search switch
    {
        null => "Something going wrong in your game? This turns off half your mods at a time, and you play to see if the problem still happens, until one is left. Use a save you don't mind losing. Everything goes back at the end.",
        { End: ProblemSearchEnd.Found } => "The problem went away with it off. Two mods that only clash together can look like this too, so if it comes back, look at what it shares with others.",
        { End: ProblemSearchEnd.NotFound } => "The problem happened with each of them off. It could be two mods together, or the game itself.",
        _ => $"Play the game, then say what happened.{(Search.RoundsLeft() > 0 ? $" About {Counted.Of(Search.RoundsLeft(), "more round")} after this." : string.Empty)}",
    };

    public string? PutBackWithoutLabel => Search?.Culprit is { } mod ? $"Put everything back without {Preview.NameOf(mod)}" : null;

    public Task LoadAsync(string? profileName, CancellationToken cancellationToken = default)
    {
        ProfileName = profileName;
        ClearPlan();
        ReportSuccess(string.Empty);
        return GuardAsync(ReadAsync, cancellationToken);
    }

    [RelayCommand]
    private Task StartAsync(CancellationToken cancellationToken) => GuardAsync(
        async token =>
        {
            var on = ActiveMods?.Invoke() ?? [];
            if (on.Count == 0)
            {
                ReportStatus("No mods are on, so none of them can be the problem.");
                return;
            }

            await PlanRoundAsync(ProblemSearch.Start(on), token);
        },
        cancellationToken);

    [RelayCommand]
    private Task StillHappensAsync(CancellationToken cancellationToken) => AnswerAsync(true, cancellationToken);

    [RelayCommand]
    private Task ItsGoneAsync(CancellationToken cancellationToken) => AnswerAsync(false, cancellationToken);

    [RelayCommand]
    private Task PutBackAsync(CancellationToken cancellationToken) => GuardAsync(
        token => Search is { } search ? PlanAsync("Put everything back", search.Original, null, "Everything is back as it was.", token) : Task.CompletedTask,
        cancellationToken);

    [RelayCommand]
    private Task PutBackWithoutAsync(CancellationToken cancellationToken) => GuardAsync(
        token =>
        {
            if (Search is not { Culprit: { } mod } search)
            {
                return Task.CompletedTask;
            }

            var name = Preview.NameOf(mod);
            var ids = search.Original.Where(id => !string.Equals(id, mod, StringComparison.OrdinalIgnoreCase)).ToList();
            return PlanAsync($"Put everything back without {name}", ids, null, $"Everything is back, without {name}. It stays in your Library, off.", token);
        },
        cancellationToken);

    [RelayCommand(CanExecute = nameof(CanApply))]
    private Task ApplyAsync(CancellationToken cancellationToken) => GuardAsync(
        async token =>
        {
            if (_pending is not { } pending)
            {
                return;
            }

            var title = Preview.Title ?? "Making the change";
            ClearPlan();
            var result = await _apply.ApplyAsync(title, new ApplyModPlanRequest(pending.Documents, pending.Plan), token);
            if (AfterChangeAsync is { } refresh)
            {
                await refresh(token);
            }

            if (result.TryGetValue(out _, out var error))
            {
                await MoveOnAsync(pending.Documents, pending.Next, pending.Done, token);
            }
            else
            {
                ReportError(error with { Message = $"That round didn't finish: {error.Message}" });
            }
        },
        cancellationToken);

    [RelayCommand]
    private void CancelPlan()
    {
        ClearPlan();
        ReportSuccess(string.Empty);
    }

    /// <summary>Stops without changing anything, for when going back can't be planned (ADR-0042).</summary>
    [RelayCommand]
    private Task ForgetAsync(CancellationToken cancellationToken) => GuardAsync(
        async token =>
        {
            if ((await ProfileDocuments.ForAsync(_getProfile, ProfileName, token)).TryGetValue(out var documents, out var error))
            {
                ClearPlan();
                await MoveOnAsync(documents, null, "Forgot the search. Your mods stay as they are now.", token);
            }
            else
            {
                ReportError(error);
            }
        },
        cancellationToken);

    private Task AnswerAsync(bool stillHappens, CancellationToken cancellationToken) => GuardAsync(
        async token =>
        {
            if (Search is not { } search)
            {
                return;
            }

            var next = search.Answer(stillHappens);
            if (next.End == ProblemSearchEnd.None)
            {
                await PlanRoundAsync(next, token);
            }
            else if ((await ProfileDocuments.ForAsync(_getProfile, ProfileName, token)).TryGetValue(out var documents, out var error))
            {
                await MoveOnAsync(documents, next, string.Empty, token);
            }
            else
            {
                ReportError(error);
            }
        },
        cancellationToken);

    private Task PlanRoundAsync(ProblemSearch next, CancellationToken cancellationToken) =>
        PlanAsync($"Round {next.Round}: turn off {Counted.Of(next.Off.Count, "mod")}", next.Keeping(), next, string.Empty, cancellationToken);

    private async Task PlanAsync(string title, IReadOnlyList<string> ids, ProblemSearch? next, string done, CancellationToken cancellationToken)
    {
        ClearPlan();
        ReportSuccess(string.Empty);
        if (!(await ProfileDocuments.FoldersAsync(_getProfile, ProfileName, cancellationToken)).TryGetValue(out var folders, out var error)
            || !(await _plan.ExecuteAsync(new PlanModChangeRequest(folders.Documents, new ModChange(ModChangeKind.Set, ids), folders.Install), cancellationToken)).TryGetValue(out var plan, out error))
        {
            ReportError(error);
            return;
        }

        if (plan.IsEmpty && !plan.IsBlocked)
        {
            await MoveOnAsync(folders.Documents, next, done, cancellationToken);
            return;
        }

        Preview.Show(title, plan);
        _pending = (folders.Documents, plan, next, done);
        CanApply = !plan.IsBlocked;
    }

    /// <summary>Keeps the search as it now stands, or ends it when <paramref name="next"/> is null.</summary>
    private async Task MoveOnAsync(string documents, ProblemSearch? next, string done, CancellationToken cancellationToken)
    {
        if (next is null)
        {
            _store.Delete(documents);
        }
        else
        {
            await _store.SaveAsync(documents, next, cancellationToken);
        }

        Search = next;
        ReportSuccess(done);
    }

    private async Task ReadAsync(CancellationToken cancellationToken)
    {
        Search = null;
        if ((await ProfileDocuments.ForAsync(_getProfile, ProfileName, cancellationToken)).TryGetValue(out var documents, out _))
        {
            if ((await _store.LoadAsync(documents, cancellationToken)).TryGetValue(out var search, out var error))
            {
                Search = search;
            }
            else
            {
                ReportError(error);
            }
        }
    }

    private void ClearPlan()
    {
        _pending = null;
        Preview.Clear();
        CanApply = false;
    }
}
