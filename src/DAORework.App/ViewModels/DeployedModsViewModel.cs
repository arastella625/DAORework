namespace DAORework.App.ViewModels;

/// <summary>
/// The Mods tab's panels that change what is deployed in the profile's documents folder: activating, the changes and
/// their backups, and the load order. Grouped so the tab stays within five dependencies (ENGINEERING_STANDARDS §3).
/// </summary>
public sealed class DeployedModsViewModel
{
    public DeployedModsViewModel(ModActivationViewModel activation, ModChangesViewModel changes, LoadOrderViewModel loadOrder)
    {
        Activation = activation;
        Changes = changes;
        LoadOrder = loadOrder;
    }

    public ModActivationViewModel Activation { get; }

    public ModChangesViewModel Changes { get; }

    public LoadOrderViewModel LoadOrder { get; }
}
