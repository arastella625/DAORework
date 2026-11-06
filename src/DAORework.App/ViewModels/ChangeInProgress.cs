using CommunityToolkit.Mvvm.ComponentModel;
using DAORework.Application.Mods.Apply;
using DAORework.Application.Mods.Changes;
using DAORework.Core.Common;
using DAORework.Core.Mods.Apply;
using DAORework.Core.Mods.Backups;

namespace DAORework.App.ViewModels;

/// <summary>
/// The change running now, shared by the whole window (P2.6-22). A big change takes minutes, so the window says how far
/// it has got and lets nothing else start until it ends: a second change would only find the first one's journal.
/// </summary>
public sealed partial class ChangeInProgress : ObservableObject
{
    private const string StayOpen = "Leave DAORework open until this finishes.";

    private readonly ApplyModPlan _apply;
    private readonly RollBackModChange _rollBack;
    private readonly RecoverModPlan _recover;

    public ChangeInProgress(ApplyModPlan apply, RollBackModChange rollBack, RecoverModPlan recover)
    {
        _apply = apply;
        _rollBack = rollBack;
        _recover = recover;
    }

    [ObservableProperty]
    public partial bool IsRunning { get; private set; }

    [ObservableProperty]
    public partial string? Title { get; private set; }

    [ObservableProperty]
    public partial string? Detail { get; private set; }

    /// <summary>Gets how far it has got, out of 100.</summary>
    [ObservableProperty]
    public partial double Percent { get; private set; }

    /// <summary>Gets a value indicating whether the steps have started, so how far it has got is known. Until then, the backup is being taken.</summary>
    [ObservableProperty]
    public partial bool HasSteps { get; private set; }

    /// <summary>Applies a plan, showing <paramref name="title"/> and how far it has got.</summary>
    public Task<Result<ApplyReport>> ApplyAsync(string title, ApplyModPlanRequest request, CancellationToken cancellationToken = default) =>
        RunAsync(title, progress => _apply.ExecuteAsync(request with { Progress = progress }, cancellationToken));

    /// <summary>Undoes a change. Its steps aren't counted, so the window only says it is working.</summary>
    public Task<Result<RestoreReport>> UndoAsync(RollBackModChangeRequest request, CancellationToken cancellationToken = default) =>
        RunAsync("Undoing this change", _ => _rollBack.ExecuteAsync(request, cancellationToken));

    /// <summary>Settles a change that did not finish. Its steps aren't counted either.</summary>
    public Task<Result<ApplyReport>> RecoverAsync(RecoverModPlanRequest request, CancellationToken cancellationToken = default) =>
        RunAsync("Sorting out the last change", _ => _recover.ExecuteAsync(request, cancellationToken));

    /// <summary>Runs <paramref name="work"/> as the change in progress, however it ends.</summary>
    public async Task<T> RunAsync<T>(string title, Func<IProgress<ChangeSteps>, Task<T>> work)
    {
        ArgumentNullException.ThrowIfNull(work);
        Title = title;
        Detail = StayOpen;
        Percent = 0;
        HasSteps = false;
        IsRunning = true;
        try
        {
            return await work(new Progress<ChangeSteps>(Show));
        }
        finally
        {
            IsRunning = false;
        }
    }

    // A report posted just before the end can arrive after it.
    private void Show(ChangeSteps steps)
    {
        if (IsRunning)
        {
            HasSteps = true;
            Percent = 100.0 * steps.Done / steps.Total;
            Detail = $"{steps.Done} of {steps.Total} done. {StayOpen}";
        }
    }
}
