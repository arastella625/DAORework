namespace DAORework.Core.Common;

/// <summary>
/// The single source of truth for Dragon Age: Origins layout conventions (SDD §6, audit B1).
/// Pure path arithmetic: nothing here touches the disk.
/// </summary>
public static class GamePaths
{
    public const string GameExecutableFileName = "daorigins.exe";
    public const string LauncherExecutableFileName = "DAOriginsLauncher.exe";
    public const string BinShipDirectoryName = "bin_ship";
    public const string AddInsDirectoryName = "AddIns";

    /// <summary>Where the game keeps user data, relative to the user's Documents folder.</summary>
    public static readonly string DocumentsRelativePath = Path.Combine("BioWare", "Dragon Age");

    /// <summary>Where the game executable lives, relative to the install directory.</summary>
    public static readonly string GameExecutableRelativePath = Path.Combine(BinShipDirectoryName, GameExecutableFileName);

    /// <summary>Where loose override mods live, relative to the documents directory.</summary>
    public static readonly string OverrideRelativePath = Path.Combine("packages", "core", "override");

    /// <summary>Install locations to probe, relative to a Program Files directory.</summary>
    public static readonly IReadOnlyList<string> KnownInstallRelativePaths =
    [
        Path.Combine("Steam", "steamapps", "common", "Dragon Age Ultimate Edition"),
        Path.Combine("Steam", "steamapps", "common", "Dragon Age Origins"),
        Path.Combine("GOG Galaxy", "Games", "Dragon Age Origins"),
        Path.Combine("EA Games", "Dragon Age"),
        Path.Combine("Origin Games", "Dragon Age"),
    ];

    public static string GameExecutable(string installDirectory) => Path.Combine(installDirectory, GameExecutableRelativePath);

    public static string LauncherExecutable(string installDirectory) => Path.Combine(installDirectory, LauncherExecutableFileName);

    public static string DefaultDocumentsDirectory(string myDocumentsDirectory) => Path.Combine(myDocumentsDirectory, DocumentsRelativePath);

    public static string OverrideDirectory(string documentsDirectory) => Path.Combine(documentsDirectory, OverrideRelativePath);

    public static string AddInsDirectory(string documentsDirectory) => Path.Combine(documentsDirectory, AddInsDirectoryName);

    /// <summary>The game's settings file, written on first launch.</summary>
    public static string SettingsFile(string documentsDirectory) => Path.Combine(documentsDirectory, "Settings", "DragonAge.ini");

    /// <summary>The list of installed add-ins (DLC and add-in mods) the game keeps.</summary>
    public static string AddInsListFile(string documentsDirectory) => Path.Combine(documentsDirectory, "Settings", "AddIns.xml");

    /// <summary>Where the game keeps every character's saves: <c>Characters\{name}\Saves\Slot_{n}\*.das</c>.</summary>
    public static string CharactersDirectory(string documentsDirectory) => Path.Combine(documentsDirectory, "Characters");
}
