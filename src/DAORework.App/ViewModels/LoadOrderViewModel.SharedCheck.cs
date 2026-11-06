using CommunityToolkit.Mvvm.ComponentModel;

namespace DAORework.App.ViewModels;

/// <summary>What <i>Combined files</i> and the cards may claim while the files mods share are still being read (P2.6-61).</summary>
public sealed partial class LoadOrderViewModel
{
    /// <summary>Gets whether <see cref="OverlapCount"/> can be believed yet.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SharedCheckNote), nameof(SharedCheckFailed))]
    public partial SharedFilesCheck SharedCheck { get; private set; }

    /// <summary>Gets a value indicating whether the last check failed, so <i>Combined files</i> shows why.</summary>
    public bool SharedCheckFailed => SharedCheck == SharedFilesCheck.Failed;

    /// <summary>Gets what <i>Combined files</i> says in place of an answer, or null once there is one.</summary>
    public string? SharedCheckNote => SharedCheck switch
    {
        SharedFilesCheck.Checking => "Still checking which files your mods share. This can take a minute.",
        SharedFilesCheck.Failed => "Couldn't check which files your mods share.",
        _ => null,
    };

    /// <summary>Reads the saved order and its conflicts again, dropping any unapplied reordering.</summary>
    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        SharedCheck = SharedFilesCheck.Checking;
        await GuardAsync(token => PlanOrderAsync(null, token), cancellationToken);
        if (SharedCheck == SharedFilesCheck.Checking)
        {
            SharedCheck = SharedFilesCheck.Failed;
        }
    }
}
