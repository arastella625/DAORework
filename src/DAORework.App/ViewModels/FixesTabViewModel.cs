namespace DAORework.App.ViewModels;

/// <summary>The Fixes tab (P2.5-06): what is applied, and applying or removing the 4 GB patch and DXVK.</summary>
public sealed class FixesTabViewModel
{
    public FixesTabViewModel(FixStatusViewModel status, FixChangeViewModel change)
    {
        ArgumentNullException.ThrowIfNull(change);
        Status = status;
        Change = change;
        change.AfterChangeAsync = status.RefreshAsync;
    }

    public FixStatusViewModel Status { get; }

    public FixChangeViewModel Change { get; }

    public async Task SelectProfileAsync(string? profileName, CancellationToken cancellationToken = default)
    {
        Change.Load(profileName);
        await Status.LoadAsync(profileName, cancellationToken);
    }
}
