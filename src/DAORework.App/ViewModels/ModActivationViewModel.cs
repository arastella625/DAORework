using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DAORework.Application.Mods.Apply;
using DAORework.Application.Mods.Changes;
using DAORework.Application.Profiles;
using DAORework.Core.Common;
using DAORework.Core.Mods.Planning;
using Microsoft.Extensions.Logging;

namespace DAORework.App.ViewModels;

/// <summary>
/// Activating and deactivating library mods from the window (P3-04), as <c>mods activate</c> does: the plan is always
/// shown first, a blocked or empty plan cannot be applied, and nothing changes until <i>Apply</i> is pressed. The
/// change is backed up and journaled like any other.
/// </summary>
public sealed partial class ModActivationViewModel : PageViewModel
{
    private readonly GetProfile _getProfile;
    private readonly PlanModChange _plan;
    private readonly ApplyModPlan _apply;
    private readonly GetModStatus _status;
    private (string Documents, ModActivationPlan Plan)? _pending;
    private PlannedChange? _planned;

    public ModActivationViewModel(GetProfile getProfile, PlanModChange plan, ApplyModPlan apply, GetModStatus status, ILogger<ModActivationViewModel> logger)
        : base(logger)
    {
        _getProfile = getProfile;
        _plan = plan;
        _apply = apply;
        _status = status;
    }

    /// <summary>Raised when <see cref="ActiveModIds"/> has been read again.</summary>
    public event EventHandler? ActiveModsChanged;

    /// <summary>Gets or sets what runs after a change was applied, such as reading the backups again.</summary>
    public Func<CancellationToken, Task>? AfterChangeAsync { get; set; }

    /// <summary>Gets the plan being previewed: a summary, its warnings, and its full text behind Show details (P2.5-18).</summary>
    public PlanPreview Preview { get; } = new();

    /// <summary>Gets the plan's full text, as <c>mods activate --dry-run</c> prints it, with mods named.</summary>
    public ObservableCollection<string> PlanLines => Preview.Lines;

    /// <summary>Gets what the plan being previewed is for, or null when there is none.</summary>
    public string? PlanTitle => Preview.Title;

    public IReadOnlyList<string> ActiveModIds { get; private set; } = [];

