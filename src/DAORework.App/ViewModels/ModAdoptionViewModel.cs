using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DAORework.Application.Mods.Adoption;
using DAORework.Application.Mods.Apply;
using DAORework.Application.Profiles;
using DAORework.Core.Mods.Adoption;
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
    private readonly ApplyModPlan _apply;
    private (string Documents, ModActivationPlan Plan, string Name)? _pending;
    private Dictionary<string, HandInstalledModRow> _planned = new(StringComparer.OrdinalIgnoreCase);
    private IReadOnlyList<string> _order = [];

    public ModAdoptionViewModel(GetProfile getProfile, PlanAdoption plan, ApplyModPlan apply, ILogger<ModAdoptionViewModel> logger)
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
        GuardAsync(
            async token =>
            {
                ClearPlan();
                ReportSuccess(string.Empty);
                var candidates = mods.Where(m => m.Candidate is not null).ToList();
                if (candidates.Count == 0)
                {
                    return;
                }

                if (!(await ProfileDocuments.ForAsync(_getProfile, ProfileName, token)).TryGetValue(out var documents, out var error)
                    || !(await _plan.ExecuteAsync(new PlanAdoptionRequest(documents, [.. candidates.Select(m => m.Candidate!)], LooseFilesName), token)).TryGetValue(out var plan, out error))
                {
                    ReportError(error);
                    return;
                }

                var titles = candidates.Select(m => m.Title).ToList();
                _pending = (documents, plan, PlanWords.Names(titles, title => title));
                CanApply = !plan.IsBlocked && !plan.IsEmpty;
                if (AfterPlanAsync is not null)
                {
                    await AfterPlanAsync(token);
                }

                // Shown once the library has been read again, so the mods planning just imported are named.
                Preview.Show(PlanPreview.TitleFor("Manage", titles), plan);
                ShowWhoWins(plan, candidates);
            },
            cancellationToken);

    [RelayCommand]
    private Task AdoptAsync(HandInstalledModRow mod, CancellationToken cancellationToken) => PlanAdoptionAsync([mod], cancellationToken);

    /// <summary>Plans again with the winner in front of the mods it beat, so nothing is decided until <i>Apply</i>.</summary>
    [RelayCommand]
    private Task SwapAsync(WhoWinsRow row, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(row);
        return PlanAdoptionAsync([.. AdoptionOrder.Swap(_order, row.WinnerModId, row.OtherModIds).Select(id => _planned[id])], cancellationToken);
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

                ClearPlan();
                var result = await _apply.ExecuteAsync(new ApplyModPlanRequest(pending.Documents, pending.Plan), token);
                if (AfterChangeAsync is not null)
                {
                    await AfterChangeAsync(token);
                }

                if (!result.TryGetValue(out var report, out var error))
                {
                    ReportError(error with { Message = $"Couldn't finish managing {pending.Name}: {error.Message}" });
                    return;
                }

                ReportSuccess($"Now managing {pending.Name}. You can undo this in History.");
            },
            cancellationToken);

    [RelayCommand]
    private void CancelPlan()
    {
        ClearPlan();
        ReportSuccess("Nothing in the documents folder was changed. The copy planning made stays in the Library.");
    }

    /// <summary>The adopted mods are the plan's last, in the order given, whatever their library ids turned out to be.</summary>
    private void ShowWhoWins(ModActivationPlan plan, List<HandInstalledModRow> candidates)
    {
        _order = [.. plan.TargetModIds.TakeLast(candidates.Count)];
        _planned = _order.Zip(candidates).DistinctBy(p => p.First, StringComparer.OrdinalIgnoreCase).ToDictionary(p => p.First, p => p.Second, StringComparer.OrdinalIgnoreCase);
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
    }
}
