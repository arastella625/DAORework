using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DAORework.App.Services;
using DAORework.Application.Fixes;
using DAORework.Application.Profiles;
using DAORework.Core.Fixes;
using Microsoft.Extensions.Logging;

namespace DAORework.App.ViewModels;

/// <summary>
/// Applying and removing the 4 GB patch and DXVK from the window (P2.5-06), as <c>fixes apply</c> and <c>fixes
/// remove</c> do (ADR-0013): the plan is shown first, a blocked plan cannot be applied, and nothing changes until
/// <i>Apply</i>; the change is backed up and journaled. DXVK is installed from a release the user downloaded.
/// </summary>
public sealed partial class FixChangeViewModel : PageViewModel
{
    private static readonly string[] _dxvkPatterns = ["*.tar.gz", "*.tgz", "*.zip", "*.dll"];

    private readonly GetProfile _getProfile;
    private readonly PlanFix _plan;
    private readonly ApplyFix _apply;
    private readonly IPathPicker _picker;
    private (string Install, PlannedFix Planned)? _pending;

    public FixChangeViewModel(GetProfile getProfile, PlanFix plan, ApplyFix apply, IPathPicker picker, ILogger<FixChangeViewModel> logger)
        : base(logger)
    {
        _getProfile = getProfile;
        _plan = plan;
        _apply = apply;
        _picker = picker;
    }

    /// <summary>Gets or sets what runs after a change was applied, such as reading the status again.</summary>
    public Func<CancellationToken, Task>? AfterChangeAsync { get; set; }

    /// <summary>Gets the plan being previewed, as <c>fixes apply --dry-run</c> prints it, with its notes.</summary>
    public ObservableCollection<string> PlanLines { get; } = [];

    [ObservableProperty]
    public partial string? ProfileName { get; set; }

    [ObservableProperty]
    public partial string? PlanTitle { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
    public partial bool CanApply { get; set; }

    public void Load(string? profileName)
    {
        ProfileName = profileName;
        ClearPlan();
        ReportSuccess(string.Empty);
    }

    [RelayCommand]
    private Task ApplyLargeAddressAwareAsync(CancellationToken cancellationToken) => PlanAsync(FixIds.LargeAddressAware, FixChangeKind.Apply, null, cancellationToken);

    [RelayCommand]
    private Task RemoveLargeAddressAwareAsync(CancellationToken cancellationToken) => PlanAsync(FixIds.LargeAddressAware, FixChangeKind.Remove, null, cancellationToken);

    [RelayCommand]
    private async Task InstallDxvkAsync(CancellationToken cancellationToken)
    {
        var release = await _picker.PickFileAsync("Choose the DXVK release you downloaded", "DXVK release", _dxvkPatterns, cancellationToken);
        if (release is not null)
        {
            await PlanAsync(FixIds.Dxvk, FixChangeKind.Apply, release, cancellationToken);
        }
    }

    [RelayCommand]
    private Task RemoveDxvkAsync(CancellationToken cancellationToken) => PlanAsync(FixIds.Dxvk, FixChangeKind.Remove, null, cancellationToken);

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
                var result = await _apply.ExecuteAsync(new ApplyFixRequest(pending.Install, pending.Planned), token);
                if (AfterChangeAsync is not null)
                {
                    await AfterChangeAsync(token);
                }

                if (!result.TryGetValue(out var outcome, out var error))
                {
                    ReportError(error with { Message = $"The change didn't finish: {error.Message}" });
                    return;
                }

                ReportSuccess(outcome.Report is not null
                    ? "Done. You can undo it in History."
                    : "Nothing needed changing in your game folder.");
            },
            cancellationToken);

    [RelayCommand]
    private void CancelPlan()
    {
        ClearPlan();
        ReportSuccess("Nothing was changed.");
    }

    private Task PlanAsync(string fix, FixChangeKind kind, string? source, CancellationToken cancellationToken) =>
        GuardAsync(
            async token =>
            {
                ClearPlan();
                ReportSuccess(string.Empty);
                if (!(await ProfileInstall.ForAsync(_getProfile, ProfileName, token)).TryGetValue(out var install, out var error)
                    || !(await _plan.ExecuteAsync(new PlanFixRequest(install, fix, kind, source), token)).TryGetValue(out var planned, out error))
                {
                    ReportError(error);
                    return;
                }

                PlanTitle = $"{(kind == FixChangeKind.Apply ? "Apply" : "Remove")} {planned.FixName}";
                foreach (var line in FixPlanText.Render(planned.Plan, planned.FixName).Concat(planned.Notes.Select(n => $"Note: {n}")))
                {
                    PlanLines.Add(line);
                }

                _pending = (install, planned);

                // A plan that only brings the record up to date changes nothing on disk, so it may go ahead, as in the CLI.
                CanApply = !planned.Plan.Change.IsBlocked;
            },
            cancellationToken);

    private void ClearPlan()
    {
        _pending = null;
        PlanLines.Clear();
        PlanTitle = null;
        CanApply = false;
    }
}
