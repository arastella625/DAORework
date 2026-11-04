using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DAORework.Application.Mods.Handover;
using DAORework.Application.Profiles;
using DAORework.Core.Common;
using DAORework.Core.Mods.Handover;
using Microsoft.Extensions.Logging;

namespace DAORework.App.ViewModels;

/// <summary>
/// The <i>Move from Vortex</i> card on the Mods page (ADR-0028 §3): where the handover stands, worked out from the game
/// folder each time, and its two steps of DAORework's own. Copying writes only the library, and turning the copies on goes
/// through the usual preview.
/// </summary>
public sealed partial class VortexHandoverViewModel : PageViewModel
{
    private readonly GetProfile _getProfile;
    private readonly ReadVortexHandover _read;
    private readonly CopyVortexMods _copy;
    private VortexHandoverState? _state;

    public VortexHandoverViewModel(GetProfile getProfile, ReadVortexHandover read, CopyVortexMods copy, ILogger<VortexHandoverViewModel> logger)
        : base(logger)
    {
        _getProfile = getProfile;
        _read = read;
        _copy = copy;
    }

    /// <summary>Gets or sets what runs after copying, such as reading the Library again.</summary>
    public Func<CancellationToken, Task>? AfterCopyAsync { get; set; }

    /// <summary>Gets or sets how the copies are turned on: the usual preview, in the order given.</summary>
    public Func<IReadOnlyList<string>, IReadOnlyList<DAORework.Core.Mods.Planning.AddInsListChange>, CancellationToken, Task>? TurnOnAsync { get; set; }

    public ObservableCollection<string> Mods { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsShown), nameof(ShowsCopy), nameof(ShowsTurnOn), nameof(StepText))]
    public partial VortexHandoverStep Step { get; set; }

    [ObservableProperty]
    public partial string? ProfileName { get; set; }

    public bool IsShown => Step is VortexHandoverStep.Copy or VortexHandoverStep.Purge or VortexHandoverStep.TurnOn;

    public bool ShowsCopy => Step == VortexHandoverStep.Copy;

    public bool ShowsTurnOn => Step == VortexHandoverStep.TurnOn;

    public string StepText => Step switch
    {
        VortexHandoverStep.Copy => "Step 1 of 3: Copy. DAORework adds its own copy of each mod. Nothing in your game changes.",
        VortexHandoverStep.Purge => "Step 2 of 3: Purge in Vortex. Open Vortex, choose Dragon Age: Origins, press Purge on the Mods page, then stop managing the game. Then Check again.",
        VortexHandoverStep.TurnOn => "Step 3 of 3: Turn them on. Vortex's files are gone, and your copies are ready.",
        _ => string.Empty,
    };

    public Task LoadAsync(string? profileName, CancellationToken cancellationToken = default)
    {
        ProfileName = profileName;
        return RefreshAsync(cancellationToken);
    }

    /// <summary>Reads where the handover stands. A profile without a documents folder simply has no card.</summary>
    public Task RefreshAsync(CancellationToken cancellationToken = default) => GuardAsync(ReadAsync, cancellationToken);

    [RelayCommand]
    private Task CheckAgainAsync(CancellationToken cancellationToken) => RefreshAsync(cancellationToken);

    [RelayCommand]
    private Task CopyAsync(CancellationToken cancellationToken) => GuardAsync(
        async token =>
        {
            if (!(await DocumentsAsync(token)).TryGetValue(out var documents, out var error)
                || !(await _copy.ExecuteAsync(documents, token)).TryGetValue(out var report, out error))
            {
                ReportError(error);
                return;
            }

            if (AfterCopyAsync is { } afterCopy)
            {
                await afterCopy(token);
            }

            await ReadAsync(token);
            Report(report);
        },
        cancellationToken);

    [RelayCommand]
    private Task TurnOnAllAsync(CancellationToken cancellationToken) =>
        _state is null ? Task.CompletedTask : TurnOnAsync?.Invoke([.. _state.Copies.Select(c => c.ModId)], _state.LostDlc, cancellationToken) ?? Task.CompletedTask;

    private Task<Result<string>> DocumentsAsync(CancellationToken cancellationToken) => ProfileDocuments.ForAsync(_getProfile, ProfileName, cancellationToken);

    private async Task ReadAsync(CancellationToken cancellationToken)
    {
        if (!(await DocumentsAsync(cancellationToken)).TryGetValue(out var documents, out _))
        {
            Show(new VortexHandoverState(VortexHandoverStep.NothingToDo, [], []));
        }
        else if ((await _read.ExecuteAsync(documents, cancellationToken)).TryGetValue(out var state, out var error))
        {
            Show(state);
        }
        else
        {
            ReportError(error);
        }
    }

    private void Show(VortexHandoverState state)
    {
        Step = state.Step;
        _state = state;
        Mods.Clear();
        foreach (var mod in state.Managed)
        {
            Mods.Add($"{mod.Name} · {Counted.Of(mod.FilesInGame, "file")}");
        }
    }

    private void Report(VortexCopyReport report)
    {
        var parts = new List<string> { $"Copied {Counted.Of(report.Copies.Count, "mod")}." };
        parts.AddRange(report.Failed.Select(f => $"{f.Name} wasn't copied: {f.Error.Message}"));
        parts.AddRange(report.RunsInTheGame.Select(n => $"{n} runs inside the game, so it stays out of this."));
        if (report.Unsettled.Count > 0)
        {
            parts.Add($"Vortex's record doesn't settle the order of {string.Join(", ", report.Unsettled)}. Check them in Arrange afterwards.");
        }

        ReportStatus(string.Join(' ', parts));
    }
}
