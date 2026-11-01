using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DAORework.Application.Catalog;
using DAORework.Application.Mods.Adoption;
using DAORework.Application.Profiles;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Fixes;
using DAORework.Core.Profiles;
using Microsoft.Extensions.Logging;

namespace DAORework.App.ViewModels;

/// <summary>
/// The first-run suggestions (ROADMAP P2.5-07): after diagnostics, a short list of next steps, each with its button.
/// <c>DAO_LAA_DISABLED</c> offers the 4 GB patch, DXVK missing from the install offers installing it, and mods
/// installed by hand offer adopting them. A button only opens the plan on its tab; nothing is applied without the plan
/// and <i>Apply</i>, and every other diagnostic stays a report.
/// </summary>
public sealed partial class NextStepsViewModel : PageViewModel
{
    private readonly GetProfile _getProfile;
    private readonly DetectKnownItems _detectKnownItems;
    private readonly ScanForAdoption _scanForAdoption;

    public NextStepsViewModel(GetProfile getProfile, DetectKnownItems detectKnownItems, ScanForAdoption scanForAdoption, ILogger<NextStepsViewModel> logger)
        : base(logger)
    {
        _getProfile = getProfile;
        _detectKnownItems = detectKnownItems;
        _scanForAdoption = scanForAdoption;
    }

    /// <summary>Gets or sets what a step's button does: switch to its tab and open its plan.</summary>
    public Func<NextStepKind, CancellationToken, Task>? TakeStepAsync { get; set; }

    public ObservableCollection<NextStep> Steps { get; } = [];

    /// <summary>Gets a value indicating whether there are steps or something to say about them.</summary>
    [ObservableProperty]
    public partial bool HasContent { get; set; }

    public void Clear()
    {
        Steps.Clear();
        ReportSuccess(string.Empty);
        HasContent = false;
    }

    /// <summary>Works out the steps for <paramref name="profileName"/> from its diagnostic <paramref name="issues"/>, the catalog and the adoption scan.</summary>
    public Task ShowAsync(string? profileName, IReadOnlyList<DiagnosticIssue> issues, CancellationToken cancellationToken = default) =>
        GuardAsync(
            async token =>
            {
                ArgumentNullException.ThrowIfNull(issues);
                Clear();
                if (profileName is null)
                {
                    return;
                }

                if (!(await _getProfile.ExecuteAsync(new GetProfileRequest(profileName), token)).TryGetValue(out var profile, out var error))
                {
                    ReportError(error);
                    HasContent = true;
                    return;
                }

                if (issues.Any(i => i.Code == DiagnosticCodes.LargeAddressAwareDisabled))
                {
                    Steps.Add(new NextStep(
                        NextStepKind.ApplyLargeAddressAware,
                        "Turn on the 4 GB patch",
                        "The game can use only 2 GB of memory, which large mods and texture packs run out of.",
                        "Preview"));
                }

                var failure = await AddDxvkStepAsync(profile, token) ?? await AddAdoptStepAsync(profile, token);
                if (failure is not null)
                {
                    ReportError(failure);
                }
                else if (Steps.Count == 0)
                {
                    ReportStatus("Nothing needs you. The 4 GB patch and DXVK are on, and every mod is managed.");
                }

                HasContent = true;
            },
            cancellationToken);

    private async Task<Error?> AddDxvkStepAsync(RuntimeProfile profile, CancellationToken cancellationToken)
    {
        if (!(await _detectKnownItems.ExecuteAsync(new DetectKnownItemsRequest(profile), cancellationToken)).TryGetValue(out var scan, out var error))
        {
            return error;
        }

        if (scan.Missing.Any(item => item.Id == FixIds.Dxvk))
        {
            Steps.Add(new NextStep(
                NextStepKind.InstallDxvk,
                "Install DXVK",
                "DXVK runs the game on Vulkan, which avoids its memory-related crashes and stutter on current Windows. Download a release first.",
                "Choose the release"));
        }

        return null;
    }

    /// <summary>A profile without a documents folder has nothing to adopt yet; the Profile page asks for the folder.</summary>
    private async Task<Error?> AddAdoptStepAsync(RuntimeProfile profile, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(profile.UserDocumentsDirectory))
        {
            return null;
        }

        if (!(await _scanForAdoption.ExecuteAsync(profile.UserDocumentsDirectory, cancellationToken)).TryGetValue(out var adoption, out var error))
        {
            return error;
        }

        if (adoption.Candidates.Count > 0)
        {
            Steps.Add(new NextStep(
                NextStepKind.AdoptHandInstalled,
                $"Manage {Counted.Of(adoption.Candidates.Count, "mod")} found in your game",
                "DAORework can take them over without reinstalling them, so you can turn them off or undo them like any other mod.",
                "See them"));
        }

        return null;
    }

    [RelayCommand]
    private Task TakeAsync(NextStep step, CancellationToken cancellationToken) =>
        TakeStepAsync is null ? Task.CompletedTask : TakeStepAsync(step.Kind, cancellationToken);
}
