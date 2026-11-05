using System.Text.RegularExpressions;

namespace DAORework.Core.Mods.Library;

/// <summary>
/// A mod's name as the window shows it: folder names and download names read as words. Only the look changes, so the
/// library keeps the name and the row's details keep the download's own. Pure.
/// </summary>
public static class ModDisplayName
{
    private const string LooseFiles = "Loose files";

    /// <summary><c>rules_core</c> reads <i>Rules core</i>, <c>commoner--a little leg</c> reads <i>Commoner - a little leg</i>; a name that already has capitals keeps them.</summary>
    public static string For(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (name.Equals("loose", StringComparison.OrdinalIgnoreCase))
        {
            return LooseFiles;
        }

        var spaced = name.Replace('_', ' ').Replace("--", " - ", StringComparison.Ordinal);
        var words = Regex.Replace(spaced, " {2,}", " ", RegexOptions.None, TimeSpan.FromSeconds(1)).Trim();
        return words.Length == 0 ? name : char.ToUpperInvariant(words[0]) + words[1..];
    }
}
