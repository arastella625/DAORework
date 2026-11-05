namespace DAORework.Core.Mods.Library;

/// <summary>
/// Files in a mod that aren't part of it as far as the game goes: what other tools leave behind (Vortex's folder
/// markers, a Mac's <c>.DS_Store</c>, <c>__MACOSX</c> and <c>._</c> files, Windows' <c>Thumbs.db</c>) and what is written
/// for people (readmes and other documents). Two mods sharing one of these changes nothing in the game. Pure.
/// </summary>
public static class NonModFiles
{
    private const string MacFolder = "__MACOSX";

    private static readonly HashSet<string> _names = new(StringComparer.OrdinalIgnoreCase)
    {
        "__folder_managed_by_vortex", ".DS_Store", "Thumbs.db", "desktop.ini",
    };

    private static readonly HashSet<string> _documents = new(StringComparer.OrdinalIgnoreCase)
    {
        ".txt", ".md", ".rtf", ".pdf", ".doc", ".docx", ".odt", ".nfo", ".htm", ".html", ".url",
    };

    /// <param name="path">A file's name, or its path inside a mod with <c>/</c> separators.</param>
    public static bool Is(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        var segments = path.Split('/');
        var name = segments[^1];
        return segments.Any(s => s.Equals(MacFolder, StringComparison.OrdinalIgnoreCase))
            || name.StartsWith("._", StringComparison.Ordinal)
            || _names.Contains(name)
            || _documents.Contains(Path.GetExtension(name));
    }
}
