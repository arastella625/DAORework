namespace DAORework.Application.Common;

/// <summary>Where DAORework keeps its own data (SDD §8.5). Bound through <c>IOptions&lt;T&gt;</c>; tests set it directly.</summary>
public sealed class DaoReworkOptions
{
    public string AppDataDirectory { get; set; } = string.Empty;

    public string ProfilesDirectory => Path.Combine(AppDataDirectory, "profiles");

    public string ReportsDirectory => Path.Combine(AppDataDirectory, "reports");

    public string ModProfilesDirectory => Path.Combine(AppDataDirectory, "mod-profiles");

    public string LogsDirectory => Path.Combine(AppDataDirectory, "logs");

    /// <summary>Gets the app's own preferences, such as the theme (ADR-0016, ADR-0017).</summary>
    public string PreferencesFile => Path.Combine(AppDataDirectory, "preferences.json");

    /// <summary>Gets the custom looks: <c>looks\{lookId}\look.json</c> and the look's picture beside it (ADR-0017).</summary>
    public string LooksDirectory => Path.Combine(AppDataDirectory, "looks");

    public string SaveBackupsDirectory => Path.Combine(AppDataDirectory, "save-backups");

    /// <summary>Gets where the backups of mod changes go, one folder per documents folder (ADR-0012, P2-04).</summary>
    public string DeploymentBackupsDirectory => Path.Combine(AppDataDirectory, "deployment-backups");

    /// <summary>Gets the mod library: each imported mod unchanged in <c>library\{modId}\</c> (ADR-0012).</summary>
    public string LibraryDirectory => Path.Combine(AppDataDirectory, "library");

    /// <summary>Gets where each documents folder's deployment record and apply journal are kept (ADR-0012, P2-05).</summary>
    public string DeploymentsDirectory => Path.Combine(AppDataDirectory, "deployments");
}
