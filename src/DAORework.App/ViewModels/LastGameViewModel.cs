using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DAORework.App.Views;
using DAORework.Application.Mods.Apply;
using DAORework.Application.Mods.Changes;
using DAORework.Application.Profiles;
using DAORework.Core.Mods.Planning;
using DAORework.Core.Mods.Troubleshooting;
using Microsoft.Extensions.Logging;

namespace DAORework.App.ViewModels;

/// <summary>
/// What changed since the last game with other mods (ADR-0043). DAORework can't see whether that game worked, so it says
/// <i>if</i>. Going back is an ordinary preview and <i>Apply</i>.
/// </summary>
public sealed partial class LastGameViewModel : PageViewModel
{
    private readonly GetProfile _getProfile;
    private readonly ReadPlayChanges _read;
    private readonly PlanTurnOn _plan;
    private readonly ChangeInProgress _apply;
    private (string Documents, ModActivationPlan Plan)? _pending;
    private PlayChanges? _changes;

    public LastGameViewModel(GetProfile getProfile, ReadPlayChanges read, PlanTurnOn plan, ChangeInProgress apply, ILogger<LastGameViewModel> logger)
        : base(logger)
    {
        _getProfile = getProfile;
        _read = read;
        _plan = plan;
        _apply = apply;
    }

    /// <summary>Gets or sets what runs after going back, such as reading the lists again.</summary>
    public Func<CancellationToken, Task>? AfterChangeAsync { get; set; }

    /// <summary>Gets or sets whether a mod is still in the Library, since one removed since then can't come back.</summary>
    public Func<string, bool> InLibrary { get; set; } = _ => true;

    public PlanPreview Preview { get; } = new();

    /// <summary>Gets what is different, each line a sentence.</summary>
    public ObservableCollection<string> Lines { get; } = [];

    [ObservableProperty]
    public partial string? ProfileName { get; set; }

    /// <summary>Gets the sentence that opens the card, or null when no game had other mods.</summary>
    [ObservableProperty]
    public partial string? Headline { get; private set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
    public partial bool CanApply { get; set; }

    public Task LoadAsync(string? profileName, CancellationToken cancellationToken = default)
    {
        ProfileName = profileName;
        return RefreshAsync(cancellationToken);
    }

    /// <summary>Reads again, after a game or a change.</summary>
    public Task RefreshAsync(CancellationToken cancellationToken = default) => GuardAsync(ReadAsync, cancellationToken);

    [RelayCommand]
    private Task GoBackAsync(CancellationToken cancellationToken) => GuardAsync(
        async token =>
        {
            if (_changes is not { } changes)
            {
                return;
            }

            ClearPlan();
            ReportSuccess(string.Empty);
            var ids = changes.Then.ModIds.Where(InLibrary).ToList();
            if (!(await ProfileDocuments.FoldersAsync(_getProfile, ProfileName, token)).TryGetValue(out var folders, out var error)
                || !(await _plan.ExecuteAsync(new PlanModChangeRequest(folders.Documents, new ModChange(ModChangeKind.Set, ids), folders.Install), token)).TryGetValue(out var plan, out error))
            {
                ReportError(error);
                return;
            }

            Preview.Show("Go back to those mods", plan);
            var gone = changes.Then.ModIds.Count - ids.Count;
            if (gone > 0)
            {
                Preview.Notes.Insert(0, $"{Counted.Of(gone, "mod")} from then {(gone == 1 ? "isn't" : "aren't")} in your Library any more, so {(gone == 1 ? "it stays" : "they stay")} out.");
            }

            _pending = (folders.Documents, plan);
            CanApply = !plan.IsBlocked && !plan.IsEmpty;
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

            ClearPlan();
            var result = await _apply.ApplyAsync("Going back to those mods", new ApplyModPlanRequest(pending.Documents, pending.Plan), token);
            if (AfterChangeAsync is { } refresh)
            {
                await refresh(token);
            }

            if (result.TryGetValue(out _, out var error))
            {
                ReportSuccess("Back to the mods from then. You can undo it in History.");
            }
            else
            {
                ReportError(error with { Message = $"That didn't finish: {error.Message}" });
            }
        },
        cancellationToken);

    [RelayCommand]
    private void CancelPlan()
    {
        ClearPlan();
        ReportSuccess(string.Empty);
    }

    private async Task ReadAsync(CancellationToken cancellationToken)
    {
        Show(null, default);
        if ((await ProfileDocuments.ForAsync(_getProfile, ProfileName, cancellationToken)).TryGetValue(out var documents, out _))
        {
            if ((await _read.ExecuteAsync(documents, cancellationToken)).TryGetValue(out var read, out var error))
            {
                Show(read.Changes, read.Now);
            }
            else
            {
                ReportError(error);
            }
        }
    }

    private void Show(PlayChanges? changes, DateTimeOffset now)
    {
        _changes = changes;
        Lines.Clear();
        Headline = changes is null ? null : $"If your game last worked {When(changes.Then.PlayedAt, now)}, this is what's new since then";
        if (changes is null)
        {
            return;
        }

        if (changes.Added.Count > 0)
        {
            Lines.Add($"Added {changes.Added.Count}: {PlanWords.Names(changes.Added, Preview.NameOf)}.");
        }

        if (changes.Removed.Count > 0)
        {
            Lines.Add($"Turned off or removed {changes.Removed.Count}: {PlanWords.Names(changes.Removed, Preview.NameOf)}.");
        }

        if (changes.OrderChanged)
        {
            Lines.Add("The order of your mods changed.");
        }
    }

    /// <summary>Mid-sentence, so only a day's name keeps its capital.</summary>
    private static string When(DateTimeOffset played, DateTimeOffset now)
    {
        var when = FriendlyTime.Of(played.ToLocalTime(), now);
        return when.StartsWith("Today", StringComparison.Ordinal) || when.StartsWith("Yesterday", StringComparison.Ordinal) || when.StartsWith("Just", StringComparison.Ordinal)
            ? char.ToLowerInvariant(when[0]) + when[1..]
            : when;
    }

    private void ClearPlan()
    {
        _pending = null;
        Preview.Clear();
        CanApply = false;
    }
}
