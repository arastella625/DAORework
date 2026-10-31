namespace DAORework.Core.Mods.Library;

public enum ArchiveShape
{
    /// <summary>A <c>.dazip</c> layout: <c>Manifest.xml</c> and <c>Contents/</c> at the root.</summary>
    AddIn,

    /// <summary>A download (a Nexus zip, say) whose only mod is one <c>.dazip</c> inside it.</summary>
    WrappedAddIn,

    /// <summary>Loose files for the override folder.</summary>
    Override,
}
