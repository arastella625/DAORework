using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DAORework.Application.Mods.Adoption;
using DAORework.Application.Mods.Apply;
using DAORework.Application.Profiles;
using DAORework.Core.Common;
using DAORework.Core.Mods.Adoption;
using DAORework.Core.Mods.Apply;
using DAORework.Core.Mods.Planning;
using Microsoft.Extensions.Logging;

namespace DAORework.App.ViewModels;

/// <summary>
/// Adopting a mod installed by hand from the window (P2.5-03, P3-04, ADR-0015), as <c>mods adopt</c> does: the plan is
/// shown first, a blocked or empty plan cannot be applied, and nothing in the documents folder moves until <i>Apply</i>.
/// Planning copies the mod into the library (only app-data is written). Loose override files need a name, since they
/// have none of their own.
/// </summary>
public sealed partial class ModAdoptionViewModel : PageViewModel
{
    private readonly GetProfile _getProfile;
    private readonly PlanAdoption _plan;
    private readonly ChangeInProgress _apply;
    private (string Documents, ModActivationPlan Plan, string Name)? _pending;
    private ILookup<string, HandInstalledModRow> _planned = Array.Empty<HandInstalledModRow>().ToLookup(_ => string.Empty);
    private IReadOnlyList<string> _order = [];
    private List<HandInstalledModRow> _asked = [];
    private IReadOnlyList<LeftOutMod> _leftOut = [];

    public ModAdoptionViewModel(GetProfile getProfile, PlanAdoption plan, ChangeInProgress apply, ILogger<ModAdoptionViewModel> logger)
        : base(logger)
    {
        _getProfile = getProfile;
        _plan = plan;
        _apply = apply;
    }

    /// <summary>Gets or sets what runs after an adoption was applied, such as reading the library and the lists again.</summary>
    public Func<CancellationToken, Task>? AfterChangeAsync { get; set; }

    /// <summary>Gets or sets what runs after a plan was made: planning copies the mod into the library, as <c>mods adopt --dry-run</c> does.</summary>
    public Func<CancellationToken, Task>? AfterPlanAsync { get; set; }

    /// <summary>Gets the plan being previewed: a summary, its warnings, and its full text behind Show details (P2.5-18).</summary>
    public PlanPreview Preview { get; } = new();

    /// <summary>Gets the plan's full text, as <c>mods adopt --dry-run</c> prints it, with mods named.</summary>
    public ObservableCollection<string> PlanLines => Preview.Lines;

    /// <summary>Gets each set of mods with different files of the same name (P2.6-21), since only the player can say which should win.</summary>
    public ObservableCollection<WhoWinsRow> WhoWins { get; } = [];

    /// <summary>Gets what the plan being previewed is for, or null when there is none.</summary>
    public string? PlanTitle => Preview.Title;

    [ObservableProperty]
    public partial string? ProfileName { get; set; }

