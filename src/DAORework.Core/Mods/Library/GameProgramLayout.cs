using DAORework.Core.Common;

namespace DAORework.Core.Mods.Library;

/// <summary>
/// Which downloads are mods that run inside the game (ADR-0033 §1): no game data anywhere and, at the archive's root or
/// under one wrapper folder, either a <c>bin_ship</c> folder or a DLL. A mod with game data as well stays an override
/// mod, so a stray DLL in a texture mod is never offered. Pure.
/// </summary>
public static class GameProgramLayout
{
    private const string BinShip = GamePaths.BinShipDirectoryName;

    public static bool Matches(IReadOnlyList<string> files)
    {
        ArgumentNullException.ThrowIfNull(files);
        if (files.Count == 0 || files.Any(GameDataFiles.IsGameData))
        {
            return false;
        }

        var root = Root(files);
        var inRoot = files.Select(f => f[root.Length..]).ToList();
        return inRoot.Any(f => f.StartsWith(BinShip + "/", StringComparison.OrdinalIgnoreCase))
            || inRoot.Any(f => !f.Contains('/', StringComparison.Ordinal) && f.EndsWith(".dll", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Where each file goes (ADR-0033 §1). With a <c>bin_ship</c> folder, its files go to the same place in the game's
    /// and the rest stays in the library; without one, everything goes into <c>bin_ship</c> as packed.
    /// </summary>
    public static IReadOnlyList<GameProgramFile> Targets(IReadOnlyList<LibraryModFile> files)
    {
        ArgumentNullException.ThrowIfNull(files);
        if (files.Count == 0)
        {
            return [];
        }

        var root = Root([.. files.Select(f => f.RelativePath)]);
        var folder = root + BinShip + "/";
        var hasFolder = files.Any(f => f.RelativePath.StartsWith(folder, StringComparison.OrdinalIgnoreCase));
        var strip = hasFolder ? folder.Length : root.Length;
        return [.. files
            .Where(f => !hasFolder || f.RelativePath.StartsWith(folder, StringComparison.OrdinalIgnoreCase))
            .Select(f => new GameProgramFile(f, BinShip + "/" + f.RelativePath[strip..]))];
    }

    /// <summary>The one folder every file sits under, with its slash, or nothing. <c>bin_ship</c> is never a wrapper.</summary>
    private static string Root(IReadOnlyList<string> files)
    {
        var first = files[0].Split('/')[0];
        var wrapper = first + "/";
        return !first.Equals(BinShip, StringComparison.OrdinalIgnoreCase) && files.All(f => f.StartsWith(wrapper, StringComparison.OrdinalIgnoreCase))
            ? wrapper
            : string.Empty;
    }
}