    [ObservableProperty]
    public partial string? ProfileName { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
    public partial bool CanApply { get; set; }

    /// <summary>Gets a value indicating whether the plan is blocked only by mods it could go ahead without.</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LeaveOutCommand))]
    public partial bool CanLeaveOut { get; set; }

    /// <summary>Gets what the button that takes those mods out of the plan says.</summary>
    [ObservableProperty]
    public partial string? LeaveOutLabel { get; set; }

    public Task LoadAsync(string? profileName, CancellationToken cancellationToken = default)
    {
        ProfileName = profileName;
        ClearPlan();
        ReportSuccess(string.Empty);
        return GuardAsync(ReadActiveAsync, cancellationToken);
    }

    /// <summary>Reads what is active again, after a change made elsewhere such as a rollback.</summary>
    public Task RefreshActiveAsync(CancellationToken cancellationToken = default) => GuardAsync(ReadActiveAsync, cancellationToken);

    [RelayCommand]
    private Task ActivateAsync(LibraryModRow mod, CancellationToken cancellationToken) => PlanChangeAsync(ModChangeKind.Activate, [mod], cancellationToken);

    [RelayCommand]
    private Task DeactivateAsync(LibraryModRow mod, CancellationToken cancellationToken) => PlanChangeAsync(ModChangeKind.Deactivate, [mod], cancellationToken);

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
                if (!(await _apply.ExecuteAsync(new ApplyModPlanRequest(pending.Documents, pending.Plan), token)).TryGetValue(out var report, out var error))
                {
                    ReportError(error with { Message = $"The change did not complete: {error.Message}" });
                    await ReadActiveAsync(token);
                    return;
                }

                await ReadActiveAsync(token);
                if (AfterChangeAsync is not null)
                {
                    await AfterChangeAsync(token);
                }

                ReportSuccess("Done. You can undo it in History.");
            },
            cancellationToken);

    /// <summary>Plans the same change again without the mods that stop it, which stay in the Library and can be turned on later.</summary>
    [RelayCommand(CanExecute = nameof(CanLeaveOut))]
    private Task LeaveOutAsync(CancellationToken cancellationToken)
    {
        if (_planned is not { } planned || _pending is not { } pending)
        {
            return Task.CompletedTask;
        }

        var leftOut = LeftOut.Of(pending.Plan, planned.Mods);
        return PlanAsync(planned with { Mods = [.. planned.Mods.Except(leftOut)], LeftOut = [.. planned.LeftOut, .. leftOut] }, cancellationToken);
    }

    [RelayCommand]
    private void CancelPlan()
    {
        ClearPlan();
        ReportSuccess("Nothing was changed.");
    }

    /// <summary>
    /// Plans activating or deactivating <paramref name="mods"/> as one change (P2.5-14), as <c>mods activate &lt;id&gt;...</c>
    /// does: mods activated together are added in the order given, after what is already active.
    /// </summary>
    public Task PlanChangeAsync(ModChangeKind kind, IReadOnlyList<LibraryModRow> mods, CancellationToken cancellationToken = default) =>
        PlanChangeAsync(kind, mods, [], cancellationToken);

    /// <summary>Plans the change as above, and puts back official DLC entries too (ADR-0028 §3).</summary>
    public Task PlanChangeAsync(ModChangeKind kind, IReadOnlyList<LibraryModRow> mods, IReadOnlyList<AddInsListChange> restoredAddIns, CancellationToken cancellationToken) =>
        PlanAsync(new PlannedChange(kind, mods, restoredAddIns, []), cancellationToken);

    private Task PlanAsync(PlannedChange change, CancellationToken cancellationToken) =>
        GuardAsync(
            async token =>
            {
                ClearPlan();
                ReportSuccess(string.Empty);
                if (change.Mods.Count == 0)
                {
                    return;
                }

                var request = new ModChange(change.Kind, [.. change.Mods.Select(m => m.Id)]);
                if (!(await ProfileDocuments.FoldersAsync(_getProfile, ProfileName, token)).TryGetValue(out var folders, out var error)
                    || !(await _plan.ExecuteAsync(new PlanModChangeRequest(folders.Documents, request, folders.Install) { RestoredAddIns = change.RestoredAddIns }, token)).TryGetValue(out var plan, out error))
                {
                    ReportError(error);
                    return;
                }

                var verb = change.Kind == ModChangeKind.Activate ? "Turn on" : "Turn off";
                Preview.Show(PlanPreview.TitleFor(verb, [.. change.Mods.Select(m => m.Title)]), plan);
                if (change.LeftOut.Count > 0)
                {
                    Preview.Notes.Insert(0, LeftOut.Note(change.LeftOut));
                }

                _pending = (folders.Documents, plan);
                _planned = change;
                CanApply = !plan.IsBlocked && !plan.IsEmpty;
                var leftOut = change.Kind == ModChangeKind.Activate ? LeftOut.Of(plan, change.Mods) : [];
                LeaveOutLabel = LeftOut.Label(leftOut);
                CanLeaveOut = leftOut.Count > 0;
            },
            cancellationToken);

    /// <summary>
    /// What is active in the profile's documents folder. A profile without one simply has nothing active, which the
    /// first Activate explains; a record that cannot be read is an error.
    /// </summary>
    private async Task ReadActiveAsync(CancellationToken cancellationToken)
    {
        ActiveModIds = [];
        if ((await DocumentsAsync(cancellationToken)).TryGetValue(out var documents, out _))
        {
            if ((await _status.ExecuteAsync(documents, cancellationToken)).TryGetValue(out var status, out var error))
            {
                ActiveModIds = status.Record.ActiveModIds;
            }
            else
            {
                ReportError(error);
            }
        }

        ActiveModsChanged?.Invoke(this, EventArgs.Empty);
    }

    private Task<Result<string>> DocumentsAsync(CancellationToken cancellationToken) => ProfileDocuments.ForAsync(_getProfile, ProfileName, cancellationToken);

    private void ClearPlan()
    {
        _pending = null;
        _planned = null;
        Preview.Clear();
        CanApply = false;
        CanLeaveOut = false;
    }
}
