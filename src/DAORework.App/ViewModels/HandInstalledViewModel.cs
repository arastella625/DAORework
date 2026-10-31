using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DAORework.Application.Mods.Adoption;
using DAORework.Application.Profiles;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Mods.Adoption;
using Microsoft.Extensions.Logging;

namespace DAORework.App.ViewModels;

/// <summary>
/// The Mods tab's <i>Installed by hand</i> list (ROADMAP P2.5-02, ADR-0015): what in the profile's documents folder
/// DAORework could adopt, and what it cannot and why, from <see cref="ScanForAdoption"/>. Only reads.
/// </summary>
public sealed partial class HandInstalledViewModel : PageViewModel
{
    private readonly GetProfile _getProfile;
    private readonly ScanForAdoption _scan;

    private bool _shown;

    public HandInstalledViewModel(GetProfile getProfile, ScanForAdoption scan, ModAdoptionViewModel adoption, ILogger<HandInstalledViewModel> logger)
        : base(logger)
    {
        _getProfile = getProfile;
        _scan = scan;
        Adoption = adoption;
    }

    /// <summary>Gets adopting the listed candidates (P3-04).</summary>
    public ModAdoptionViewModel Adoption { get; }

    public ObservableCollection<HandInstalledModRow> Candidates { get; } = [];

    public ObservableCollection<HandInstalledModRow> NotAdoptable { get; } = [];

    [ObservableProperty]
    public partial string? ProfileName { get; set; }

    [ObservableProperty]
    public partial string? OfficialSummary { get; set; }

    public void Load(string? profileName)
    {
        ProfileName = profileName;
        Candidates.Clear();
        NotAdoptable.Clear();
        OfficialSummary = null;
        _shown = false;
        Adoption.Load(profileName);
        ReportSuccess(string.Empty);
    }

    /// <summary>Scans again after a change elsewhere (an adoption, a rollback), but only once the user has scanned.</summary>
    public Task RescanAsync(CancellationToken cancellationToken = default) => _shown ? ScanAsync(cancellationToken) : Task.CompletedTask;

    [RelayCommand]
    private Task ScanAsync(CancellationToken cancellationToken) =>
        GuardAsync(
            async token =>
            {
                if (ProfileName is null)
                {
                    return;
                }

                if (!(await _getProfile.ExecuteAsync(new GetProfileRequest(ProfileName), token)).TryGetValue(out var profile, out var error))
                {
                    ReportError(error);
                }
                else if (string.IsNullOrWhiteSpace(profile.UserDocumentsDirectory))
                {
                    ReportError(new Error(DiagnosticCodes.ModDocumentsNotSet, $"Profile '{ProfileName}' has no documents folder.", "Set it on the Profile page."));
                }
                else if (!(await _scan.ExecuteAsync(profile.UserDocumentsDirectory, token)).TryGetValue(out var scan, out error))
                {
                    ReportError(error);
                }
                else
                {
                    Show(scan);
                }
            },
            cancellationToken);

    private void Show(AdoptionScan scan)
    {
        Candidates.Clear();
        NotAdoptable.Clear();
        foreach (var candidate in scan.Candidates)
        {
            Candidates.Add(HandInstalledModRow.From(candidate));
        }

        foreach (var exclusion in scan.Exclusions.Where(e => e.Reason != AdoptionExclusionReason.Official))
        {
            NotAdoptable.Add(HandInstalledModRow.From(exclusion));
        }

        _shown = true;
        var official = scan.Exclusions.Count(e => e.Reason == AdoptionExclusionReason.Official);
        OfficialSummary = official == 0 ? null : $"Official DLC ({official} add-in(s)) is never adopted or changed.";
        ReportSuccess(scan.Candidates.Count == 0
            ? "Nothing installed by hand can be adopted."
            : $"{scan.Candidates.Count} mod(s) installed by hand could be adopted. Adopt shows the plan first; nothing moves until you apply it.");
    }
}
