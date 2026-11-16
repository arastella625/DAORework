using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DAORework.Application.Mods.Programs;
using DAORework.Application.Profiles;
using DAORework.Core.Catalog;
using DAORework.Core.Fixes;
using DAORework.Core.Mods.Planning;
using DAORework.Core.Mods.Safety;
using Microsoft.Extensions.Logging;

namespace DAORework.App.ViewModels;

/// <summary>
/// Turning a mod that runs inside the game on or off from the Mods tab (ADR-0033 §3, P2.8-05): its own preview, since it
/// goes into the game folder on the fixes' record, never in a change with other mods. Turning one on needs the tick,
/// which is never ticked for the player and is asked every time; turning one off doesn't. Undo is on the History tab.
/// </summary>
public sealed partial class GameProgramChangeViewModel : PageViewModel
{
    private readonly GetProfile _getProfile;
    private readonly GameProgramSteps _steps;
    private readonly GetGameProgramStatus _status;
    private (string Install, PlannedGameProgramMod Planned)? _pending;

    public GameProgramChangeViewModel(GetProfile getProfile, GameProgramSteps steps, GetGameProgramStatus status, ILogger<GameProgramChangeViewModel> logger)
        : base(logger)
    {
        _getProfile = getProfile;
        _steps = steps;
        _status = status;
    }

    /// <summary>Raised when <see cref="ActiveModIds"/> has been read again.</summary>
    public event EventHandler? ActiveModsChanged;

    /// <summary>Gets or sets what runs after a change was applied, such as reading the Library again.</summary>
    public Func<CancellationToken, Task>? AfterChangeAsync { get; set; }

    /// <summary>Gets the mods of this kind that are on in the profile's game folder.</summary>
    public IReadOnlyList<string> ActiveModIds { get; private set; } = [];

    /// <summary>Gets what the plan notes, such as 64-bit DLLs or a file left in place.</summary>
    public ObservableCollection<string> Notes { get; } = [];

    /// <summary>Gets what the mod needs that isn't in the game (ADR-0033 §5), shown with the other warning.</summary>
    public ObservableCollection<string> Needs { get; } = [];

    /// <summary>Gets every file the plan touches, shown on <i>Show details</i>.</summary>
    public ObservableCollection<string> Lines { get; } = [];

    [ObservableProperty]
    public partial string? ProfileName { get; set; }

    [ObservableProperty]
    public partial string? Title { get; set; }

    [ObservableProperty]
    public partial string? Summary { get; set; }

    [ObservableProperty]
    public partial bool ShowDetails { get; set; }

    /// <summary>Gets a value indicating whether the plan puts a program in the game folder, so it asks first.</summary>
    [ObservableProperty]
    public partial bool NeedsTrust { get; set; }

    /// <summary>Gets or sets whether the player ticked <see cref="GameProgramTrust.Question"/>, which is never ticked for them.</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
    public partial bool IsTrusted { get; set; }

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

    /// <summary>Reads what is on again, after a change made elsewhere such as an undo.</summary>
    public Task RefreshActiveAsync(CancellationToken cancellationToken = default) => GuardAsync(ReadActiveAsync, cancellationToken);

    /// <summary>Shows the preview of turning <paramref name="mod"/> on or off. Nothing changes until <i>Apply</i>.</summary>
    public Task PlanAsync(ModChangeKind kind, LibraryModRow mod, CancellationToken cancellationToken = default) =>
        GuardAsync(
            async token =>
            {
                ArgumentNullException.ThrowIfNull(mod);
                ClearPlan();
                ReportSuccess(string.Empty);
                var change = kind == ModChangeKind.Activate ? FixChangeKind.Apply : FixChangeKind.Remove;
                if (!(await ProfileInstall.ForAsync(_getProfile, ProfileName, token)).TryGetValue(out var install, out var error)
                    || !(await _steps.Plan.ExecuteAsync(new PlanGameProgramModRequest(install, mod.Id, change), token)).TryGetValue(out var planned, out error))
                {
                    ReportError(error);
                    return;
                }

                Show(planned, mod.Title);
                if (change == FixChangeKind.Apply)
                {
                    await ShowNeedsAsync(mod, token);
                }

                _pending = (install, planned);
                CanApply = !planned.Plan.Change.IsBlocked;
            },
            cancellationToken);

    private bool CanApplyNow() => CanApply && (!NeedsTrust || IsTrusted);

    [RelayCommand(CanExecute = nameof(CanApplyNow))]
    private Task ApplyAsync(CancellationToken cancellationToken) =>
        GuardAsync(
            async token =>
            {
                if (_pending is not { } pending)
                {
                    return;
                }

                var trusted = IsTrusted;
                ClearPlan();
                var result = await _steps.Apply.ExecuteAsync(new ApplyGameProgramModRequest(pending.Install, pending.Planned, trusted), token);
                await ReadActiveAsync(token);
                if (AfterChangeAsync is not null)
                {
                    await AfterChangeAsync(token);
                }

                if (!result.TryGetValue(out var outcome, out var error))
                {
                    ReportError(error with { Message = $"The change didn't finish: {error.Message}" });
                    return;
                }

                ReportSuccess(outcome.Report is null ? "Nothing needed changing in your game folder." : "Done. You can undo it in History.");
            },
            cancellationToken);

    [RelayCommand]
    private void CancelPlan()
    {
        if (Title is null)
        {
            return;
        }

        ClearPlan();
        ReportSuccess("Nothing was changed.");
    }

    private void Show(PlannedGameProgramMod planned, string title)
    {
        var plan = planned.Plan;
        var turningOn = plan.Kind == FixChangeKind.Apply;
        Title = $"{(turningOn ? "Turn on" : "Turn off")} {title}";
        Summary = GameProgramPlanWords.Summary(plan);
        NeedsTrust = turningOn && plan.Change.Operations.Count > 0;
        foreach (var note in GameProgramPlanWords.Notes(planned))
        {
            Notes.Add(note);
        }

        foreach (var line in FixPlanText.Render(plan, planned.ModName))
        {
            Lines.Add(line);
        }
    }

    /// <summary>Only a warning, never a stop: a player who sorted the need out some other way can still go ahead.</summary>
    private async Task ShowNeedsAsync(LibraryModRow mod, CancellationToken cancellationToken)
    {
        if (!(await _steps.Needs.ExecuteAsync(ProfileName!, mod.Id, cancellationToken)).TryGetValue(out var needs, out var error))
        {
            Needs.Add(NeedsCheck.NotChecked(mod.Title, error.Message));
            return;
        }

        foreach (var need in needs)
        {
            Needs.Add(need);
        }
    }

    /// <summary>A profile without an install folder has none of these on; the first <i>Turn on</i> explains.</summary>
    private async Task ReadActiveAsync(CancellationToken cancellationToken)
    {
        ActiveModIds = [];
        if ((await ProfileInstall.ForAsync(_getProfile, ProfileName, cancellationToken)).TryGetValue(out var install, out _))
        {
            if ((await _status.ExecuteAsync(install, cancellationToken)).TryGetValue(out var active, out var error))
            {
                ActiveModIds = active;
            }
            else
            {
                ReportError(error);
            }
        }

        ActiveModsChanged?.Invoke(this, EventArgs.Empty);
    }

    private void ClearPlan()
    {
        _pending = null;
        Title = null;
        Summary = null;
        ShowDetails = false;
        NeedsTrust = false;
        IsTrusted = false;
        CanApply = false;
        Notes.Clear();
        Needs.Clear();
        Lines.Clear();
    }
}
