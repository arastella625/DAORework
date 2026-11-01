namespace DAORework.Core.Mods.Conflicts;

/// <summary>
/// What it means that two mods ship an override file of the same name, by the file's kind (P2.5-21): the game uses
/// one copy, and whether that loses a look, a table's changes, a script's behaviour or a mod's text depends on what the
/// file is. Said after the shared-file warning; identical copies need no explanation. Pure.
/// </summary>
public static class SharedFileMeaning
{
    private static readonly HashSet<string> _looks = new(StringComparer.OrdinalIgnoreCase)
    {
        ".dds", ".tga", ".mmh", ".msh", ".mao", ".phy", ".mor", ".mop", ".tnt", ".gr2", ".ani", ".anb", ".fxe",
    };

    /// <returns>A sentence for the file's kind, or null for a kind DAORework says nothing particular about.</returns>
    public static string? For(string fileName)
    {
        ArgumentNullException.ThrowIfNull(fileName);
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".gda" => "It is a data table, so the other mod's changes to it are lost. Look for a compatibility patch; mods that add to a table with their own M2DA files do not clash.",
            ".ncs" => "It is a script, so what the other mod does with it is lost. Look for a compatibility patch.",
            ".tlk" => "It is a talk table, so the other mod's text is not shown.",
            ".erf" or ".rim" => "It is an archive: only one is loaded, and what is inside is not compared yet.",
            ".xml" => "It is a settings file the game reads one copy of.",
            _ when _looks.Contains(extension) => "It is a texture or model, so only that mod's look shows.",
            _ => null,
        };
    }
}
