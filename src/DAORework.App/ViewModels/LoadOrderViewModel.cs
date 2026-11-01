using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DAORework.Application.Mods.Apply;
using DAORework.Application.Mods.Changes;
using DAORework.Application.Mods.Library;
using DAORework.Application.Profiles;
using DAORework.Core.Mods.Planning;
using Microsoft.Extensions.Logging;

namespace DAORework.App.ViewModels;

/// <summary>
/// The load order and its conflicts (ROADMAP P2.5-05): the active mods, lowest priority first, and per override file
/// which mods ship it and whose copy wins. Moving a mod plans the new order at once, so the conflicts and the plan
/// change before anything is applied; <i>Apply</i> applies it like any mod change, backed up and journaled.
/// </summary>
public sealed partial class LoadOrderViewModel : PageViewModel
{
    private readonly GetProfile _getProfile;
    private readonly PlanModChange _plan;
    private readonly ApplyModPlan _apply;
    private readonly ListLibrary _listLibrary;
    private IReadOnlyList<string> _saved = [];
    private (string Documents, ModActivationPlan Plan)? _pending;

    public LoadOrderViewModel(GetProfile getProfile, PlanModChange plan, ApplyModPlan apply, ListLibrary listLibrary, ILogger<LoadOrderViewModel> logger)
        : base(logger)
    {
        _getProfile = getProfile;
        _plan = plan;
        _apply = apply;
        _listLibrary = listLibrary;
    }

    /// <summary>Gets or sets what runs after a new order was applied, such as reading the other panels again.</summary>
    public Func<CancellationToken, Task>? AfterChangeAsync { get; set; }

    /// <summary>Gets the active mods, lowest priority first, in the order being edited.</summary>
    public ObservableCollection<LoadOrderRow> Order { get; } = [];

    /// <summary>Gets the override files more than one mod in <see cref="Order"/> ships.</summary>
    public ObservableCollection<FileConflictRow> Conflicts { get; } = [];

    /// <summary>Gets how many files are combined from several mods' copies (ADR-0025), such as <c>chargenmorphcfg.xml</c>.</summary>
    [ObservableProperty]
    public partial int CombinedCount { get; set; }

    /// <summary>Gets the plan for the edited order, empty while it is the saved one.</summary>
    /// <summary>Gets the plan for the new order: a summary, its warnings, and its full text behind Show details (P2.5-18).</summary>
    public PlanPreview Preview { get; } = new();

    /// <summary>Gets the plan's full text, with mods named.</summary>
    public ObservableCollection<string> PlanLines => Preview.Lines;

