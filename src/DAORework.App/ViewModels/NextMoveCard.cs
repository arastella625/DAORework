using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DAORework.Core.Mods;

namespace DAORework.App.ViewModels;

/// <summary>
/// The one next step on Home and the Mods page (P2.6-27), worked out from the Mods tab's own state as it changes, so it
/// reads nothing itself. Its button only opens the right place or a preview: nothing changes without <i>Apply</i>.
/// </summary>
public sealed partial class NextMoveCard : ObservableObject
{
    private readonly ModsTabViewModel _mods;
    private FixStatusViewModel? _fixes;
    public NextMoveCard(ModsTabViewModel mods)
    {
        ArgumentNullException.ThrowIfNull(mods);
        _mods = mods;
        NotifyCollectionChangedEventHandler changed = (_, _) => Update();
        PropertyChangedEventHandler stateChanged = (_, _) => Update();
        mods.Library.Mods.CollectionChanged += changed;
        mods.HandInstalled.Candidates.CollectionChanged += changed;
        mods.HandInstalled.PropertyChanged += stateChanged;
        mods.Changes.PropertyChanged += stateChanged;
        mods.Handover.PropertyChanged += stateChanged;
        mods.Activation.ActiveModsChanged += (_, _) => Update();
        Update();
    }

    /// <summary>Gets or sets how a place is opened, set by the window.</summary>
    public Func<HomeTarget, CancellationToken, Task>? OpenAsync { get; set; }

    [ObservableProperty]
    public partial NextMoveKind Kind { get; private set; }

    [ObservableProperty]
    public partial string Title { get; private set; } = string.Empty;

    [ObservableProperty]
    public partial string Body { get; private set; } = string.Empty;

    [ObservableProperty]
    public partial string? Button { get; private set; }

    /// <summary>Gets the way to a first game with mods, shown until a mod is on (P2.6-36).</summary>
    public ObservableCollection<FirstStep> FirstSteps { get; } = [];

    /// <summary>Gets a value indicating whether there is something to do; Home and the header say the rest.</summary>
    public bool IsShown => Kind != NextMoveKind.Play;

    public bool HasButton => Button is not null;

    /// <summary>Follows the Fixes tab too, whose unfinished change also stops everything.</summary>
    public void Watch(FixStatusViewModel fixes)
    {
        ArgumentNullException.ThrowIfNull(fixes);
        _fixes = fixes;
        fixes.PropertyChanged += (_, _) => Update();
        Update();
    }

    [RelayCommand]
    private async Task TakeAsync(CancellationToken cancellationToken)
    {
        switch (Kind)
        {
            case NextMoveKind.SetUpTheGame:
                await OpenTargetAsync(HomeTarget.GameFolders, cancellationToken);
                break;
            case NextMoveKind.SettleTheLastChange:
                await OpenTargetAsync(_mods.Changes.ChangeUnfinished ? HomeTarget.ModsHistory : HomeTarget.FixesHistory, cancellationToken);
                break;
            case NextMoveKind.TakeOverWhatIsInTheGame:
                await OpenTargetAsync(HomeTarget.ModsLibrary, cancellationToken);
                _mods.Filter.ShowOnlyCommand.Execute(ModShowFilter.Found);
                if (string.IsNullOrWhiteSpace(_mods.HandInstalled.Adoption.LooseFilesName))
                {
                    _mods.HandInstalled.Adoption.LooseFilesName = "Loose files";
                }

                _mods.HandInstalled.SelectAllCommand.Execute(null);
                await _mods.HandInstalled.AdoptSelectedCommand.ExecuteAsync(null);
                break;
            case NextMoveKind.ClearCopies:
                await OpenTargetAsync(HomeTarget.ModsLibrary, cancellationToken);
                await _mods.KeepEach.PlanCommand.ExecuteAsync(null);
                break;
            case NextMoveKind.AddMods:
                await OpenTargetAsync(HomeTarget.ModsLibrary, cancellationToken);
                await _mods.Library.ImportFileCommand.ExecuteAsync(null);
                break;
            default:
                break;
        }
    }

    private Task OpenTargetAsync(HomeTarget target, CancellationToken cancellationToken) =>
        OpenAsync?.Invoke(target, cancellationToken) ?? Task.CompletedTask;

    private void Update()
    {
        var facts = new NextMoveFacts(
            _mods.HandInstalled.ProfileName is not null,
            _mods.Changes.ChangeUnfinished || _fixes is { ChangeUnfinished: true },
            _mods.Handover.IsShown,
            _mods.HandInstalled.Candidates.Count(c => c.CanManage && c.Candidate!.Files.Count > c.Candidate.Programs.Count),
            _mods.Library.Mods.Count,
            _mods.Activation.ActiveModIds.Count,
            _mods.Library.CopiesToClear);
        var move = NextMoves.For(facts);
        Kind = move.Kind;
        Title = move.Title;
        Body = move.Body;
        Button = move.Button;
        OnPropertyChanged(nameof(IsShown));
        OnPropertyChanged(nameof(HasButton));
        FirstSteps.Clear();
        foreach (var step in Core.Mods.FirstSteps.AreNeeded(facts) ? Core.Mods.FirstSteps.For(facts) : [])
        {
            FirstSteps.Add(step);
        }
    }
}
