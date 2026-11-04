namespace DAORework.Core.Documents;

/// <summary>What the game's Proton prefix looks like from outside (ADR-0031). Pure.</summary>
public static class ProtonPaths
{
    /// <summary>The Steam app id of Dragon Age: Origins, which names its prefix.</summary>
    public const string AppId = "47810";

    /// <summary>Gets the folder below a Steam library that holds the prefix's drive.</summary>
    public static string PrefixInLibrary(string library) => Path.Combine(library, "steamapps", "compatdata", AppId, "pfx");

    /// <summary>Whether a path is inside the game's prefix, so losing the prefix loses it too.</summary>
    public static bool IsInPrefix(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        return path.Replace('\\', '/').Contains($"/compatdata/{AppId}/pfx/", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Whether an install folder is in a Steam library.</summary>
    public static bool IsSteamInstall(string installDirectory)
    {
        ArgumentNullException.ThrowIfNull(installDirectory);
        return installDirectory.Replace('\\', '/').Contains("/steamapps/common/", StringComparison.OrdinalIgnoreCase);
    }
}
