using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DAORework.Application.Mods.Library;
using DAORework.Application.Profiles;
using Microsoft.Extensions.Logging;

namespace DAORework.App.ViewModels;

/// <summary>
/// <i>Keep one of each</i> (ADR-0039): one preview for every mod held more than once. Each set starts on a pick the
/// player can change, every swap goes in one plan, and only once that has been applied are the others removed for good.
/// </summary>
public sealed partial class KeepEachViewModel : PageViewModel
{
    private readonly GetProfile _getProfile;
    private readonly PlanKeepEach _plan;
    private readonly CopyRemoval _removal;
    private string? _profileName;
    private (KeepEachPreview Preview, string? Documents)? _pending;
    private int _asked;

    public KeepEachViewModel(GetProfile getProfile, PlanKeepEach plan, CopyRemoval removal, ILogger<KeepEachViewModel> logger)
        : base(logger)
    {
        _getProfile = getProfile;
        _plan = plan;
        _removal = removal;
    }

    /// <summary>Gets or sets what runs afterwards, such as reading the Library again.</summary>
    public Func<CancellationToken, Task>? AfterChangeAsync { get; set; }

    public PlanPreview Preview { get; } = new();

    /// <summary>Gets each set of copies, those that aren't the same first.</summary>
    public ObservableCollection<KeepEachRow> Rows { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RemoveCopiesCommand))]
    public partial bool CanRemove { get; set; }

    /// <summary>Gets what the main button says.</summary>
    [ObservableProperty]
    public partial string? RemoveLabel { get; set; }

    /// <summary>Points at the profile a copy that is on can be swapped in. A preview made for another one no longer applies.</summary>
    public void Load(string? profileName)
    {
        _profileName = profileName;
        Clear();
        ReportSuccess(string.Empty);
    }

    [RelayCommand]
    private Task PlanAsync(CancellationToken cancellationToken) => GuardAsync(
        token =>
        {
            Clear();
            ReportSuccess(string.Empty);
            return ShowPlanAsync(token);
        },
        cancellationToken);

    /// <summary>Plans again for the picks as they are now. A pick made meanwhile asks again, and the newest answer is the one shown.</summary>
    [RelayCommand(AllowConcurrentExecutions = true)]
    private Task ReplanAsync(CancellationToken cancellationToken) => GuardAsync(ShowPlanAsync, cancellationToken);

    [RelayCommand(CanExecute = nameof(CanRemove))]
    private Task RemoveCopiesAsync(CancellationToken cancellationToken) => GuardAsync(
        async token =>
        {
            if (_pending is not { } pending)
            {
                return;
            }

            var plan = pending.Preview.Plan;
            Clear();
            var ids = plan.Removing.Select(e => e.Mod.Id).ToList();
            var error = await _removal.RunAsync(KeepEachWords.Title, pending.Preview.Swap, pending.Documents, ids, token);
            if (AfterChangeAsync is { } refresh)
            {
                await refresh(token);
            }

            if (error is not null)
            {
                ReportError(error);
            }
            else
            {
                ReportSuccess(KeepEachWords.Done(plan));
            }
        },
        cancellationToken);

    [RelayCommand]
    private void Cancel() => Clear();

    private async Task ShowPlanAsync(CancellationToken cancellationToken)
    {
        var asked = ++_asked;
        CanRemove = false;
        var folders = await ProfileDocuments.FoldersAsync(_getProfile, _profileName, cancellationToken);
        var picks = Rows.ToDictionary(r => r.Group, r => r.Pick?.ModId, StringComparer.OrdinalIgnoreCase);
        var request = new PlanKeepEachRequest(picks, folders.IsSuccess ? folders.Value.Documents : null, folders.IsSuccess ? folders.Value.Install : null);
        var planned = await _plan.ExecuteAsync(request, cancellationToken);
        if (asked != _asked)
        {
            return;
        }

        if (planned.TryGetValue(out var preview, out var error))
        {
            Show(preview, request.DocumentsDirectory);
        }
        else
        {
            ReportError(error);
        }
    }

    private void Show(KeepEachPreview preview, string? documents)
    {
        var plan = preview.Plan;
        if (plan.Sets.Count == 0)
        {
            ReportSuccess("You have no copies to clear.");
            return;
        }

        var summary = KeepEachWords.Summary(plan);
        Preview.Clear();
        if (preview.Swap is { } swap)
        {
            Preview.Show(KeepEachWords.Title, swap, summary);
        }
        else
        {
            Preview.Title = KeepEachWords.Title;
            Preview.Summary = summary;
        }

        foreach (var (line, place) in plan.Removing.Select((e, i) => (KeepCopyWords.Line(e), i)))
        {
            Preview.Lines.Insert(place, line);
        }

        foreach (var problem in plan.Problems)
        {
            Preview.Problems.Add(problem);
        }

        foreach (var (note, place) in KeepEachWords.Notes(plan).Select((n, i) => (n, i)))
        {
            Preview.Notes.Insert(place, note);
        }

        ShowSets(preview);
        _pending = (preview, documents);
        RemoveLabel = KeepEachWords.Button(plan);
        CanRemove = plan.Clearing.Count > 0 && plan.Problems.Count == 0 && preview.Swap is not { IsBlocked: true };
    }

    private void ShowSets(KeepEachPreview preview)
    {
        if (Rows.Count == 0)
        {
            foreach (var set in preview.Plan.Sets)
            {
                Rows.Add(new KeepEachRow(set, () => ReplanCommand.Execute(null)));
            }

            return;
        }

        var sets = preview.Plan.Sets.ToDictionary(s => s.Group, StringComparer.OrdinalIgnoreCase);
        foreach (var row in Rows)
        {
            if (sets.TryGetValue(row.Group, out var set))
            {
                row.Update(set);
            }
        }
    }

    private void Clear()
    {
        _asked++;
        _pending = null;
        Preview.Clear();
        Rows.Clear();
        CanRemove = false;
    }
}
