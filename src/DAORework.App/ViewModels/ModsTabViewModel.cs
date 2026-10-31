namespace DAORework.App.ViewModels;

/// <summary>The Mods tab's panels, grouped so the pages stay within five dependencies (ENGINEERING_STANDARDS §3).</summary>
public sealed class ModsTabViewModel
{
    public ModsTabViewModel(
        ModsPageViewModel folders,
        HandInstalledViewModel handInstalled,
        LibraryViewModel library,
        DeployedModsViewModel deployed)
    {
        ArgumentNullException.ThrowIfNull(handInstalled);
        ArgumentNullException.ThrowIfNull(deployed);
        Folders = folders;
        HandInstalled = handInstalled;
        Library = library;
        Activation = deployed.Activation;
        Changes = deployed.Changes;
        LoadOrder = deployed.LoadOrder;
        Activation.ActiveModsChanged += (_, _) => Library.ShowActive(Activation.ActiveModIds);

        // A change made in one panel shows in the others: a new backup after Apply, what is active after a rollback,
        // an adopted mod in the Library and no longer in the list of mods installed by hand, the new load order.
        Activation.AfterChangeAsync = RefreshAfterChangeAsync;
        Changes.AfterChangeAsync = RefreshAfterChangeAsync;
        LoadOrder.AfterChangeAsync = RefreshAfterChangeAsync;
        handInstalled.Adoption.AfterChangeAsync = RefreshAfterChangeAsync;
        handInstalled.Adoption.AfterPlanAsync = library.LoadAsync;
    }

    /// <summary>Gets the summary of the override and AddIns folders and the inventory.</summary>
    public ModsPageViewModel Folders { get; }

    /// <summary>Gets the <i>Installed by hand</i> list (P2.5-02).</summary>
    public HandInstalledViewModel HandInstalled { get; }

    /// <summary>Gets the <i>Library</i> (P3-04).</summary>
    public LibraryViewModel Library { get; }

    /// <summary>Gets activating and deactivating library mods in the selected profile (P3-04).</summary>
    public ModActivationViewModel Activation { get; }

    /// <summary>Gets what is active, the backups to roll back to, and recovery (P3-04).</summary>
    public ModChangesViewModel Changes { get; }

    /// <summary>Gets the load order and the files mods share (P2.5-05).</summary>
    public LoadOrderViewModel LoadOrder { get; }

    /// <summary>Points the panels at <paramref name="profileName"/> and reads the library, which every profile shares.</summary>
    public async Task SelectProfileAsync(string? profileName, CancellationToken cancellationToken = default)
    {
        Folders.Load(profileName);
        HandInstalled.Load(profileName);
        await Library.LoadAsync(cancellationToken);
        await Activation.LoadAsync(profileName, cancellationToken);
        await Changes.LoadAsync(profileName, cancellationToken);
        await LoadOrder.LoadAsync(profileName, cancellationToken);
        await HandInstalled.RefreshAsync(cancellationToken);
    }

    private async Task RefreshAfterChangeAsync(CancellationToken cancellationToken)
    {
        await Library.LoadAsync(cancellationToken);
        await Activation.RefreshActiveAsync(cancellationToken);
        await Changes.RefreshAsync(cancellationToken);
        await LoadOrder.RefreshAsync(cancellationToken);
        await HandInstalled.RefreshAsync(cancellationToken);
    }
}
