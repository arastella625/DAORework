namespace DAORework.App.ViewModels;

/// <summary>
/// The Mods tab's panels that change what is deployed in the profile's documents folder: activating, the changes and
/// their backups, the load order, and the handover from Vortex. Grouped so the tab stays within five dependencies (ENGINEERING_STANDARDS §3).
/// </summary>
public sealed class DeployedModsViewModel
{
    public DeployedModsViewModel(ModActivationViewModel activation, ModChangesViewModel changes, LoadOrderViewModel loadOrder, VortexHandoverViewModel handover)
    {
        Activation = activation;
        Changes = changes;
        LoadOrder = loadOrder;
        Handover = handover;
    }

    public ModActivationViewModel Activation { get; }

    public ModChangesViewModel Changes { get; }

    public LoadOrderViewModel LoadOrder { get; }

    public VortexHandoverViewModel Handover { get; }
}
