using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DAORework.Application.Mods.Apply;
using DAORework.Application.Mods.Changes;
using DAORework.Application.Profiles;
using DAORework.Core.Mods.Combining;
using DAORework.Core.Mods.Library;
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
    private const int MaxPackedRows = 50;

    private const int MaxSharedRows = 30;

    private readonly GetProfile _getProfile;
    private readonly PlanModChange _plan;
    private readonly ChangeInProgress _apply;
    private readonly ReadLoadOrderDetails _details;
    private IReadOnlyList<string> _saved = [];
    private (string Documents, ModActivationPlan Plan)? _pending;

    public LoadOrderViewModel(GetProfile getProfile, PlanModChange plan, ChangeInProgress apply, ReadLoadOrderDetails details, ILogger<LoadOrderViewModel> logger)
        : base(logger)
    {
        _getProfile = getProfile;
        _plan = plan;
        _apply = apply;
        _details = details;
    }

    /// <summary>Gets or sets what runs after a new order was applied, such as reading the other panels again.</summary>
    public Func<CancellationToken, Task>? AfterChangeAsync { get; set; }

    /// <summary>Gets the active mods, lowest priority first, in the order being edited.</summary>
    public ObservableCollection<LoadOrderRow> Order { get; } = [];

    /// <summary>Gets what more than one active mod holds inside an archive (P3-07). The game's choice there isn't known, so no mod is said to win.</summary>
    public ObservableCollection<FileConflictRow> PackedConflicts { get; } = [];

    /// <summary>Gets the override files more than one mod in <see cref="Order"/> ships.</summary>
    public ObservableCollection<FileConflictRow> Conflicts { get; } = [];

    /// <summary>Gets what <i>Combined files</i> lists: each shared file, or a row for each set of mods once there are too many to read.</summary>
    public ObservableCollection<FileConflictRow> SharedRows { get; } = [];

    /// <summary>Gets what each mod's row says about its shared files in the saved order (P2.6-33).</summary>
    [ObservableProperty]
    public partial IReadOnlyDictionary<string, string> ConflictMarks { get; private set; } = new Dictionary<string, string>();

    /// <summary>Gets how many shared files differ between mods, set once the list is filled so the cards count it once.</summary>
    [ObservableProperty]
    public partial int OverlapCount { get; set; }

    /// <summary>Gets how many files are combined from several mods' copies (ADR-0025), such as <c>chargenmorphcfg.xml</c>.</summary>
    [ObservableProperty]
    public partial int CombinedCount { get; set; }

    /// <summary>Gets the files combined from several mods' copies, each with the mods it came from.</summary>
    public ObservableCollection<FileConflictRow> CombinedFiles { get; } = [];

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

    /// <summary>Moves a mod to <paramref name="place"/>, counted from 0, as dropping it does (ADR-0038). One move, one preview.</summary>
    public Task MoveToPlaceAsync(LoadOrderRow mod, int place, CancellationToken cancellationToken = default) => MoveToAsync(mod, place, cancellationToken);

    /// <summary>Reads the saved order and its conflicts again, dropping any unapplied reordering.</summary>
    public Task RefreshAsync(CancellationToken cancellationToken = default) => GuardAsync(token => PlanOrderAsync(null, token), cancellationToken);

    [RelayCommand]
    private Task MoveUpAsync(LoadOrderRow mod, CancellationToken cancellationToken) => MoveToAsync(mod, mod.Position - 2, cancellationToken);

    [RelayCommand]
    private Task MoveDownAsync(LoadOrderRow mod, CancellationToken cancellationToken) => MoveToAsync(mod, mod.Position, cancellationToken);

    /// <summary>Moves a mod to the top in one step and one preview (P2.6-19), where the others' copies of its files win.</summary>
    [RelayCommand]
    private Task MoveToTopAsync(LoadOrderRow mod, CancellationToken cancellationToken) => MoveToAsync(mod, 0, cancellationToken);

    /// <summary>Moves a mod to the bottom in one step and one preview (P2.6-19), where its copies of files win.</summary>
    [RelayCommand]
    private Task MoveToBottomAsync(LoadOrderRow mod, CancellationToken cancellationToken) => MoveToAsync(mod, Order.Count - 1, cancellationToken);

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
                var result = await _apply.ApplyAsync("Saving the new order", new ApplyModPlanRequest(pending.Documents, pending.Plan), token);
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

    private Task MoveToAsync(LoadOrderRow mod, int to, CancellationToken cancellationToken) =>
        GuardAsync(
            async token =>
            {
                var order = Order.Select(r => r.Id).ToList();
                var from = order.IndexOf(mod.Id);
                if (from < 0 || to < 0 || to >= order.Count || to == from)
                {
                    return;
                }

                order.RemoveAt(from);
                order.Insert(to, mod.Id);
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
        if (!(await ProfileDocuments.FoldersAsync(_getProfile, ProfileName, cancellationToken)).TryGetValue(out var folders, out var error))
        {
            Show([], null);
            return;
        }

        var documents = folders.Documents;
        var change = order is null ? new ModChange(ModChangeKind.Activate, []) : new ModChange(ModChangeKind.Set, order);
        if (!(await _plan.ExecuteAsync(new PlanModChangeRequest(documents, change, folders.Install), cancellationToken)).TryGetValue(out var plan, out error))
        {
            ReportError(error);
            return;
        }

        if (order is null)
        {
            _saved = plan.TargetModIds;
            ConflictMarks = Core.Mods.Conflicts.ConflictMarks.For(plan.Conflicts.BetweenMods);
        }

        var details = await _details.ExecuteAsync(plan.TargetModIds, cancellationToken);
        var names = details.Names.ToDictionary(n => n.Key, n => ModDisplayName.For(n.Value), StringComparer.OrdinalIgnoreCase);
        Show(plan.TargetModIds.Select((id, i) => new LoadOrderRow(id, names.TryGetValue(id, out var name) ? name : id, i + 1)).ToList(), plan, names);
        ShowPacked(details, names);
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

    private void Show(IReadOnlyList<LoadOrderRow> rows, ModActivationPlan? plan, Dictionary<string, string>? names = null)
    {
        Order.Clear();
        foreach (var row in rows)
        {
            Order.Add(row);
        }

        ShowCombined(plan?.Combined ?? [], id => names is not null && names.TryGetValue(id, out var name) ? name : id);
        Conflicts.Clear();
        foreach (var conflict in plan?.Conflicts.BetweenMods ?? [])
        {
            Conflicts.Add(FileConflictRow.From(conflict, names ?? new Dictionary<string, string>()));
        }

        ShowShared(plan, names);
        IsReordered = false;
    }

    private void ShowShared(ModActivationPlan? plan, Dictionary<string, string>? names)
    {
        var groups = Conflicts.Count <= MaxSharedRows
            ? []
            : SharedFileNotes.Groups(plan?.Conflicts.BetweenMods ?? [], ModActivationPlanWarnings.CombinedAware(id => names is not null && names.TryGetValue(id, out var name) ? name : id));
        SharedRows.Clear();
        foreach (var row in groups.Count > 0 ? groups.Select(FileConflictRow.From) : Conflicts)
        {
            SharedRows.Add(row);
        }

        OverlapCount = Conflicts.Count(c => !c.SameContent);
    }

    private void ShowPacked(LoadOrderDetails details, Dictionary<string, string> names)
    {
        PackedConflicts.Clear();
        foreach (var conflict in details.Packed.Take(MaxPackedRows))
        {
            var mods = string.Join(", ", conflict.ModIds.Select(id => names.TryGetValue(id, out var name) ? name : id));
            PackedConflicts.Add(new FileConflictRow(conflict.FileName, "Inside an archive", $"In {mods}. The game uses one copy, and DAORework can't tell which."));
        }

        if (details.Packed.Count > MaxPackedRows)
        {
            PackedConflicts.Add(new FileConflictRow($"{details.Packed.Count - MaxPackedRows} more", "Inside an archive", "More files these mods share inside their archives."));
        }
    }

    private void ShowCombined(IReadOnlyList<CombinedOverride> combined, Func<string, string> nameOf)
    {
        CombinedFiles.Clear();
        foreach (var file in combined)
        {
            var from = string.Join(", ", file.FromModIds.Select(nameOf));
            CombinedFiles.Add(new FileConflictRow(file.FileName, "Combined", $"Merged from {from}, so every mod's part works."));
        }

        CombinedCount = CombinedFiles.Count;
    }
}
