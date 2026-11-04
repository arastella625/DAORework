namespace DAORework.Core.Mods.Library;

/// <summary>The file kinds the game reads from <c>override</c> (ADR-0033 §1). The one list of them lives here.</summary>
public static class GameDataFiles
{
    private static readonly HashSet<string> _others = new(StringComparer.OrdinalIgnoreCase) { ".gda", ".ncs", ".tlk", ".erf", ".rim" };

    /// <summary>Gets the extensions of textures, models and animations, whose copies differ only in how they look.</summary>
    public static IReadOnlySet<string> Looks { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ".dds", ".tga", ".mmh", ".msh", ".mao", ".phy", ".mor", ".mop", ".tnt", ".gr2", ".ani", ".anb", ".fxe",
    };

    public static bool IsGameData(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        var extension = Path.GetExtension(path);
        return Looks.Contains(extension) || _others.Contains(extension);
    }
}
