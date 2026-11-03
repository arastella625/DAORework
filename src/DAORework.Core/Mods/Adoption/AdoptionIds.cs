namespace DAORework.Core.Mods.Adoption;

/// <summary>The stable ids of adoption candidates (ADR-0015).</summary>
public static class AdoptionIds
{
    public const string LooseOverrideFiles = "override:(loose)";

    public static string AddIn(string uid) => "addin:" + uid;

    public static string OverrideFolder(string folder) => "override:" + folder;

    /// <summary>A Vortex mod, by its whole folder name in Vortex's record (ADR-0028).</summary>
    public static string VortexMod(string name) => "vortex:" + name;
}