    /// <summary>Gets or sets the name the loose override files are adopted under.</summary>
    [ObservableProperty]
    public partial string? LooseFilesName { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
    public partial bool CanApply { get; set; }

    /// <summary>Gets a value indicating whether the plan is blocked only by mods it could go ahead without (P2.6-53).</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LeaveOutCommand))]
    public partial bool CanLeaveOut { get; set; }

    /// <summary>Gets what the button that takes those mods out of the plan says.</summary>
    [ObservableProperty]
    public partial string? LeaveOutLabel { get; set; }

    public void Load(string? profileName)
    {
        ProfileName = profileName;
        ClearPlan();
        ReportSuccess(string.Empty);
    }

    /// <summary>
    /// Plans adopting <paramref name="mods"/> as one change (P2.5-13), in the order given (a later one wins a file name
    /// two of them share), as <c>mods adopt &lt;id&gt;...</c> does: one plan, one backup, one rollback.
    /// </summary>
    public Task PlanAdoptionAsync(IReadOnlyList<HandInstalledModRow> mods, CancellationToken cancellationToken = default) =>
        GuardAsync(token => PlanNowAsync(mods, [], token), cancellationToken);

    [RelayCommand]
    private Task AdoptAsync(HandInstalledModRow mod, CancellationToken cancellationToken) => PlanAdoptionAsync([mod], cancellationToken);

    /// <summary>Plans again with the winner in front of the mods it beat, so nothing is decided until <i>Apply</i>.</summary>
    [RelayCommand]
    private Task SwapAsync(WhoWinsRow row, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(row);
        var leftovers = _planned.Where(g => !_order.Contains(g.Key, StringComparer.OrdinalIgnoreCase)).SelectMany(g => g);
        var leftOut = _leftOut;
        return GuardAsync(token => PlanNowAsync([.. AdoptionOrder.Swap(_order, row.WinnerModId, row.OtherModIds).SelectMany(id => _planned[id]), .. leftovers], leftOut, token), cancellationToken);
    }

    /// <summary>Plans the same change again without the mods that stop it, which stay in the game as they are.</summary>
    [RelayCommand(CanExecute = nameof(CanLeaveOut))]
    private Task LeaveOutAsync(CancellationToken cancellationToken)
    {
        if (_pending is not { } pending)
        {
            return Task.CompletedTask;
        }

        var ids = LeftOut.Ids(pending.Plan, _order);
        var rows = ids.SelectMany(id => _planned[id]).ToList();
        var reasons = ids.Select(id => new LeftOutMod(Preview.NameOf(id), LeftOut.Why(pending.Plan, id, Preview.NameOf)));
        var asked = _asked;
        var leftOut = _leftOut;
        return GuardAsync(token => PlanNowAsync([.. asked.Except(rows)], [.. leftOut, .. reasons], token), cancellationToken);
    }

    [RelayCommand(CanExecute = nameof(CanApply))]
    private Task ApplyAsync(CancellationToken cancellationToken) =>
        GuardAsync(
            async token =>
            {
                if (_pending is not { } pending)
                {
                    return;
                }

                var title = Preview.Title ?? $"Manage {pending.Name}";
                ClearPlan();
                var result = await _apply.ApplyAsync(title, new ApplyModPlanRequest(pending.Documents, pending.Plan), token);
                if (AfterChangeAsync is not null)
                {
                    await AfterChangeAsync(token);
                }

                if (!result.TryGetValue(out var report, out var error))
                {
                    if (!await GoOnWithoutAsync(error, pending.Documents, token))
                    {
                        ReportError(error with { Message = $"Couldn't finish managing {pending.Name}: {error.Message}" });
                    }

                    return;
                }

                if (_leftOut.Count > 0)
                {
                    ReportStatus(LeftOut.Outcome($"Now managing {pending.Name}.", _leftOut));
                }
                else
                {
                    ReportSuccess($"Now managing {pending.Name}. You can undo this in History.");
                }
            },
            cancellationToken);

    /// <summary>
    /// One mod's file stopped a batch partway (P2.6-53): the change is put back as if it never started, and the rest
    /// comes back as a preview without that mod, so nothing happens that the player hasn't seen.
    /// </summary>
    /// <returns>Whether there is a new preview. If not, the change stays unfinished for History to settle.</returns>
    private async Task<bool> GoOnWithoutAsync(Error error, string documents, CancellationToken cancellationToken)
    {
        var failed = error.ModId is { } id ? _planned[id].ToList() : [];
        if (failed.Count == 0 || failed.Count == _asked.Count
            || (await _apply.RecoverAsync(new RecoverModPlanRequest(documents, RecoveryChoice.RollBack), cancellationToken)).IsFailure)
        {
            return false;
        }

        if (AfterChangeAsync is not null)
        {
            await AfterChangeAsync(cancellationToken);
        }

        await PlanNowAsync([.. _asked.Except(failed)], [.. _leftOut, new LeftOutMod(failed[0].Title, error.Cause ?? error.Message)], cancellationToken);
        return true;
    }

    /// <summary>One mod alone fails as it says; in a batch, what can't be adopted is left out and the rest goes on (P2.6-53).</summary>
    private async Task PlanNowAsync(IReadOnlyList<HandInstalledModRow> mods, IReadOnlyList<LeftOutMod> leftOut, CancellationToken cancellationToken)
    {
        ClearPlan();
        ReportSuccess(string.Empty);
        var candidates = mods.Where(m => m.Candidate is not null).ToList();
        if (candidates.Count == 0)
        {
            return;
        }

        var request = new PlanAdoptionRequest(string.Empty, [.. candidates.Select(m => m.Candidate!)], LooseFilesName) { SkipFailures = candidates.Count > 1 };
        if (!(await ProfileDocuments.ForAsync(_getProfile, ProfileName, cancellationToken)).TryGetValue(out var documents, out var error)
            || !(await _plan.ExecuteAsync(request with { DocumentsDirectory = documents }, cancellationToken)).TryGetValue(out var plan, out error))
        {
            ReportError(error);
            return;
        }

        var skipped = plan.SkippedCandidates.Select(s => s.CandidateId).ToHashSet(StringComparer.Ordinal);
        var kept = candidates.Where(m => !skipped.Contains(m.Candidate!.Id)).ToList();
        _leftOut = [.. leftOut, .. plan.SkippedCandidates.Select(s => new LeftOutMod(s.Name, s.Why))];
        if (AfterPlanAsync is not null)
        {
            await AfterPlanAsync(cancellationToken);
        }

        if (kept.Count == 0)
        {
            ReportFailure(LeftOut.Outcome("None of these could be managed.", _leftOut));
            return;
        }

        // Shown once the library has been read again, so the mods planning just imported are named.
        var titles = kept.Select(m => m.Title).ToList();
        _pending = (documents, plan, PlanWords.Names(titles, title => title));
        CanApply = !plan.IsBlocked && !plan.IsEmpty;
        Preview.Show(PlanPreview.TitleFor("Manage", titles), plan);
        LeftOut.Show(Preview, _leftOut, "in your game as it is");
        _asked = kept;
        ShowWhoWins(plan, kept);
        var leaving = LeftOut.Ids(plan, _order);
        LeaveOutLabel = LeftOut.Label([.. leaving.Select(Preview.NameOf)]);
        CanLeaveOut = leaving.Count > 0;
    }

    [RelayCommand]
    private void CancelPlan()
    {
        ClearPlan();
        ReportSuccess("Nothing in the documents folder was changed. The copy planning made stays in the Library.");
    }

    /// <summary>Each row with the mod it turned out to be. Only mods new to the game have an order to change: a leftover goes wherever its mod already is.</summary>
    private void ShowWhoWins(ModActivationPlan plan, List<HandInstalledModRow> candidates)
    {
        _order = [.. plan.AdoptedModIds.Distinct(StringComparer.OrdinalIgnoreCase).Except(plan.CurrentModIds, StringComparer.OrdinalIgnoreCase)];
        _planned = plan.AdoptedModIds.Zip(candidates).ToLookup(p => p.First, p => p.Second, StringComparer.OrdinalIgnoreCase);
        foreach (var group in SharedFileNotes.Groups([.. plan.Conflicts.BetweenMods.Where(c => !c.SameContent)], Preview.NameOf))
        {
            WhoWins.Add(new WhoWinsRow(group.Title, group.Kinds, group.WinnerModId, group.OtherModIds, AdoptionOrder.CanSwap(_order, group.WinnerModId, group.OtherModIds)));
        }
    }

    private void ClearPlan()
    {
        _pending = null;
        Preview.Clear();
        WhoWins.Clear();
        CanApply = false;
        CanLeaveOut = false;
    }
}
