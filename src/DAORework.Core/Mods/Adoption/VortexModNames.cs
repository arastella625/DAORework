using System.Text.RegularExpressions;

namespace DAORework.Core.Mods.Adoption;

/// <summary>
/// The name the window shows for a Vortex mod (ADR-0028 §2). Vortex names a mod's folder after its download, which
/// ends in the Nexus id and version (<c>Anto Hairstyles 2-3480-1-2-3</c>); the name is what comes before.
/// </summary>
public static partial class VortexModNames
{
    public static string Display(string folderName)
    {
        ArgumentNullException.ThrowIfNull(folderName);
        var match = Suffixed().Match(folderName);
        return match.Success ? match.Groups["name"].Value.TrimEnd() : folderName;
    }

    // A dash and the Nexus id, then any dash-separated parts that start with a digit or are empty (version, timestamp).
    [GeneratedRegex(@"^(?<name>.+?)\s*-\d+(?:-(?:\d[0-9A-Za-z]*)?)*$", RegexOptions.CultureInvariant, 1000)]
    private static partial Regex Suffixed();
}
