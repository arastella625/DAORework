namespace DAORework.Core.Mods.Adoption;

public enum AdoptionExclusionReason
{
    /// <summary>BioWare's DLC, by the catalog's <c>Official</c> entries. Never adopted or touched.</summary>
    Official,

    /// <summary>A registered add-in with no folder in the documents folder: it is in the install folder, or missing.</summary>
    NoFolderInDocuments,

    /// <summary>An <c>AddIns</c> folder with no <c>AddInItem</c> in <c>Settings\AddIns.xml</c>, so the game does not load it.</summary>
    NotRegistered,
}
