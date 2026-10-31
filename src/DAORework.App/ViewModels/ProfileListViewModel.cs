using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DAORework.Application.Profiles;
using Microsoft.Extensions.Logging;

namespace DAORework.App.ViewModels;

/// <summary>The profile selector in the header: lists profiles, creates one, and tells the pages which one is selected.</summary>
public sealed partial class ProfileListViewModel : PageViewModel
{
    private readonly ListProfiles _listProfiles;
    private readonly CreateProfile _createProfile;

    public ProfileListViewModel(ListProfiles listProfiles, CreateProfile createProfile, ILogger<ProfileListViewModel> logger)
        : base(logger)
    {
        _listProfiles = listProfiles;
        _createProfile = createProfile;
        NewProfileName = string.Empty;
    }

    public event EventHandler<string?>? SelectionChanged;

    /// <summary>Gets or sets what runs once the first profile has been created and selected: the first-run flow (P2.5-07).</summary>
    public Func<CancellationToken, Task>? AfterFirstProfileAsync { get; set; }

    public ObservableCollection<string> Profiles { get; } = [];

    [ObservableProperty]
    public partial string? SelectedProfile { get; set; }

    [ObservableProperty]
    public partial string NewProfileName { get; set; }

    public Task LoadAsync(CancellationToken cancellationToken = default) =>
        GuardAsync(
            async token =>
            {
                await RefreshAsync(token);
                SelectedProfile ??= Profiles.FirstOrDefault();
            },
            cancellationToken);

    [RelayCommand]
    private Task CreateProfileAsync(CancellationToken cancellationToken) =>
        GuardAsync(
            async token =>
            {
                var first = Profiles.Count == 0;
                var result = await _createProfile.ExecuteAsync(new CreateProfileRequest(NewProfileName), token);
                if (!result.TryGetValue(out var profile, out var error))
                {
                    ReportError(error);
                    return;
                }

                NewProfileName = string.Empty;
                await RefreshAsync(token);
                SelectedProfile = profile.Name;
                ReportSuccess($"Created profile '{profile.Name}'.");
                if (first && AfterFirstProfileAsync is not null)
                {
                    await AfterFirstProfileAsync(token);
                }
            },
            cancellationToken);

    partial void OnSelectedProfileChanged(string? value) => SelectionChanged?.Invoke(this, value);

    private async Task RefreshAsync(CancellationToken cancellationToken)
    {
        var names = (await _listProfiles.ExecuteAsync(new ListProfilesRequest(), cancellationToken)).Value;
        var selected = SelectedProfile;
        Profiles.Clear();
        foreach (var name in names.Order(StringComparer.OrdinalIgnoreCase))
        {
            Profiles.Add(name);
        }

        SelectedProfile = Profiles.Contains(selected ?? string.Empty) ? selected : null;
    }
}
