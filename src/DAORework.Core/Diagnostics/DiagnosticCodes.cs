namespace DAORework.Core.Diagnostics;

/// <summary>
/// The catalog of diagnostic codes. Format: DAO_&lt;AREA&gt;_&lt;CONDITION&gt;.
/// A released code is never renamed or reused; deprecate it instead (SDD §8.3).
/// Rules and tests reference these constants, never a string literal.
/// </summary>
public static class DiagnosticCodes
{
    public const string InstallDirectoryMissing = "DAO_INSTALL_DIRECTORY_MISSING";
    public const string InstallDirectoryNotFound = "DAO_INSTALL_DIRECTORY_NOT_FOUND";

    public const string GameExecutableMissing = "DAO_GAME_EXECUTABLE_MISSING";
    public const string GameExecutableNotFound = "DAO_GAME_EXECUTABLE_NOT_FOUND";

    public const string LauncherExecutableMissing = "DAO_LAUNCHER_EXECUTABLE_MISSING";
    public const string LauncherExecutableNotFound = "DAO_LAUNCHER_EXECUTABLE_NOT_FOUND";

    public const string DocumentsDirectoryNotFound = "DAO_DOCUMENTS_DIRECTORY_NOT_FOUND";
    public const string DocumentsDirectoryElsewhere = "DAO_DOCUMENTS_DIRECTORY_ELSEWHERE";
    public const string DocumentsOneDriveRedirected = "DAO_DOCUMENTS_ONEDRIVE_REDIRECTED";

    public const string OverrideDirectoryFound = "DAO_OVERRIDE_DIRECTORY_FOUND";
    public const string OverrideDirectoryNotFound = "DAO_OVERRIDE_DIRECTORY_NOT_FOUND";

    public const string AddInsDirectoryFound = "DAO_ADDINS_DIRECTORY_FOUND";
    public const string AddInsDirectoryNotFound = "DAO_ADDINS_DIRECTORY_NOT_FOUND";

    public const string ProfileValid = "DAO_PROFILE_VALID";

    // P1-03: the extended rule set. Every code below is report-only; none blocks a launch.
    public const string LargeAddressAwareEnabled = "DAO_LAA_ENABLED";
    public const string LargeAddressAwareDisabled = "DAO_LAA_DISABLED";
    public const string LargeAddressAwareUnreadable = "DAO_LAA_UNREADABLE";

    public const string PrerequisitesNotChecked = "DAO_PREREQ_NOT_CHECKED";
    public const string DirectXRuntimeFound = "DAO_PREREQ_DIRECTX_FOUND";
    public const string DirectXRuntimeMissing = "DAO_PREREQ_DIRECTX_MISSING";
    public const string VcRedistFound = "DAO_PREREQ_VCREDIST_FOUND";
    public const string VcRedistMissing = "DAO_PREREQ_VCREDIST_MISSING";

    public const string ConfigFound = "DAO_CONFIG_FOUND";
    public const string ConfigNotFound = "DAO_CONFIG_NOT_FOUND";
    public const string ConfigUnreadable = "DAO_CONFIG_UNREADABLE";
    public const string ConfigResolutionSuspicious = "DAO_CONFIG_RESOLUTION_SUSPICIOUS";

    public const string SavesFound = "DAO_SAVES_FOUND";
    public const string SavesNone = "DAO_SAVES_NONE";
    public const string SavesEmptyFile = "DAO_SAVES_EMPTY_FILE";
    public const string SavesMany = "DAO_SAVES_MANY";

    public const string DlcFound = "DAO_DLC_FOUND";
    public const string DlcNone = "DAO_DLC_NONE";
    public const string DlcListUnreadable = "DAO_DLC_LIST_UNREADABLE";

    // P1-07: the known-items catalog (ADR-0009 tier 1, ADR-0011). Report only.
    public const string KnownItemsFound = "DAO_KNOWN_ITEMS_FOUND";
    public const string KnownItemsNone = "DAO_KNOWN_ITEMS_NONE";
    public const string KnownItemSuggested = "DAO_KNOWN_ITEM_SUGGESTED";
    public const string KnownItemConflict = "DAO_KNOWN_ITEM_CONFLICT";
    public const string KnownItemWrongArchitecture = "DAO_KNOWN_ITEM_WRONG_ARCHITECTURE";
    public const string KnownItemUnrecognized = "DAO_KNOWN_ITEM_UNRECOGNIZED";
    public const string KnownItemUnreadable = "DAO_KNOWN_ITEM_UNREADABLE";
    public const string CatalogInvalid = "DAO_CATALOG_INVALID";

    // P1-05: save backups (copied out of the saves folder into app-data only).
    public const string SaveBackupNothingToBackUp = "DAO_SAVE_BACKUP_NOTHING_TO_BACK_UP";
    public const string SaveBackupFailed = "DAO_SAVE_BACKUP_FAILED";
    public const string SaveBackupRetentionInvalid = "DAO_SAVE_BACKUP_RETENTION_INVALID";

    // P2-02: the mod library and conflict scanner (ADR-0012).
    public const string ModPathInvalid = "DAO_MOD_PATH_INVALID";
    public const string ModListDuplicate = "DAO_MOD_LIST_DUPLICATE";

    // P2-04: backups of what a plan touches, and restoring them.
    public const string ModPlanBlocked = "DAO_MOD_PLAN_BLOCKED";
    public const string ModPlanEmpty = "DAO_MOD_PLAN_EMPTY";
    public const string ModBackupFailed = "DAO_MOD_BACKUP_FAILED";
    public const string ModBackupNotFound = "DAO_MOD_BACKUP_NOT_FOUND";
    public const string ModRestoreFailed = "DAO_MOD_RESTORE_FAILED";

