using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DAORework.Application.Mods.Apply;
using DAORework.Application.Mods.Changes;
using DAORework.Application.Profiles;
using DAORework.Core.Common;
using DAORework.Core.Mods.Apply;
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
    private readonly PlanTurnOn _plan;
    private readonly ChangeInProgress _apply;
    private readonly GetModStatus _status;
    private (string Documents, ModActivationPlan Plan)? _pending;
    private PlannedChange? _planned;

    public ModActivationViewModel(GetProfile getProfile, PlanTurnOn plan, ChangeInProgress apply, GetModStatus status, ILogger<ModActivationViewModel> logger)
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

    /// <summary>Gets or sets where a mod that runs inside the game goes instead: its own preview (ADR-0033 §3).</summary>
    public Func<ModChangeKind, LibraryModRow, CancellationToken, Task>? ProgramChangeAsync { get; set; }

    /// <summary>Gets or sets whether a mod that runs inside the game is on in the game folder.</summary>
    public Func<string, bool> ProgramIsOn { get; set; } = _ => false;

    /// <summary>Gets the change running now, which the whole window shares (P2.6-22).</summary>
    public ChangeInProgress Change => _apply;

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

                var title = Preview.Title ?? "Making the change";
                var planned = _planned;
                ClearPlan();
                if (!(await _apply.ApplyAsync(title, new ApplyModPlanRequest(pending.Documents, pending.Plan), token)).TryGetValue(out var report, out var error))
                {
                    if (!await GoOnWithoutAsync(error, pending.Documents, planned, token))
                    {
                        ReportError(error with { Message = $"The change did not complete: {error.Message}" });
                        await ReadActiveAsync(token);
                    }

                    return;
                }

                await ReadActiveAsync(token);
                if (AfterChangeAsync is not null)
                {
                    await AfterChangeAsync(token);
                }

                if (planned is { LeftOut.Count: > 0 })
                {
                    ReportStatus(LeftOut.Outcome($"Turned on {Counted.Of(planned.Mods.Count, "mod")}.", planned.LeftOut));
                }
                else
                {
                    ReportSuccess("Done. You can undo it in History.");
                }
            },
            cancellationToken);

    /// <summary>
    /// One mod's file stopped a batch partway (P2.6-53): the change is put back as if it never started, and the rest
    /// comes back as a preview without that mod, so nothing happens that the player hasn't seen.
    /// </summary>
    /// <returns>Whether there is a new preview. If not, the change stays unfinished for History to settle.</returns>
    private async Task<bool> GoOnWithoutAsync(Error error, string documents, PlannedChange? planned, CancellationToken cancellationToken)
    {
        if (planned is not { Kind: ModChangeKind.Activate, Mods.Count: > 1 }
            || planned.Mods.FirstOrDefault(m => string.Equals(m.Id, error.ModId, StringComparison.OrdinalIgnoreCase)) is not { } failed
            || (await _apply.RecoverAsync(new RecoverModPlanRequest(documents, RecoveryChoice.RollBack), cancellationToken)).IsFailure)
        {
            return false;
        }

        await ReadActiveAsync(cancellationToken);
        if (AfterChangeAsync is not null)
        {
            await AfterChangeAsync(cancellationToken);
        }

        await PlanNowAsync(planned with { Mods = [.. planned.Mods.Except([failed])], LeftOut = [.. planned.LeftOut, new LeftOutMod(failed.Title, "One of its files couldn't be placed.", error.Cause)] }, cancellationToken);
        return true;
    }

    /// <summary>Plans the same change again without the mods that stop it, which stay in the Library and can be turned on later.</summary>
    [RelayCommand(CanExecute = nameof(CanLeaveOut))]
    private Task LeaveOutAsync(CancellationToken cancellationToken)
    {
        if (_planned is not { } planned || _pending is not { } pending)
        {
            return Task.CompletedTask;
        }

        var leftOut = LeftOut.Of(pending.Plan, planned.Mods);
        var reasons = leftOut.Select(m => new LeftOutMod(m.Title, LeftOut.Why(pending.Plan, m.Id, Preview.NameOf)));
        return GuardAsync(token => PlanNowAsync(planned with { Mods = [.. planned.Mods.Except(leftOut)], LeftOut = [.. planned.LeftOut, .. reasons] }, token), cancellationToken);
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

    /// <summary>
    /// Plans the change as above, and puts back official DLC entries too (ADR-0028 §3). A mod that runs inside the game
    /// gets its own preview when it is the only one picked, and is left out and named otherwise.
    /// </summary>
    public Task PlanChangeAsync(ModChangeKind kind, IReadOnlyList<LibraryModRow> mods, IReadOnlyList<AddInsListChange> restoredAddIns, CancellationToken cancellationToken)
    {
        var (rest, programs) = ProgramsApart.Split(kind, mods, ProgramIsOn);
        if (rest.Count == 0 && programs.Count == 1 && ProgramChangeAsync is { } program)
        {
            ClearPlan();
            ReportSuccess(string.Empty);
            return program(kind, programs[0], cancellationToken);
        }

        return GuardAsync(token => PlanNowAsync(new PlannedChange(kind, rest, restoredAddIns, ProgramsApart.LeftOut(kind, programs)), token), cancellationToken);
    }

    private async Task PlanNowAsync(PlannedChange change, CancellationToken cancellationToken)
    {
        ClearPlan();
        ReportSuccess(string.Empty);
        if (change.Mods.Count == 0)
        {
            if (change.LeftOut.Count > 0)
            {
                ReportStatus(LeftOut.Outcome("Nothing was changed.", change.LeftOut));
            }

            return;
        }

        var request = new ModChange(change.Kind, [.. change.Mods.Select(m => m.Id)]);
        if (!(await ProfileDocuments.FoldersAsync(_getProfile, ProfileName, cancellationToken)).TryGetValue(out var folders, out var error)
            || !(await _plan.ExecuteAsync(new PlanModChangeRequest(folders.Documents, request, folders.Install) { RestoredAddIns = change.RestoredAddIns }, cancellationToken)).TryGetValue(out var plan, out error))
        {
            ReportError(error);
            return;
        }

        var verb = change.Kind == ModChangeKind.Activate ? "Turn on" : "Turn off";
        Preview.Show(PlanPreview.TitleFor(verb, [.. change.Mods.Select(m => m.Title)]), plan);
        LeftOut.Show(Preview, change.LeftOut, "in your Library, off");
        _pending = (folders.Documents, plan);
        _planned = change;
        CanApply = !plan.IsBlocked && !plan.IsEmpty;
        var leftOut = change.Kind == ModChangeKind.Activate ? LeftOut.Of(plan, change.Mods) : [];
        LeaveOutLabel = LeftOut.Label([.. leftOut.Select(m => m.Title)]);
        CanLeaveOut = leftOut.Count > 0;
    }

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
