using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DAORework.Application.Mods.Library;
using DAORework.Application.Profiles;
using DAORework.Core.Mods.Library;
using Microsoft.Extensions.Logging;

namespace DAORework.App.ViewModels;

/// <summary>
/// <i>Keep this one</i> on a mod with copies (ADR-0039): the preview says what goes and what comes back, a copy that is
/// on is swapped through the usual change, and only then are the others removed for good.
/// </summary>
public sealed partial class KeepCopyViewModel : PageViewModel
{
    private readonly GetProfile _getProfile;
    private readonly PlanKeepCopy _plan;
    private readonly FindModsInside _inside;
    private readonly CopyRemoval _removal;
    private string? _profileName;
    private (KeepCopyPreview Preview, string? Documents)? _pending;

    public KeepCopyViewModel(GetProfile getProfile, PlanKeepCopy plan, CopyRemoval removal, FindModsInside inside, ILogger<KeepCopyViewModel> logger)
        : base(logger)
    {
        _getProfile = getProfile;
        _plan = plan;
        _inside = inside;
        _removal = removal;
    }

    /// <summary>Gets or sets what runs afterwards, such as reading the Library again.</summary>
    public Func<CancellationToken, Task>? AfterChangeAsync { get; set; }

    public PlanPreview Preview { get; } = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RemoveOthersCommand))]
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

    /// <summary>What each mod is inside (ADR-0041), for the Library's rows.</summary>
    public IReadOnlyDictionary<string, ModInside> FindInside(IReadOnlyList<LibraryEntry> entries) => _inside.Execute(entries);

    [RelayCommand]
    private Task PlanKeepAsync(LibraryModRow row, CancellationToken cancellationToken) => PlanAsync(row.Id, row.Title, null, cancellationToken);

    /// <summary>Plans keeping the bigger mod this one is inside, and removing this one (ADR-0041).</summary>
    [RelayCommand]
    private Task PlanKeepWholeAsync(LibraryModRow row, CancellationToken cancellationToken) =>
        row.Inside is { } inside ? PlanAsync(inside.WholeId, ModDisplayName.For(inside.WholeName), [row.Id], cancellationToken) : Task.CompletedTask;

    private Task PlanAsync(string keepId, string title, IReadOnlyList<string>? removing, CancellationToken cancellationToken) => GuardAsync(
        async token =>
        {
            Clear();
            ReportSuccess(string.Empty);
            var folders = await ProfileDocuments.FoldersAsync(_getProfile, _profileName, token);
            var request = new PlanKeepCopyRequest(keepId, folders.IsSuccess ? folders.Value.Documents : null, folders.IsSuccess ? folders.Value.Install : null) { Removing = removing };
            if ((await _plan.ExecuteAsync(request, token)).TryGetValue(out var preview, out var error))
            {
                Show(title, preview, request.DocumentsDirectory, removing is not null);
            }
            else
            {
                ReportError(error);
            }
        },
        cancellationToken);

    [RelayCommand(CanExecute = nameof(CanRemove))]
    private Task RemoveOthersAsync(CancellationToken cancellationToken) => GuardAsync(
        async token =>
        {
            if (_pending is not { } pending)
            {
                return;
            }

            var title = Preview.Title ?? "Keeping one copy";
            Clear();
            var ids = pending.Preview.Plan.Removing.Select(e => e.Mod.Id).ToList();
            var failed = await _removal.RunAsync(title, pending.Preview.Swap, pending.Documents, ids, token);
            await RefreshAsync(token);
            if (failed is { } error)
            {
                ReportError(error);
            }
            else
            {
                ReportSuccess($"Kept {ModDisplayName.For(pending.Preview.Plan.Kept.Mod.Name)}. {KeepCopyWords.Space(pending.Preview.Plan.BytesFreed, _removal.GoesToRecycleBin)}");
            }
        },
        cancellationToken);

    [RelayCommand]
    private void Cancel() => Clear();

    private void Show(string title, KeepCopyPreview preview, string? documents, bool named)
    {
        var plan = preview.Plan;
        var summary = $"Removes {KeepCopyWords.Removing(plan)} from your Library. {KeepCopyWords.Space(plan.BytesFreed, _removal.GoesToRecycleBin)}";
        if (preview.Swap is { } swap)
        {
            Preview.Show($"Keep {title}", swap, summary);
            Preview.Notes.Insert(0, $"{ModDisplayName.For(plan.SwappedOut!)} is on, so this one takes its place.");
        }
        else
        {
            Preview.Title = $"Keep {title}";
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

        Preview.Notes.Insert(0, KeepCopyWords.Warning(_removal.GoesToRecycleBin));
        if (KeepCopyWords.Lost(plan.OnlyInRemoved) is { } lost)
        {
            Preview.Notes.Insert(0, lost);
        }

        _pending = (preview, documents);
        RemoveLabel = named ? $"Remove {KeepCopyWords.Removing(plan)}" : plan.Removing.Count == 1 ? "Remove the other copy" : "Remove the other copies";
        CanRemove = plan.Problems.Count == 0 && preview.Swap is not { IsBlocked: true };
    }

    private async Task RefreshAsync(CancellationToken cancellationToken)
    {
        if (AfterChangeAsync is { } refresh)
        {
            await refresh(cancellationToken);
        }
    }

    private void Clear()
    {
        _pending = null;
        Preview.Clear();
        CanRemove = false;
    }
}
