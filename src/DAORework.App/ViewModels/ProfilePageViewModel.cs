namespace DAORework.App.ViewModels;

/// <summary>The profile page: the paths section, the profile's look and the detected-installs section, kept in step.</summary>
public sealed class ProfilePageViewModel
{
    public ProfilePageViewModel(ProfilePathsViewModel paths, InstallsViewModel installs, ProfileLookViewModel look)
    {
        Paths = paths;
        Installs = installs;
        Look = look;
        Installs.Applied += (_, profile) => Paths.Show(profile);
    }

    public ProfilePathsViewModel Paths { get; }

    public InstallsViewModel Installs { get; }

    public ProfileLookViewModel Look { get; }

    public async Task LoadAsync(string? profileName, CancellationToken cancellationToken = default)
    {
        Installs.Load(profileName);
        await Paths.LoadAsync(profileName, cancellationToken);
        await Look.LoadAsync(profileName, cancellationToken);
    }
}
