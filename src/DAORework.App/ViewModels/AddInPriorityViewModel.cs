using CommunityToolkit.Mvvm.ComponentModel;
using DAORework.Application.Mods.AddIns;
using DAORework.Application.Profiles;
using DAORework.Core.Mods.AddIns;
using Microsoft.Extensions.Logging;

namespace DAORework.App.ViewModels;

/// <summary>
/// Each enabled add-in's Priority and the files it shares with other add-ins (P2.5-23), for its row on Mods. Only reads:
/// DAORework changes no priority.
/// </summary>
public sealed partial class AddInPriorityViewModel : PageViewModel
{
    private static readonly IReadOnlyDictionary<string, AddInMark> _none = new Dictionary<string, AddInMark>();

    private readonly GetProfile _getProfile;
    private readonly FindAddInClashes _findAddInClashes;
    private string? _profileName;

    public AddInPriorityViewModel(GetProfile getProfile, FindAddInClashes findAddInClashes, ILogger<AddInPriorityViewModel> logger)
        : base(logger)
    {
        _getProfile = getProfile;
        _findAddInClashes = findAddInClashes;
    }

    /// <summary>Gets each enabled add-in's mark, by UID; empty when no profile or documents folder is known.</summary>
    [ObservableProperty]
    public partial IReadOnlyDictionary<string, AddInMark> Marks { get; private set; } = _none;

    /// <summary>Reads <c>AddIns.xml</c> and the add-ins of <paramref name="profileName"/>'s documents folder again.</summary>
    public Task LoadAsync(string? profileName, CancellationToken cancellationToken = default)
    {
        _profileName = profileName;
        return RefreshAsync(cancellationToken);
    }

    /// <summary>Reads them again for the same profile, after a change that may have turned an add-in on or off.</summary>
    public Task RefreshAsync(CancellationToken cancellationToken = default) =>
        GuardAsync(
            async token =>
            {
                ReportSuccess(string.Empty);
                if (_profileName is not { } profileName)
                {
                    Marks = _none;
                    return;
                }

                if (!(await _getProfile.ExecuteAsync(new GetProfileRequest(profileName), token)).TryGetValue(out var profile, out var error)
                    || !(await _findAddInClashes.ExecuteAsync(profile.UserDocumentsDirectory, token)).TryGetValue(out var report, out error))
                {
                    Marks = _none;
                    ReportError(error);
                    return;
                }

                Marks = AddInMarks.For(report);
            },
            cancellationToken);
}
