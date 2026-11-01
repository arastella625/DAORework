using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DAORework.Application.Mods.Adoption;
using DAORework.Application.Mods.Apply;
using DAORework.Application.Profiles;
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

                var names = string.Join(", ", candidates.Select(m => m.Title));
                _pending = (documents, plan, names);
                CanApply = !plan.IsBlocked && !plan.IsEmpty;
                if (AfterPlanAsync is not null)
                {
                    await AfterPlanAsync(token);
                }

                // Shown once the library has been read again, so the mods planning just imported are named.
                Preview.Show(candidates.Count == 1 ? $"Manage {names}" : $"Manage {candidates.Count} mods: {names}", plan);
            },
            cancellationToken);

    [RelayCommand]
    private Task AdoptAsync(HandInstalledModRow mod, CancellationToken cancellationToken) => PlanAdoptionAsync([mod], cancellationToken);

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

                ReportSuccess($"Now managing {pending.Name}. It is in the Library, and you can undo this in History.");
            },
            cancellationToken);

    [RelayCommand]
    private void CancelPlan()
    {
        ClearPlan();
        ReportSuccess("Nothing in the documents folder was changed. The copy planning made stays in the Library.");
    }

    private void ClearPlan()
    {
        _pending = null;
        Preview.Clear();
        CanApply = false;
    }
}
