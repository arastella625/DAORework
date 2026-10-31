namespace DAORework.Core.Mods.Adoption;

/// <summary>The stable ids of adoption candidates (ADR-0015).</summary>
public static class AdoptionIds
{
    public const string LooseOverrideFiles = "override:(loose)";

    public static string AddIn(string uid) => "addin:" + uid;

    public static string OverrideFolder(string folder) => "override:" + folder;
}
