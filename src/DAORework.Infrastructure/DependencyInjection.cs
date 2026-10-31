using System.IO.Abstractions;
using DAORework.Application.Abstractions;
using DAORework.Application.Common;
using DAORework.Application.Installations;
using DAORework.Application.Launching;
using DAORework.Application.Profiles;
using DAORework.Core.Launching;
using DAORework.Infrastructure.Catalog;
using DAORework.Infrastructure.Fixes;
using DAORework.Infrastructure.Installations;
using DAORework.Infrastructure.Installations.Steam;
using DAORework.Infrastructure.KnownFolders;
using DAORework.Infrastructure.Launching;
using DAORework.Infrastructure.Looks;
using DAORework.Infrastructure.Mods;
using DAORework.Infrastructure.Persistence;
using DAORework.Infrastructure.Registry;
using DAORework.Infrastructure.Safety;
using DAORework.Infrastructure.Saves;
using DAORework.Infrastructure.Support;
using Microsoft.Extensions.DependencyInjection;

namespace DAORework.Infrastructure;

/// <summary>Registers every adapter behind the Application ports, configured through <see cref="DaoReworkOptions"/>.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, Action<DaoReworkOptions> configure)
    {
        services.Configure(configure);

        // Ports for the machine (ADR-0003): the file system, known folders, processes, hashing and the clock.
        services.AddSingleton<IFileSystem, FileSystem>();
        services.AddSingleton<IKnownFolders, EnvironmentKnownFolders>();
        services.AddSingleton<IProcessLauncher, ProcessLauncher>();
        services.AddSingleton<IUriLauncher, ShellUriLauncher>();
        services.AddSingleton<IFileHasher, Sha256FileHasher>();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IRegistry, WindowsRegistry>();

        // Persistence under the app-data folder (SDD §8.4): atomic writes, collision-free report names.
        services.AddSingleton<ReportFileWriter>();
        services.AddSingleton<IProfileRepository, JsonProfileRepository>();
        services.AddSingleton<IPreferencesStore, JsonPreferencesStore>();
        services.AddSingleton<ILookStore, JsonLookStore>();
        services.AddSingleton<IBackgroundImageImporter, SkiaBackgroundImageImporter>();
        services.AddSingleton<IDiagnosticReportWriter, JsonDiagnosticReportWriter>();
        services.AddSingleton<IModInventoryWriter, JsonModInventoryWriter>();
        services.AddSingleton<IDaoModProfileService, JsonDaoModProfileService>();
        services.AddSingleton<ISupportBundleWriter, ZipSupportBundleWriter>();
        services.AddSingleton<ISaveBackupStore, ZipSaveBackupStore>();
        services.AddSingleton<IKnownItemsCatalogSource, EmbeddedKnownItemsCatalogSource>();
        services.AddSingleton<IDeploymentBackupStore, FolderDeploymentBackupStore>();
        services.AddSingleton<IDeploymentRecordStore, JsonDeploymentRecordStore>();
        services.AddSingleton<IApplyJournalStore, FileApplyJournalStore>();
        services.AddSingleton<IDeploymentWriter, DocumentsDeploymentWriter>();
        services.AddSingleton<IModLibrary, FolderModLibrary>();
        services.AddSingleton<IDocumentsScanner, DocumentsScanner>();
        services.AddSingleton<IHandInstalledModsReader, HandInstalledModsReader>();
        services.AddSingleton<IAdoptionImporter, HandInstalledModImporter>();
        services.AddSingleton<IMalwareScanner, AmsiMalwareScanner>();
        services.AddSingleton<IFixLibrary, FolderFixLibrary>();
        services.AddSingleton<IInstallFiles, InstallFolderFiles>();
        services.AddSingleton<IFixSourceReader, FixSourceReader>();

        // Install locators run in registration order (SDD §5, Composite): the launchers' own records first, folder probing last.
        services.AddSingleton<IInstallLocator, SteamLibraryLocator>();
        services.AddSingleton<IInstallLocator, GogRegistryLocator>();
        services.AddSingleton<IInstallLocator, BioWareRegistryLocator>();
        services.AddSingleton<IInstallLocator, WellKnownPathLocator>();

        // Runtimes are keyed by mode (SDD §5, Factory via keyed DI); add a runtime by adding a keyed registration.
        // The original game: through Steam for a Steam install, otherwise by starting the executable (ADR-0022).
        services.AddSingleton<ProcessGameRuntime>();
        services.AddSingleton<SteamGameInstall>();
        services.AddKeyedSingleton<IGameRuntime, SteamAwareGameRuntime>(RuntimeMode.OriginalGame);
        services.AddKeyedSingleton<IGameRuntime, ExperimentalRuntime>(RuntimeMode.ExperimentalRuntime);
        services.AddSingleton<IGameRuntimeResolver, KeyedGameRuntimeResolver>();

        return services;
    }
}
