namespace DAORework.App.ViewModels;

/// <summary>The tab pages, grouped so the window stays within five dependencies (ENGINEERING_STANDARDS §3).</summary>
public sealed class PagesViewModel
{
    public PagesViewModel(
        ProfilePageViewModel profile,
        DiagnosticsPageViewModel diagnostics,
        ModsTabViewModel modsTab,
        SavesPageViewModel saves,
        FixesTabViewModel fixes)
    {
        Profile = profile;
        Diagnostics = diagnostics;
        ModsTab = modsTab;
        Saves = saves;
        Fixes = fixes;
    }

    public ProfilePageViewModel Profile { get; }

    public DiagnosticsPageViewModel Diagnostics { get; }

    /// <summary>Gets the Mods tab's panels.</summary>
    public ModsTabViewModel ModsTab { get; }

    public ModsPageViewModel Mods => ModsTab.Folders;

    /// <summary>Gets the Mods tab's <i>Installed by hand</i> list (P2.5-02).</summary>
    public HandInstalledViewModel HandInstalled => ModsTab.HandInstalled;

    /// <summary>Gets the Mods tab's <i>Library</i> (P3-04).</summary>
    public LibraryViewModel Library => ModsTab.Library;

    public SavesPageViewModel Saves { get; }

    /// <summary>Gets the Fixes tab (P2.5-06).</summary>
    public FixesTabViewModel Fixes { get; }

    /// <summary>Points every page at <paramref name="profileName"/> (null clears them).</summary>
    public async Task SelectProfileAsync(string? profileName, CancellationToken cancellationToken = default)
    {
        Diagnostics.Load(profileName);
        await ModsTab.SelectProfileAsync(profileName, cancellationToken);
        await Profile.LoadAsync(profileName, cancellationToken);
        await Saves.LoadAsync(profileName, cancellationToken);
        await Fixes.SelectProfileAsync(profileName, cancellationToken);
    }
}
