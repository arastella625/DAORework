using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DAORework.App.Services;
using DAORework.Application.Profiles;
using DAORework.Core.Launching;
using DAORework.Core.Profiles;
using Microsoft.Extensions.Logging;

namespace DAORework.App.ViewModels;

/// <summary>The four paths of a profile with a picker each, and the launch target.</summary>
public sealed partial class ProfilePathsViewModel : PageViewModel
{
    private static readonly string[] _executablePatterns = ["*.exe"];

    private readonly GetProfile _getProfile;
    private readonly UpdateProfilePath _updateProfilePath;
    private readonly UpdateLaunchTarget _updateLaunchTarget;
    private readonly IPathPicker _picker;

    public ProfilePathsViewModel(
        GetProfile getProfile,
        UpdateProfilePath updateProfilePath,
        UpdateLaunchTarget updateLaunchTarget,
        IPathPicker picker,
        ILogger<ProfilePathsViewModel> logger)
        : base(logger)
    {
        _getProfile = getProfile;
        _updateProfilePath = updateProfilePath;
        _updateLaunchTarget = updateLaunchTarget;
        _picker = picker;
    }

    [ObservableProperty]
    public partial string? ProfileName { get; set; }

    [ObservableProperty]
    public partial bool HasProfile { get; set; }

    [ObservableProperty]
    public partial string? InstallDirectory { get; set; }

    [ObservableProperty]
    public partial string? GameExecutablePath { get; set; }

    [ObservableProperty]
    public partial string? LauncherExecutablePath { get; set; }

    [ObservableProperty]
    public partial string? UserDocumentsDirectory { get; set; }

    [ObservableProperty]
    public partial bool IsGameTarget { get; set; }

    [ObservableProperty]
    public partial bool IsLauncherTarget { get; set; }

    public Task LoadAsync(string? profileName, CancellationToken cancellationToken = default) =>
        GuardAsync(
            async token =>
            {
                ProfileName = profileName;
                if (profileName is null)
                {
                    Apply(null);
                    return;
                }

                var result = await _getProfile.ExecuteAsync(new GetProfileRequest(profileName), token);
                if (!result.TryGetValue(out var profile, out var error))
                {
                    Apply(null);
                    ReportError(error);
                    return;
                }

                Apply(profile);
                ReportSuccess(string.Empty);
            },
            cancellationToken);

    /// <summary>Shows a profile another view model just saved, without a reload.</summary>
    public void Show(RuntimeProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ProfileName = profile.Name;
        Apply(profile);
    }

    [RelayCommand]
    private Task BrowseAsync(ProfilePathKind kind, CancellationToken cancellationToken) =>
        GuardAsync(
            async token =>
            {
                if (ProfileName is null)
                {
                    return;
                }

                var path = await PickAsync(kind, token);
                if (path is null)
                {
                    return;
                }

                var result = await _updateProfilePath.ExecuteAsync(new UpdateProfilePathRequest(ProfileName, kind, path), token);
                if (!result.TryGetValue(out var profile, out var error))
                {
                    ReportError(error);
                    return;
                }

                Apply(profile);
                ReportSuccess($"Saved the {Describe(kind)} path.");
            },
            cancellationToken);

    [RelayCommand]
    private Task SetLaunchTargetAsync(LaunchTarget target, CancellationToken cancellationToken) =>
        GuardAsync(
            async token =>
            {
                if (ProfileName is null)
                {
                    return;
                }

                var result = await _updateLaunchTarget.ExecuteAsync(new UpdateLaunchTargetRequest(ProfileName, target), token);
                if (!result.TryGetValue(out var profile, out var error))
                {
                    ReportError(error);
                    return;
                }

                Apply(profile);
                ReportSuccess($"Launch target set to {target}.");
            },
            cancellationToken);

    private static string Describe(ProfilePathKind kind) => kind switch
    {
        ProfilePathKind.Install => "install directory",
        ProfilePathKind.Game => "game executable",
        ProfilePathKind.Launcher => "launcher executable",
        ProfilePathKind.Documents => "documents folder",
        _ => kind.ToString(),
    };

    private Task<string?> PickAsync(ProfilePathKind kind, CancellationToken cancellationToken) => kind switch
    {
        ProfilePathKind.Install => _picker.PickFolderAsync("Choose the Dragon Age: Origins install folder", cancellationToken),
        ProfilePathKind.Documents => _picker.PickFolderAsync("Choose the Dragon Age documents folder", cancellationToken),
        ProfilePathKind.Game => _picker.PickFileAsync("Choose daorigins.exe", "Executables", _executablePatterns, cancellationToken),
        ProfilePathKind.Launcher => _picker.PickFileAsync("Choose DAOriginsLauncher.exe", "Executables", _executablePatterns, cancellationToken),
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown path kind."),
    };

    private void Apply(RuntimeProfile? profile)
    {
        HasProfile = profile is not null;
        InstallDirectory = profile?.InstallDirectory;
        GameExecutablePath = profile?.GameExecutablePath;
        LauncherExecutablePath = profile?.LauncherExecutablePath;
        UserDocumentsDirectory = profile?.UserDocumentsDirectory;
        IsGameTarget = profile?.LaunchTarget == LaunchTarget.Game;
        IsLauncherTarget = profile?.LaunchTarget == LaunchTarget.OfficialLauncher;
    }
}
