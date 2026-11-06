using DAORework.Core.Mods.Library;

namespace DAORework.Core.Mods.Conflicts;

/// <summary>
/// A file's name as a player would say it (P2.6-54): <c>alistair_main.dlg</c> is <i>Alistair's conversation</i>. Only
/// what the name makes plain is claimed; anything else keeps its file name. Pure.
/// </summary>
public static class SharedFileNames
{
    private static readonly HashSet<string> _companions = new(StringComparer.OrdinalIgnoreCase)
    {
        "alistair", "morrigan", "leliana", "sten", "zevran", "oghren", "wynne", "shale", "loghain",
    };

    /// <returns>The plain name, or null when the file's name doesn't say whose it is or its kind has no plain word.</returns>
    public static string? For(string fileName)
    {
        ArgumentNullException.ThrowIfNull(fileName);
        var owner = Path.GetFileNameWithoutExtension(fileName).Split('_', '-')[0];
        return _companions.Contains(owner) && Noun(fileName) is { } noun
            ? $"{char.ToUpperInvariant(owner[0])}{owner[1..].ToLowerInvariant()}'s {noun}"
            : null;
    }

    private static string? Noun(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".dlg" or ".cnv" => "conversation",
            ".ncs" => "script",
            ".plo" => "plot file",
            _ when GameDataFiles.Looks.Contains(extension) => "look",
            _ => null,
        };
    }
}
