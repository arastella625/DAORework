using CommunityToolkit.Mvvm.ComponentModel;

namespace DAORework.App.ViewModels;

/// <summary>The window: the profile selector and launch action in the header, and the pages below.</summary>
public sealed partial class MainWindowViewModel : ObservableObject
{
    /// <summary>The index of Home, the first tab (ADR-0026).</summary>
    public const int HomePage = 0;

    /// <summary>The index of the Mods tab.</summary>
    public const int ModsPage = 1;

    /// <summary>The index of the Fixes tab.</summary>
    public const int FixesPage = 2;

    /// <summary>The index of the Saves tab.</summary>
    public const int SavesPage = 3;

    /// <summary>The index of the History tab.</summary>
    public const int HistoryPage = 4;

    /// <summary>The index of the Settings tab, which also holds the game folders and Troubleshooting.</summary>
    public const int SettingsPage = 5;

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

    /// <summary>Gets or sets whether Settings shows the profile's game folders (P2.5-27).</summary>
    [ObservableProperty]
    public partial bool GameFoldersOpen { get; set; }

    /// <summary>Gets or sets whether Settings shows Troubleshooting: the diagnostics and the folder figures.</summary>
    [ObservableProperty]
    public partial bool TroubleshootingOpen { get; set; }

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

    /// <summary>Gets the change running now, shown across the window, which locks until it ends (P2.6-22).</summary>
    public ChangeInProgress Change => ModsTab.Activation.Change;

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
            case NextStepKind.MoveFromVortex:
                SelectedPage = ModsPage;
                await ModsTab.Handover.RefreshAsync(cancellationToken);
                break;
            default:
                SelectedPage = ModsPage;
                ModsTab.Filter.ShowOnlyCommand.Execute(ModShowFilter.Found);
                await HandInstalled.ScanCommand.ExecuteAsync(null);
                break;
        }
    }

    // A Home button: the tab and section it names, and for the last change, its Undo asked in History, confirmed first.
    private async Task OpenFromHomeAsync(HomeTarget target, BackupRow? change, CancellationToken cancellationToken)
    {
        switch (target)
        {
            case HomeTarget.Diagnostics:
                SelectedPage = SettingsPage;
                TroubleshootingOpen = true;
                break;
            case HomeTarget.GameFolders:
                SelectedPage = SettingsPage;
                GameFoldersOpen = true;
                break;
            case HomeTarget.ModsHistory or HomeTarget.FixesHistory:
                SelectedPage = HistoryPage;
                if (change is not null)
                {
                    var changes = target == HomeTarget.ModsHistory ? (RecoverableChangesViewModel)ModsTab.Changes : Fixes.Status;
                    await changes.RollBackToCommand.ExecuteAsync(change);
                }

                break;
            default:
                SelectedPage = ModsPage;
                ShowOnModsPage(target);
                break;
        }
    }

    // Home's cards open the Mods page as they say: Arrange for an overlap, the combined files, or every mod.
    private void ShowOnModsPage(HomeTarget target)
    {
        var filter = ModsTab.Filter;
        filter.Kind = target == HomeTarget.ModsCombined ? ModKindFilter.CombinedFiles : ModKindFilter.All;
        filter.Show = ModShowFilter.All;
        filter.Arranging = target == HomeTarget.ModsArrange;
    }
}
