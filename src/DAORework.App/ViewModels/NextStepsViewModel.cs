using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DAORework.Application.Catalog;
using DAORework.Application.Mods.Handover;
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
    private readonly ReadVortexHandover _readHandover;

    public NextStepsViewModel(GetProfile getProfile, DetectKnownItems detectKnownItems, ReadVortexHandover readHandover, ILogger<NextStepsViewModel> logger)
        : base(logger)
    {
        _getProfile = getProfile;
        _detectKnownItems = detectKnownItems;
        _readHandover = readHandover;
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

                AddDocumentsStep(issues);
                if (issues.Any(i => i.Code == DiagnosticCodes.LargeAddressAwareDisabled))
                {
                    Steps.Add(new NextStep(
                        NextStepKind.ApplyLargeAddressAware,
                        "Turn on the 4 GB patch",
                        "The game can use only 2 GB of memory, which large mods and texture packs run out of.",
                        "Preview"));
                }

                var failure = await AddDxvkStepAsync(profile, token) ?? await AddVortexStepAsync(profile, token);
                if (failure is not null)
                {
                    ReportError(failure);
                }
                else if (Steps.Count == 0)
                {
                    ReportStatus("Nothing else needs you: the 4 GB patch and DXVK are on.");
                }

                HasContent = true;
            },
            cancellationToken);

    /// <summary>First, since no mod reaches the game until it's sorted out. Names both folders (ADR-0046).</summary>
    private void AddDocumentsStep(IReadOnlyList<DiagnosticIssue> issues)
    {
        if (issues.FirstOrDefault(i => i.Code == DiagnosticCodes.DocumentsNotTheGames) is { } issue)
        {
            Steps.Add(new NextStep(
                NextStepKind.ChooseGameDocumentsFolder,
                "Your mods aren't where the game looks",
                $"{issue.Message} Choose the game's folder in Game folders, or move your saves and mods into it.",
                "Open Game folders"));
        }
    }

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

    /// <summary>Mods found in the game are the next step's to offer (P2.6-27). This is only Vortex coming back, which it doesn't cover.</summary>
    private async Task<Error?> AddVortexStepAsync(RuntimeProfile profile, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(profile.UserDocumentsDirectory))
        {
            return null;
        }

        if (!(await _readHandover.ExecuteAsync(profile.UserDocumentsDirectory, cancellationToken)).TryGetValue(out var handover, out var error))
        {
            return error;
        }

        if (handover.PlacingAgain)
        {
            Steps.Add(new NextStep(
                NextStepKind.MoveFromVortex,
                "Vortex is placing mods again",
                "Stop managing Dragon Age in Vortex, or it will keep changing your game.",
                "See what to do"));
        }

        return null;
    }

    [RelayCommand]
    private Task TakeAsync(NextStep step, CancellationToken cancellationToken) =>
        TakeStepAsync is null ? Task.CompletedTask : TakeStepAsync(step.Kind, cancellationToken);
}
