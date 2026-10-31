using DAORework.Application.Catalog;
using DAORework.Application.Catalog.Matchers;
using DAORework.Application.Diagnostics;
using DAORework.Application.Diagnostics.Rules;
using DAORework.Application.Documents;
using DAORework.Application.Fixes;
using DAORework.Application.Installations;
using DAORework.Application.Launching;
using DAORework.Application.Looks;
using DAORework.Application.Mods;
using DAORework.Application.Mods.Adoption;
using DAORework.Application.Mods.Apply;
using DAORework.Application.Mods.Backups;
using DAORework.Application.Mods.Changes;
using DAORework.Application.Mods.Library;
using DAORework.Application.Preferences;
using DAORework.Application.Profiles;
using DAORework.Application.Saves;
using DAORework.Application.Support;
using Microsoft.Extensions.DependencyInjection;

namespace DAORework.Application;

/// <summary>Registers every use case and rule. Ports are registered by the layer that implements them (Standards §2).</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Diagnostic rules run in registration order (SDD §5, Composite). Add a check by adding a rule here.
        services.AddSingleton<IDiagnosticRule, InstallDirectoryRule>();
        services.AddSingleton<IDiagnosticRule, GameExecutableRule>();
        services.AddSingleton<IDiagnosticRule, LauncherExecutableRule>();
        services.AddSingleton<IDiagnosticRule, DocumentsDirectoryRule>();
        services.AddSingleton<IDiagnosticRule, DocumentsRedirectionRule>();
        services.AddSingleton<IDiagnosticRule, ModFoldersRule>();
        services.AddSingleton<IDiagnosticRule, LargeAddressAwareRule>();
        services.AddSingleton<IDiagnosticRule, SteamProtectedExecutableRule>();
        services.AddSingleton<IDiagnosticRule, PrerequisitesRule>();
        services.AddSingleton<IDiagnosticRule, ConfigRule>();
        services.AddSingleton<IDiagnosticRule, SavesRule>();
        services.AddSingleton<IDiagnosticRule, DlcRule>();
        services.AddSingleton<IDiagnosticRule, KnownItemsRule>();

        // One matcher per fingerprint kind in the known-items catalog (ADR-0011).
        services.AddSingleton<IFingerprintMatcher, AddInUidMatcher>();
        services.AddSingleton<IFingerprintMatcher, OverrideFileMatcher>();
        services.AddSingleton<IFingerprintMatcher, GameFileMatcher>();
        services.AddSingleton<IFingerprintMatcher, LargeAddressAwareMatcher>();

        services.AddSingleton<RunDiagnostics>();
        services.AddSingleton<InstallationProbe>();
        services.AddSingleton<DetectInstallations>();
        services.AddSingleton<ApplyInstallation>();
        services.AddSingleton<DetectDocumentsFolders>();
        services.AddSingleton<CreateProfile>();
        services.AddSingleton<GetProfile>();
        services.AddSingleton<ListProfiles>();
        services.AddSingleton<GetPreferences>();
        services.AddSingleton<SetLook>();
        services.AddSingleton<ListLooks>();
        services.AddSingleton<CreateLook>();
        services.AddSingleton<UpdateLook>();
        services.AddSingleton<DeleteLook>();
        services.AddSingleton<SetProfileLook>();
        services.AddSingleton<SetLookBackground>();
        services.AddSingleton<RemoveLookBackground>();
        services.AddSingleton<AdjustLookBackground>();
        services.AddSingleton<LoadLook>();
        services.AddSingleton<UpdateLaunchTarget>();
        services.AddSingleton<UpdateProfilePath>();
        services.AddSingleton<InspectModFolders>();
        services.AddSingleton<BuildModInventory>();
        services.AddSingleton<LaunchGame>();
        services.AddSingleton<ExportSupportBundle>();
        services.AddSingleton<BackupSaves>();
        services.AddSingleton<ListSaveBackups>();
        services.AddSingleton<DetectKnownItems>();
        services.AddSingleton<BackUpDeployment>();
        services.AddSingleton<RestoreDeployment>();
        services.AddSingleton<JournaledApplyRunner>();
        services.AddSingleton<ApplyModPlan>();
        services.AddSingleton<RecoverModPlan>();
        services.AddSingleton<ImportMod>();
        services.AddSingleton<CheckKnownDownload>();
        services.AddSingleton<ListLibrary>();
        services.AddSingleton<PlanModChange>();
        services.AddSingleton<RollBackModChange>();
        services.AddSingleton<GetModStatus>();
        services.AddSingleton<ScanForAdoption>();
        services.AddSingleton<PlanAdoption>();

        // One strategy per managed fix (ADR-0013). Add a fix by adding its IManagedFix here.
        services.AddSingleton<IManagedFix, LargeAddressAwareFix>();
        services.AddSingleton<IManagedFix, DxvkFix>();
        services.AddSingleton<PlanFix>();
        services.AddSingleton<ApplyFix>();
        services.AddSingleton<GetFixStatus>();

        return services;
    }
}
