using CommunityToolkit.Mvvm.ComponentModel;

namespace DAORework.App.ViewModels;

/// <summary>The window: the profile selector and launch action in the header, and the pages below.</summary>
public sealed partial class MainWindowViewModel : ObservableObject
{
    /// <summary>The index of Home, the first tab (ADR-0026).</summary>
    public const int HomePage = 0;

    /// <summary>The index of the Profile tab.</summary>
    public const int ProfilePage = 1;

    /// <summary>The index of the Diagnostics tab.</summary>
    public const int DiagnosticsPage = 2;

    /// <summary>The index of the Mods tab.</summary>
    public const int ModsPage = 3;

    /// <summary>The index of the Saves tab.</summary>
    public const int SavesPage = 4;

    /// <summary>The index of the Fixes tab.</summary>
    public const int FixesPage = 5;

    /// <summary>The index of the Settings tab.</summary>
    public const int SettingsPage = 6;

    /// <summary>The index of the Mods tab's <i>Library</i> section.</summary>
    public const int LibrarySection = 0;

    /// <summary>The index of the Mods tab's <i>Found in your game</i> section (P2.5-17).</summary>
    public const int InstalledByHandSection = 1;

    /// <summary>The index of the Mods tab's <i>Arrange</i> section.</summary>
    public const int ArrangeSection = 2;

    /// <summary>The index of the Mods tab's <i>History</i> section.</summary>
    public const int HistorySection = 3;

    private Task? _initialization;

    public MainWindowViewModel(ProfileListViewModel profiles, LaunchViewModel launch, PagesViewModel pages, SettingsPageViewModel settings, HomeViewModel home)
    {
        Profiles = profiles;
        Launch = launch;
        Pages = pages;
        Settings = settings;
        Home = home;
        Profiles.SelectionChanged += (_, name) => _ = SelectProfileAsync(name);
        Settings.Looks.ListChanged += (_, _) => _ = Pages.Profile.Look.RefreshAsync(); // The Profile page offers the looks made in Settings.

        // The first-run flow (P2.5-07): the first profile is diagnosed at once, and the diagnosis ends in next steps.
        Profiles.AfterFirstProfileAsync = _ => Diagnostics.RunDiagnosticsCommand.ExecuteAsync(null);
        Diagnostics.AfterReportAsync = Home.ShowReportAsync;
        NextSteps.TakeStepAsync = TakeStepAsync;
        Home.Watch(ModsTab, Fixes);
        Home.CheckAsync = _ => Diagnostics.RunDiagnosticsCommand.ExecuteAsync(null);
        Home.NavigateAsync = OpenFromHomeAsync;
    }

    /// <summary>Gets or sets the tab shown; a next step switches to the tab its plan is on.</summary>
    [ObservableProperty]
    public partial int SelectedPage { get; set; }

    /// <summary>Gets or sets the Mods tab's section shown (P2.5-17); the <i>Adopt</i> next step opens <i>Installed by hand</i>.</summary>
    [ObservableProperty]
    public partial int ModsSection { get; set; }

    /// <summary>Gets Home, the first tab (P2.5-25).</summary>
    public HomeViewModel Home { get; }

    /// <summary>Gets the suggested next steps, shown on Home (P2.5-07).</summary>
    public NextStepsViewModel NextSteps => Home.NextSteps;

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
        Home.Show(profileName);
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

    // A Home button: the tab and section it names, and for the last change, its Undo asked there, confirmed first.
    private async Task OpenFromHomeAsync(HomeTarget target, BackupRow? change, CancellationToken cancellationToken)
    {
        switch (target)
        {
            case HomeTarget.Diagnostics:
                SelectedPage = DiagnosticsPage;
                break;
            case HomeTarget.FixesHistory:
                SelectedPage = FixesPage;
                if (change is not null)
                {
                    await Fixes.Status.RollBackToCommand.ExecuteAsync(change);
                }

                break;
            default:
                SelectedPage = ModsPage;
                ModsSection = target switch
                {
                    HomeTarget.ModsArrange => ArrangeSection,
                    HomeTarget.ModsHistory => HistorySection,
                    _ => LibrarySection,
                };
                if (change is not null)
                {
                    await ModsTab.Changes.RollBackToCommand.ExecuteAsync(change);
                }

                break;
        }
    }
}