    [ObservableProperty]
    public partial string? ProfileName { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
    public partial bool CanApply { get; set; }

    [ObservableProperty]
    public partial bool IsReordered { get; set; }

    public Task LoadAsync(string? profileName, CancellationToken cancellationToken = default)
    {
        ProfileName = profileName;
        ReportSuccess(string.Empty);
        return RefreshAsync(cancellationToken);
    }

    /// <summary>Reads the saved order and its conflicts again, dropping any unapplied reordering.</summary>
    public Task RefreshAsync(CancellationToken cancellationToken = default) => GuardAsync(token => PlanOrderAsync(null, token), cancellationToken);

    [RelayCommand]
    private Task MoveUpAsync(LoadOrderRow mod, CancellationToken cancellationToken) => MoveAsync(mod, -1, cancellationToken);

    [RelayCommand]
    private Task MoveDownAsync(LoadOrderRow mod, CancellationToken cancellationToken) => MoveAsync(mod, 1, cancellationToken);

    [RelayCommand(CanExecute = nameof(CanApply))]
    private Task ApplyAsync(CancellationToken cancellationToken) =>
        GuardAsync(
            async token =>
            {
                if (_pending is not { } pending)
                {
                    return;
                }

                CanApply = false;
                var result = await _apply.ExecuteAsync(new ApplyModPlanRequest(pending.Documents, pending.Plan), token);
                await PlanOrderAsync(null, token);
                if (AfterChangeAsync is not null)
                {
                    await AfterChangeAsync(token);
                }

                if (!result.TryGetValue(out var report, out var error))
                {
                    ReportError(error with { Message = $"The new order was not applied: {error.Message}" });
                    return;
                }

                ReportSuccess("Saved the new order. You can undo it in History.");
            },
            cancellationToken);

    [RelayCommand]
    private Task ResetAsync(CancellationToken cancellationToken) =>
        GuardAsync(
            async token =>
            {
                await PlanOrderAsync(null, token);
                ReportSuccess("Back to the saved order; nothing was changed.");
            },
            cancellationToken);

    private Task MoveAsync(LoadOrderRow mod, int step, CancellationToken cancellationToken) =>
        GuardAsync(
            async token =>
            {
                var order = Order.Select(r => r.Id).ToList();
                var from = order.IndexOf(mod.Id);
                var to = from + step;
                if (from < 0 || to < 0 || to >= order.Count)
                {
                    return;
                }

                (order[from], order[to]) = (order[to], order[from]);
                ReportSuccess(string.Empty);
                await PlanOrderAsync(order, token);
            },
            cancellationToken);

    /// <summary>Plans <paramref name="order"/>, or with null the saved order (a plan that changes nothing), and shows it.</summary>
    private async Task PlanOrderAsync(IReadOnlyList<string>? order, CancellationToken cancellationToken)
    {
        _pending = null;
        CanApply = false;
        Preview.Clear();
        if (!(await ProfileDocuments.ForAsync(_getProfile, ProfileName, cancellationToken)).TryGetValue(out var documents, out var error))
        {
            Show([], null);
            return;
        }

        var change = order is null ? new ModChange(ModChangeKind.Activate, []) : new ModChange(ModChangeKind.Set, order);
        if (!(await _plan.ExecuteAsync(new PlanModChangeRequest(documents, change), cancellationToken)).TryGetValue(out var plan, out error))
        {
            ReportError(error);
            return;
        }

        if (order is null)
        {
            _saved = plan.TargetModIds;
        }

        var names = (await _listLibrary.ExecuteAsync(cancellationToken)).Value.ToDictionary(e => e.Mod.Id, e => e.Mod.Name, StringComparer.OrdinalIgnoreCase);
        Show(plan.TargetModIds.Select((id, i) => new LoadOrderRow(id, names.TryGetValue(id, out var name) ? name : id, i + 1)).ToList(), plan, names);
        IsReordered = !plan.TargetModIds.SequenceEqual(_saved, StringComparer.OrdinalIgnoreCase);
        if (IsReordered)
        {
            ShowPlan(plan, names);
            _pending = (documents, plan);

            // Library mods never change and a mod activated later goes last, so an order that changes no file now never
            // will: there is nothing to apply.
            CanApply = !plan.IsBlocked && !plan.IsEmpty;
        }
    }

    private void ShowPlan(ModActivationPlan plan, Dictionary<string, string> names)
    {
        Preview.NameOf = id => names.TryGetValue(id, out var name) ? name : id;
        Preview.Show(
            "The plan for the new order",
            plan,
            plan.IsEmpty && !plan.IsBlocked ? "No file changes: none of these mods ships a file another one does, so their order makes no difference." : null);
    }

    private void Show(IReadOnlyList<LoadOrderRow> rows, ModActivationPlan? plan, IReadOnlyDictionary<string, string>? names = null)
    {
        Order.Clear();
        foreach (var row in rows)
        {
            Order.Add(row);
        }

        CombinedCount = plan?.Combined.Count ?? 0;
        Conflicts.Clear();
        foreach (var conflict in plan?.Conflicts.OverrideConflicts ?? [])
        {
            Conflicts.Add(FileConflictRow.From(conflict, names ?? new Dictionary<string, string>()));
        }

        IsReordered = false;
    }
}
