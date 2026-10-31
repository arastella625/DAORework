using CommunityToolkit.Mvvm.ComponentModel;

namespace DAORework.App.ViewModels;

/// <summary>The window: the profile selector and launch action in the header, and the pages below.</summary>
public sealed partial class MainWindowViewModel : ObservableObject
{
    /// <summary>The Mods tab's index in the window.</summary>
    public const int ModsPage = 2;

    /// <summary>The Fixes tab's index in the window.</summary>
    public const int FixesPage = 4;

    /// <summary>The <i>Installed by hand</i> section's index on the Mods tab (P2.5-17).</summary>
    public const int InstalledByHandSection = 1;

    private Task? _initialization;

    public MainWindowViewModel(ProfileListViewModel profiles, LaunchViewModel launch, PagesViewModel pages, SettingsPageViewModel settings, NextStepsViewModel nextSteps)
    {
        Profiles = profiles;
        Launch = launch;
        Pages = pages;
        Settings = settings;
        NextSteps = nextSteps;
        Profiles.SelectionChanged += (_, name) => _ = SelectProfileAsync(name);
        Settings.Looks.ListChanged += (_, _) => _ = Pages.Profile.Look.RefreshAsync(); // The Profile page offers the looks made in Settings.

        // The first-run flow (P2.5-07): the first profile is diagnosed at once, and the diagnosis ends in next steps.
        Profiles.AfterFirstProfileAsync = _ => Diagnostics.RunDiagnosticsCommand.ExecuteAsync(null);
        Diagnostics.AfterReportAsync = NextSteps.ShowAsync;
        NextSteps.TakeStepAsync = TakeStepAsync;
    }

    /// <summary>Gets or sets the tab shown; a next step switches to the tab its plan is on.</summary>
    [ObservableProperty]
    public partial int SelectedPage { get; set; }

    /// <summary>Gets or sets the Mods tab's section shown (P2.5-17); the <i>Adopt</i> next step opens <i>Installed by hand</i>.</summary>
    [ObservableProperty]
    public partial int ModsSection { get; set; }

    /// <summary>Gets the suggested next steps on the Profile tab (P2.5-07).</summary>
    public NextStepsViewModel NextSteps { get; }

    public ProfileListViewModel Profiles { get; }

    public LaunchViewModel Launch { get; }

    public PagesViewModel Pages { get; }

    public ProfilePageViewModel Profile => Pages.Profile;

    public DiagnosticsPageViewModel Diagnostics => Pages.Diagnostics;

    public ModsPageViewModel Mods => Pages.Mods;

    public HandInstalledViewModel HandInstalled => Pages.HandInstalled;

    public LibraryViewModel Library => Pages.Library;

    public ModsTabViewModel ModsTab => Pages.ModsTab;

    public FixesTabViewModel Fixes => Pages.Fixes;

    public SavesPageViewModel Saves => Pages.Saves;

    /// <summary>Gets the Settings page. It is about the app, not a profile, so it does not follow the selection.</summary>
    public SettingsPageViewModel Settings { get; }

    /// <summary>Loads the profile list and the custom looks once; the first profile becomes selected and the pages follow.</summary>
    public Task InitializeAsync(CancellationToken cancellationToken = default) =>
        _initialization ??= Task.WhenAll(Profiles.LoadAsync(cancellationToken), Settings.LoadAsync(cancellationToken));

    /// <summary>Points the launch action and every page at <paramref name="profileName"/> (null clears them).</summary>
    public Task SelectProfileAsync(string? profileName, CancellationToken cancellationToken = default)
    {
        Launch.ProfileName = profileName;
        NextSteps.Clear();
        return Pages.SelectProfileAsync(profileName, cancellationToken);
    }

    /// <summary>Shows the tab a step belongs on and opens its plan or list there; nothing is applied until the user applies it.</summary>
    private async Task TakeStepAsync(NextStepKind step, CancellationToken cancellationToken)
    {
        switch (step)
        {
            case NextStepKind.ApplyLargeAddressAware:
                SelectedPage = FixesPage;
                await Fixes.Change.ApplyLargeAddressAwareCommand.ExecuteAsync(null);
                break;
            case NextStepKind.InstallDxvk:
                SelectedPage = FixesPage;
                await Fixes.Change.InstallDxvkCommand.ExecuteAsync(null);
                break;
            default:
                SelectedPage = ModsPage;
                ModsSection = InstalledByHandSection;
                await HandInstalled.ScanCommand.ExecuteAsync(null);
                break;
        }
    }
}
