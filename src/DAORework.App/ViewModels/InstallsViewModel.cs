using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DAORework.Application.Documents;
using DAORework.Application.Installations;
using DAORework.Application.Profiles;
using DAORework.Core.Documents;
using DAORework.Core.Installations;
using DAORework.Core.Profiles;
using Microsoft.Extensions.Logging;

namespace DAORework.App.ViewModels;

/// <summary>The detected installs and documents folders on the profile page, with a "Use" button each.</summary>
public sealed partial class InstallsViewModel : PageViewModel
{
    private readonly DetectInstallations _detectInstallations;
    private readonly DetectDocumentsFolders _detectDocumentsFolders;
    private readonly ApplyInstallation _applyInstallation;
    private readonly UpdateProfilePath _updateProfilePath;

    public InstallsViewModel(
        DetectInstallations detectInstallations,
        DetectDocumentsFolders detectDocumentsFolders,
        ApplyInstallation applyInstallation,
        UpdateProfilePath updateProfilePath,
        ILogger<InstallsViewModel> logger)
        : base(logger)
    {
        _detectInstallations = detectInstallations;
        _detectDocumentsFolders = detectDocumentsFolders;
        _applyInstallation = applyInstallation;
        _updateProfilePath = updateProfilePath;
    }

    /// <summary>Raised with the saved profile after an install or documents folder was applied to it.</summary>
    public event EventHandler<RuntimeProfile>? Applied;

    public ObservableCollection<Installation> Installs { get; } = [];

    public ObservableCollection<DocumentsFolder> DocumentsFolders { get; } = [];

    [ObservableProperty]
    public partial string? ProfileName { get; set; }

    [ObservableProperty]
    public partial bool HasDetected { get; set; }

    public void Load(string? profileName)
    {
        ProfileName = profileName;
    }

    [RelayCommand]
    private Task DetectAsync(CancellationToken cancellationToken) =>
        GuardAsync(
            async token =>
            {
                var installations = (await _detectInstallations.ExecuteAsync(new DetectInstallationsRequest(), token)).Value;
                var documents = (await _detectDocumentsFolders.ExecuteAsync(new DetectDocumentsFoldersRequest(), token)).Value;
                Replace(Installs, installations);
                Replace(DocumentsFolders, documents.Candidates);

                HasDetected = true;
                ReportStatus(installations.Count == 0
                    ? "No installation was detected. Use the Browse buttons above."
                    : $"Found {Counted.Of(installations.Count, "installation")}.");
            },
            cancellationToken);

    [RelayCommand]
    private Task UseAsync(Installation installation, CancellationToken cancellationToken) =>
        GuardAsync(
            async token =>
            {
                if (ProfileName is null)
                {
                    return;
                }

                var result = await _applyInstallation.ExecuteAsync(new ApplyInstallationRequest(ProfileName, installation), token);
                if (!result.TryGetValue(out var profile, out var error))
                {
                    ReportError(error);
                    return;
                }

                Applied?.Invoke(this, profile);
                ReportSuccess($"Profile '{profile.Name}' now uses {installation.InstallDirectory}.");
            },
            cancellationToken);

    [RelayCommand]
    private Task UseDocumentsAsync(DocumentsFolder folder, CancellationToken cancellationToken) =>
        GuardAsync(
            async token =>
            {
                if (ProfileName is null)
                {
                    return;
                }

                var request = new UpdateProfilePathRequest(ProfileName, ProfilePathKind.Documents, folder.Path);
                var result = await _updateProfilePath.ExecuteAsync(request, token);
                if (!result.TryGetValue(out var profile, out var error))
                {
                    ReportError(error);
                    return;
                }

                Applied?.Invoke(this, profile);
                ReportSuccess($"Profile '{profile.Name}' now uses the documents folder {folder.Path}.");
            },
            cancellationToken);

    private static void Replace<T>(ObservableCollection<T> target, IReadOnlyList<T> items)
    {
        target.Clear();
        foreach (var item in items)
        {
            target.Add(item);
        }
    }
}
