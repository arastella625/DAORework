namespace DAORework.Core.Mods.Adoption;

public enum AdoptionCandidateKind
{
    /// <summary>A registered add-in whose <c>AddIns\&lt;uid&gt;</c> folder is in the documents folder.</summary>
    AddIn,

    /// <summary>A top-level subfolder of <c>override</c> other than DAORework's own.</summary>
    OverrideFolder,

    /// <summary>The files directly in <c>override</c>, together, adopted as one mod the user names.</summary>
    LooseOverrideFiles,
}