    // P2-05: the journaled executor.
    public const string ModApplyFailed = "DAO_MOD_APPLY_FAILED";
    public const string ModApplyUnfinished = "DAO_MOD_APPLY_UNFINISHED";
    public const string ModNothingToRecover = "DAO_MOD_NOTHING_TO_RECOVER";
    public const string ModRecordUnreadable = "DAO_MOD_RECORD_UNREADABLE";

    // P2-07: importing mods into the library.
    public const string ModImportUnsupported = "DAO_MOD_IMPORT_UNSUPPORTED";
    public const string ModImportInvalid = "DAO_MOD_IMPORT_INVALID";
    public const string ModImportFailed = "DAO_MOD_IMPORT_FAILED";

    // P2-06: the mods activate, deactivate, rollback and recover commands.
    public const string ModNotInLibrary = "DAO_MOD_NOT_IN_LIBRARY";
    public const string ModNotActive = "DAO_MOD_NOT_ACTIVE";
    public const string ModDocumentsNotSet = "DAO_MOD_DOCUMENTS_NOT_SET";
    public const string ModScanFailed = "DAO_MOD_SCAN_FAILED";

    // P2.5-10: the app's own preferences, such as the theme (ADR-0016, ADR-0017).
    public const string PreferencesUnreadable = "DAO_PREFERENCES_UNREADABLE";

    // P2.5-11: custom looks (ADR-0017).
    public const string LookColorInvalid = "DAO_LOOK_COLOR_INVALID";
    public const string LookAccentReserved = "DAO_LOOK_ACCENT_RESERVED";
    public const string LookAccentUnreadable = "DAO_LOOK_ACCENT_UNREADABLE";
    public const string LookNotFound = "DAO_LOOK_NOT_FOUND";
    public const string LookNameInvalid = "DAO_LOOK_NAME_INVALID";
    public const string LookUnreadable = "DAO_LOOK_UNREADABLE";
    public const string LookImageUnreadable = "DAO_LOOK_IMAGE_UNREADABLE";
    public const string LookImageTooLarge = "DAO_LOOK_IMAGE_TOO_LARGE";
    public const string LookBackgroundMissing = "DAO_LOOK_BACKGROUND_MISSING";

    // P2.5-02: the scan for hand-installed mods to adopt (ADR-0015).
    public const string ModAdoptScanFailed = "DAO_MOD_ADOPT_SCAN_FAILED";

    // P2.5-03: adopting hand-installed mods (ADR-0015).
    public const string ModAdoptAlreadyActive = "DAO_MOD_ADOPT_ALREADY_ACTIVE";
    public const string ModAdoptSourceMissing = "DAO_MOD_ADOPT_SOURCE_MISSING";
    public const string ModAdoptNameRequired = "DAO_MOD_ADOPT_NAME_REQUIRED";
    public const string ModAdoptNotACandidate = "DAO_MOD_ADOPT_NOT_A_CANDIDATE";

    // P2.5-12: mod safety (ADR-0018).
    public const string ModSafetyThreatFound = "DAO_MOD_SAFETY_THREAT_FOUND";
    public const string ModSafetyDownloadDiffers = "DAO_MOD_SAFETY_DOWNLOAD_DIFFERS";

    // ADR-0023: Steam's copy of daorigins.exe is protected by SteamStub, and Steam refuses it once patched.
    public const string SteamExecutableProtected = "DAO_STEAM_EXE_PROTECTED";
    public const string SteamExecutablePatched = "DAO_STEAM_EXE_PATCHED";
    public const string FixSteamProtected = "DAO_FIX_STEAM_PROTECTED";

    public const string FixSourceInvalid = "DAO_FIX_SOURCE_INVALID";
    public const string FixSourceRequired = "DAO_FIX_SOURCE_REQUIRED";
    public const string FixUnknown = "DAO_FIX_UNKNOWN";
    public const string FixNotNeeded = "DAO_FIX_NOT_NEEDED";
    public const string FixNotApplied = "DAO_FIX_NOT_APPLIED";
    public const string FixInstallNotSet = "DAO_FIX_INSTALL_NOT_SET";
    public const string FixInstallNotWritable = "DAO_FIX_INSTALL_NOT_WRITABLE";
    public const string FixFileInUse = "DAO_FIX_FILE_IN_USE";
    public const string FixInstallUnreadable = "DAO_FIX_INSTALL_UNREADABLE";
    public const string FixLibraryFailed = "DAO_FIX_LIBRARY_FAILED";

    public const string ProfileNameEmpty = "DAO_PROFILE_NAME_EMPTY";
    public const string ProfileNameTooLong = "DAO_PROFILE_NAME_TOO_LONG";
    public const string ProfileNameInvalidCharacters = "DAO_PROFILE_NAME_INVALID_CHARACTERS";
    public const string ProfileNameReserved = "DAO_PROFILE_NAME_RESERVED";

    public const string FileReadFailed = "DAO_FILE_READ_FAILED";
    public const string ProcessStartFailed = "DAO_PROCESS_START_FAILED";
    public const string ModFolderUnreadable = "DAO_MOD_FOLDER_UNREADABLE";

    public const string ProfileNotFound = "DAO_PROFILE_NOT_FOUND";
    public const string ProfilePathInvalid = "DAO_PROFILE_PATH_INVALID";
    public const string ProfileValidationFailed = "DAO_PROFILE_VALIDATION_FAILED";
    public const string RuntimeModeUnsupported = "DAO_RUNTIME_MODE_UNSUPPORTED";
    public const string RuntimeExperimentalUnavailable = "DAO_RUNTIME_EXPERIMENTAL_UNAVAILABLE";
}
