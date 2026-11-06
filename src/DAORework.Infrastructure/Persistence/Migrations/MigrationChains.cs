using DAORework.Core.Looks;
using DAORework.Core.Mods;
using DAORework.Core.Mods.Apply;
using DAORework.Core.Mods.Backups;
using DAORework.Core.Mods.Downloads;
using DAORework.Core.Mods.Handover;
using DAORework.Core.Mods.Library;
using DAORework.Core.Mods.Troubleshooting;
using DAORework.Core.Preferences;
using DAORework.Core.Profiles;

namespace DAORework.Infrastructure.Persistence.Migrations;

/// <summary>One chain per loaded document type. Adding a schema version means adding its migration here.</summary>
public static class MigrationChains
{
    public static JsonMigrationChain RuntimeProfiles { get; } = new(RuntimeProfile.CurrentSchemaVersion, new NameFromFileStemMigration());

    /// <summary>Gets the chain for <c>preferences.json</c>, new at version 1 (ADR-0017). A file without a version is read as 1.</summary>
    public static JsonMigrationChain Preferences { get; } = new(AppPreferences.CurrentSchemaVersion, new UnchangedMigration(0));

    /// <summary>Gets the chain for a look's <c>look.json</c>, new at version 1 (ADR-0017).</summary>
    public static JsonMigrationChain Looks { get; } = new(Look.CurrentSchemaVersion, new UnchangedMigration(0));

    /// <summary>Gets the chain for a handover from Vortex, new at version 1 (ADR-0028).</summary>
    public static JsonMigrationChain VortexHandovers { get; } = new(VortexHandover.CurrentSchemaVersion, new UnchangedMigration(0));

    /// <summary>Gets the chain for a search for the problem mod, new at version 1 (ADR-0042).</summary>
    public static JsonMigrationChain ProblemSearches { get; } = new(ProblemSearch.CurrentSchemaVersion, new UnchangedMigration(0));

    /// <summary>Gets the chain for a list of recent games, new at version 1 (ADR-0043).</summary>
    public static JsonMigrationChain PlayHistories { get; } = new(PlayHistory.CurrentSchemaVersion, new UnchangedMigration(0));

    /// <summary>Gets the chain for <c>new-downloads.json</c>, new at version 1 (ADR-0034).</summary>
    public static JsonMigrationChain NewDownloads { get; } = new(NewDownloadsSettings.CurrentSchemaVersion, new UnchangedMigration(0));

    public static JsonMigrationChain ModProfiles { get; } = new(DaoModProfile.CurrentSchemaVersion, new NameFromFileStemMigration());

    public static JsonMigrationChain LibraryEntries { get; } =
        new(LibraryEntry.CurrentSchemaVersion, new UnversionedLibraryEntryMigration(), new DownloadOriginMigration(), new UnchangedMigration(2));

    /// <summary>Gets the chain for backup manifests: version 2 adds moves (ADR-0015). Every manifest has carried <c>schemaVersion</c>.</summary>
    public static JsonMigrationChain DeploymentBackups { get; } = new(DeploymentBackup.CurrentSchemaVersion, new UnchangedMigration(0), new UnchangedMigration(1));

    /// <summary>Gets the chain for apply journals: version 2 adds move steps (ADR-0015), and version 3 an entry put back as it was (ADR-0028). Every journal has carried <c>schemaVersion</c>.</summary>
    public static JsonMigrationChain ApplyJournals { get; } = new(ApplyJournal.CurrentSchemaVersion, new UnchangedMigration(0), new UnchangedMigration(1), new UnchangedMigration(2));
}
