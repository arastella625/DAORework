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

    /// <summary>Gets the plan being previewed, as <c>mods activate --dry-run</c> prints it.</summary>
    public ObservableCollection<string> PlanLines { get; } = [];

    public IReadOnlyList<string> ActiveModIds { get; private set; } = [];

    [ObservableProperty]
    public partial string? ProfileName { get; set; }

    [ObservableProperty]
    public partial string? PlanTitle { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
    public partial bool CanApply { get; set; }

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
    private Task ActivateAsync(LibraryModRow mod, CancellationToken cancellationToken) => PlanAsync(ModChangeKind.Activate, mod, cancellationToken);

    [RelayCommand]
    private Task DeactivateAsync(LibraryModRow mod, CancellationToken cancellationToken) => PlanAsync(ModChangeKind.Deactivate, mod, cancellationToken);

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

                var already = report.StepsAlreadyDone > 0 ? $", {report.StepsAlreadyDone} already done" : string.Empty;
                ReportSuccess($"Done: {report.StepsRun} step(s) applied{already}. Backup {report.BackupId} holds what was there before.");
            },
            cancellationToken);

    [RelayCommand]
    private void CancelPlan()
    {
        ClearPlan();
        ReportSuccess("Nothing was changed.");
    }

    private Task PlanAsync(ModChangeKind kind, LibraryModRow mod, CancellationToken cancellationToken) =>
        GuardAsync(
            async token =>
            {
                ClearPlan();
                ReportSuccess(string.Empty);
                if (!(await DocumentsAsync(token)).TryGetValue(out var documents, out var error)
                    || !(await _plan.ExecuteAsync(new PlanModChangeRequest(documents, new ModChange(kind, [mod.Id])), token)).TryGetValue(out var plan, out error))
                {
                    ReportError(error);
                    return;
                }

                PlanTitle = $"{(kind == ModChangeKind.Activate ? "Activate" : "Deactivate")} {mod.Title}";
                foreach (var line in ModActivationPlanText.Render(plan))
                {
                    PlanLines.Add(line);
                }

                _pending = (documents, plan);
                CanApply = !plan.IsBlocked && !plan.IsEmpty;
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
        PlanLines.Clear();
        PlanTitle = null;
        CanApply = false;
    }
}
