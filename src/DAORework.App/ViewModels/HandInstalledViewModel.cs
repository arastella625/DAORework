using System.Collections.ObjectModel;
using System.ComponentModel;
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

    /// <summary>Gets the section's name on the Mods tab, with how many mods could be adopted (P2.5-17).</summary>
    public string SectionHeader => Candidates.Count == 0 ? "Installed by hand" : $"Installed by hand ({Candidates.Count})";

    /// <summary>Gets a value indicating whether any candidate is ticked for <i>Adopt selected</i> (P2.5-13).</summary>
    public bool HasSelection => Candidates.Any(c => c.IsSelected);

    public ObservableCollection<HandInstalledModRow> NotAdoptable { get; } = [];

    [ObservableProperty]
    public partial string? ProfileName { get; set; }

    [ObservableProperty]
    public partial string? OfficialSummary { get; set; }

    public void Load(string? profileName)
    {
        ProfileName = profileName;
        ClearCandidates();
        NotAdoptable.Clear();
        OfficialSummary = null;
        Adoption.Load(profileName);
        ReportSuccess(string.Empty);
    }

    /// <summary>
    /// Reads the list without being asked (P2.5-16): when a profile is chosen and after every change elsewhere (an
    /// adoption, a rollback). It only reads. A profile with no documents folder yet shows an empty list, not an error.
    /// </summary>
    public Task RefreshAsync(CancellationToken cancellationToken = default) => GuardAsync(token => ReadAsync(quiet: true, token), cancellationToken);

    /// <summary>Plans adopting every ticked mod as one change, in the list's order (P2.5-13).</summary>
    [RelayCommand(CanExecute = nameof(HasSelection))]
    private Task AdoptSelectedAsync(CancellationToken cancellationToken) =>
        Adoption.PlanAdoptionAsync([.. Candidates.Where(c => c.IsSelected)], cancellationToken);

    [RelayCommand]
    private void SelectAll() => SetSelected(true);

    [RelayCommand]
    private void ClearSelection() => SetSelected(false);

    [RelayCommand]
    private Task ScanAsync(CancellationToken cancellationToken) => GuardAsync(token => ReadAsync(quiet: false, token), cancellationToken);

    private async Task ReadAsync(bool quiet, CancellationToken cancellationToken)
    {
        if (ProfileName is null)
        {
            return;
        }

        if (!(await _getProfile.ExecuteAsync(new GetProfileRequest(ProfileName), cancellationToken)).TryGetValue(out var profile, out var error))
        {
            ReportError(error);
        }
        else if (string.IsNullOrWhiteSpace(profile.UserDocumentsDirectory))
        {
            if (!quiet)
            {
                ReportError(new Error(DiagnosticCodes.ModDocumentsNotSet, $"Profile '{ProfileName}' has no documents folder.", "Set it on the Profile page."));
            }
        }
        else if (!(await _scan.ExecuteAsync(profile.UserDocumentsDirectory, cancellationToken)).TryGetValue(out var scan, out error))
        {
            ReportError(error);
        }
        else
        {
            Show(scan);
        }
    }

    private void SetSelected(bool selected)
    {
        foreach (var candidate in Candidates)
        {
            candidate.IsSelected = selected;
        }
    }

    private void OnCandidateChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(HandInstalledModRow.IsSelected))
        {
            SelectionChanged();
        }
    }

    private void SelectionChanged()
    {
        OnPropertyChanged(nameof(SectionHeader));
        OnPropertyChanged(nameof(HasSelection));
        AdoptSelectedCommand.NotifyCanExecuteChanged();
    }

    private void ClearCandidates()
    {
        foreach (var candidate in Candidates)
        {
            candidate.PropertyChanged -= OnCandidateChanged;
        }

        Candidates.Clear();
        SelectionChanged();
    }

    private void Show(AdoptionScan scan)
    {
        ClearCandidates();
        NotAdoptable.Clear();
        foreach (var candidate in scan.Candidates)
        {
            var row = HandInstalledModRow.From(candidate);
            row.PropertyChanged += OnCandidateChanged;
            Candidates.Add(row);
        }

        SelectionChanged();

        foreach (var exclusion in scan.Exclusions.Where(e => e.Reason != AdoptionExclusionReason.Official))
        {
            NotAdoptable.Add(HandInstalledModRow.From(exclusion));
        }

        var official = scan.Exclusions.Count(e => e.Reason == AdoptionExclusionReason.Official);
        OfficialSummary = official == 0 ? null : $"Official DLC ({official} add-in(s)) is never adopted or changed.";
        ReportSuccess(scan.Candidates.Count == 0
            ? "Nothing installed by hand can be adopted."
            : $"{scan.Candidates.Count} mod(s) installed by hand could be adopted. Adopt one, or tick several and adopt them together; the plan shows first, and nothing moves until you apply it.");
    }
}
