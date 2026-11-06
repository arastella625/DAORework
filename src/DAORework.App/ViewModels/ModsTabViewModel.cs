using DAORework.Application.Mods.Library;
using DAORework.Core.Mods.Adoption;
using DAORework.Core.Mods.Planning;

namespace DAORework.App.ViewModels;

/// <summary>The Mods tab's panels, grouped so the pages stay within five dependencies (ENGINEERING_STANDARDS §3).</summary>
public sealed class ModsTabViewModel
{
    public ModsTabViewModel(
        ModsPageViewModel folders,
        HandInstalledViewModel handInstalled,
        LibraryViewModel library,
        DeployedModsViewModel deployed,
        LibraryActionsViewModel actions)
    {
        ArgumentNullException.ThrowIfNull(handInstalled);
        ArgumentNullException.ThrowIfNull(deployed);
        ArgumentNullException.ThrowIfNull(library);
        ArgumentNullException.ThrowIfNull(actions);
        Folders = folders;
        ModPage = actions.ModPage;
        LookFirst = actions.LookFirst;
        KeepCopy = actions.KeepCopy;
        HandInstalled = handInstalled;
        Library = library;
        Activation = deployed.Activation;
        Changes = deployed.Changes;
        LoadOrder = deployed.LoadOrder;
        Handover = deployed.Handover;
        Handover.AfterCopyAsync = library.LoadAsync;
        NewDownloads = deployed.NewDownloads;
        NewDownloads.AddAsync = library.AddPathsAsync;
        library.LookFirstAsync = (paths, token) => LookFirst.HoldBackAsync(paths, FoundInGame(), token);
        LookFirst.AddAsync = library.ImportPathsAsync;
        LookFirst.ManageAsync = ManageHeldAsync;
        Handover.TurnOnAsync = TurnOnAsync;
        Activation.ActiveModsChanged += (_, _) => Library.ShowActive(Activation.ActiveModIds);
        LoadOrder.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(LoadOrderViewModel.ConflictMarks))
            {
                library.ShowConflicts(LoadOrder.ConflictMarks);
            }
        };
        Filter = new ModListFilter(library.Mods, handInstalled.Candidates);
        Selection = new LibrarySelectionViewModel(library.Mods, Filter.VisibleMods, Activation);
        Setup = new SetupSummary(this);
        NextMove = new NextMoveCard(this);

        // A change made in one panel shows in the others: a new backup after Apply, what is active after a rollback,
        // an adopted mod in the Library and no longer in the list of mods installed by hand, the new load order.
        Activation.AfterChangeAsync = RefreshAfterChangeAsync;
        Changes.AfterChangeAsync = RefreshAfterChangeAsync;
        LoadOrder.AfterChangeAsync = RefreshAfterChangeAsync;
        handInstalled.Adoption.AfterChangeAsync = RefreshAfterChangeAsync;
        KeepCopy.AfterChangeAsync = RefreshAfterChangeAsync;
        handInstalled.Adoption.AfterPlanAsync = library.LoadAsync;

        // Plans and Changes name mods by their Library names, not their ids (P2.5-18, P2.5-19).
        Activation.Preview.NameOf = NameOf;
        KeepCopy.Preview.NameOf = NameOf;
        handInstalled.Adoption.Preview.NameOf = NameOf;
        Changes.NameOf = NameOf;
    }

    /// <summary>Gets opening a known mod's page in the browser (ADR-0034 §7).</summary>
    public ModPageViewModel ModPage { get; }

    /// <summary>Gets <i>Keep this one</i> (ADR-0039).</summary>
    public KeepCopyViewModel KeepCopy { get; }

    /// <summary>Gets the look before adding, and what it held back (ADR-0039).</summary>
    public LookFirstViewModel LookFirst { get; }

    /// <summary>Gets the summary of the override and AddIns folders and the inventory.</summary>
    public ModsPageViewModel Folders { get; }

    /// <summary>Gets the <i>Installed by hand</i> list (P2.5-02).</summary>
    public HandInstalledViewModel HandInstalled { get; }

    /// <summary>Gets the <i>Library</i> (P3-04).</summary>
    public LibraryViewModel Library { get; }

    /// <summary>Gets the Library's tick boxes and activating or deactivating the ticked mods together (P2.5-14).</summary>
    public LibrarySelectionViewModel Selection { get; }

    /// <summary>Gets activating and deactivating library mods in the selected profile (P3-04).</summary>
    public ModActivationViewModel Activation { get; }

    /// <summary>Gets what is active, the backups to roll back to, and recovery (P3-04).</summary>
    public ModChangesViewModel Changes { get; }

    /// <summary>Gets the load order and the files mods share (P2.5-05).</summary>
    public LoadOrderViewModel LoadOrder { get; }

    /// <summary>Gets the <i>Move from Vortex</i> card (ADR-0028 §3).</summary>
    public VortexHandoverViewModel Handover { get; }

    /// <summary>Gets the <i>New downloads</i> card (ADR-0034 §5).</summary>
    public NewDownloadsViewModel NewDownloads { get; }

    /// <summary>Gets the kind cards, the chips and Arrange, which filter the page (ADR-0026, P2.5-26).</summary>
    public ModListFilter Filter { get; }

    /// <summary>Gets the three kinds of mod with what is on and whether each is ready; the cards here and on Home.</summary>
    public SetupSummary Setup { get; }

    /// <summary>Gets the one next step, here and on Home (P2.6-27).</summary>
    public NextMoveCard NextMove { get; }

    /// <summary>Points the panels at <paramref name="profileName"/> and reads the library, which every profile shares.</summary>
    public async Task SelectProfileAsync(string? profileName, CancellationToken cancellationToken = default)
    {
        Folders.Load(profileName);
        KeepCopy.ProfileName = profileName;
        HandInstalled.Load(profileName);
        await Activation.LoadAsync(profileName, cancellationToken); // First, so a listed mod's state is known as it is read.
        await Library.LoadAsync(cancellationToken);
        await Changes.LoadAsync(profileName, cancellationToken);
        await LoadOrder.LoadAsync(profileName, cancellationToken);
        await HandInstalled.RefreshAsync(cancellationToken);
        await Handover.LoadAsync(profileName, cancellationToken);
        await NewDownloads.LoadAsync(cancellationToken);
    }

    private Task TurnOnAsync(IReadOnlyList<string> modIds, IReadOnlyList<AddInsListChange> restoredAddIns, CancellationToken cancellationToken)
    {
        Filter.ShowOnlyCommand.Execute(ModShowFilter.All); // The preview shows with your mods, which a chip may be hiding.
        var rows = modIds.Select(id => Library.Mods.FirstOrDefault(m => string.Equals(m.Id, id, StringComparison.OrdinalIgnoreCase))).OfType<LibraryModRow>().ToList();
        return Activation.PlanChangeAsync(ModChangeKind.Activate, rows, restoredAddIns, cancellationToken);
    }

    private List<AdoptionCandidate> FoundInGame() => [.. HandInstalled.Candidates.Select(c => c.Candidate).OfType<AdoptionCandidate>()];

    /// <summary>Plans managing what the held downloads look like. Loose files have no name of their own, so they take the download's.</summary>
    private Task ManageHeldAsync(IReadOnlyList<HeldDownload> held, CancellationToken cancellationToken)
    {
        var ids = held.Select(h => h.Held.InGame?.Id).ToHashSet(StringComparer.Ordinal);
        var rows = HandInstalled.Candidates.Where(c => c.CanManage && ids.Contains(c.Candidate?.Id)).ToList();
        if (rows.Any(r => r.NeedsName) && string.IsNullOrWhiteSpace(HandInstalled.Adoption.LooseFilesName))
        {
            HandInstalled.Adoption.LooseFilesName = held.First(h => h.Held.InGame?.Kind == AdoptionCandidateKind.LooseOverrideFiles).Name;
        }

        return HandInstalled.Adoption.PlanAdoptionAsync(rows, cancellationToken);
    }

    private string NameOf(string modId) =>
        Library.Mods.FirstOrDefault(m => string.Equals(m.Id, modId, StringComparison.OrdinalIgnoreCase))?.Title ?? modId;

    private async Task RefreshAfterChangeAsync(CancellationToken cancellationToken)
    {
        await Library.LoadAsync(cancellationToken);
        await Activation.RefreshActiveAsync(cancellationToken);
        await Changes.RefreshAsync(cancellationToken);
        await LoadOrder.RefreshAsync(cancellationToken);
        await HandInstalled.RefreshAsync(cancellationToken);
        await Handover.RefreshAsync(cancellationToken);
    }
}
