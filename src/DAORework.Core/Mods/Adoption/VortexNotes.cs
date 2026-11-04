namespace DAORework.Core.Mods.Adoption;

/// <summary>
/// What the window and the command line say about a candidate Vortex has a hand in (ADR-0028 §2). The verb is the
/// caller's: the window says <i>managing</i> and the command line <i>adopting</i>.
/// </summary>
public static class VortexNotes
{
    public static IReadOnlyList<string> For(AdoptionCandidate candidate, string doing)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        ArgumentException.ThrowIfNullOrWhiteSpace(doing);
        var capital = char.ToUpperInvariant(doing[0]) + doing[1..];
        return candidate switch
        {
            { Kind: AdoptionCandidateKind.VortexMod, DeployedByVortex: true } =>
                ["Vortex still manages this mod and would put its files back if they were moved, so it can't be managed on its own. Move from Vortex brings it over (mods vortex on the command line)."],
            { Kind: AdoptionCandidateKind.VortexMod } =>
                [$"Vortex placed this mod but can no longer change it: the folder has moved since, or Vortex's own copy is gone. {capital} it here is safe. Stop managing Dragon Age in Vortex too, so it doesn't place mods again."],
            { DeployedByVortex: true } =>
                [$"Vortex manages files in {(candidate.Kind == AdoptionCandidateKind.AddIn ? "AddIns" : "override")}. Purge this mod in Vortex before {doing} it here, or Vortex may undo the move."],
            _ => [],
        };
    }
}
