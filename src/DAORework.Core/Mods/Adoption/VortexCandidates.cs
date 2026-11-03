using DAORework.Core.Mods.Library;

namespace DAORework.Core.Mods.Adoption;

/// <summary>
/// The mods Vortex placed in <c>override</c>, by its records (ADR-0028 §2): each is the files its record lists that are
/// still there, wherever they sit, named as Vortex names it. Files in DAORework's own folder are never Vortex's.
/// </summary>
internal static class VortexCandidates
{
    public static (List<AdoptionCandidate> Candidates, HashSet<string> Claimed) Build(HandInstalledSnapshot snapshot, AdoptionCatalog known)
    {
        var present = OverrideFiles(snapshot);
        var candidates = new List<AdoptionCandidate>();
        var claimed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var record in snapshot.VortexRecords)
        {
            var byMod = record.Files
                .Where(f => present.ContainsKey(f.Path) && claimed.Add(f.Path))
                .GroupBy(f => f.Mod, StringComparer.OrdinalIgnoreCase);
            foreach (var mod in byMod)
            {
                var files = mod.Select(f => present[f.Path]).OrderBy(f => f.Path, StringComparer.Ordinal).ToList();
                candidates.Add(new AdoptionCandidate(
                    AdoptionIds.VortexMod(mod.Key),
                    AdoptionCandidateKind.VortexMod,
                    VortexModNames.Display(mod.Key),
                    ModLayout.OverridePrefix.TrimEnd('/'),
                    files)
                {
                    CatalogItem = known.ForOverrideFiles(files),
                    DeployedByVortex = record.IsLive(mod.Key),
                });
            }
        }

        return (candidates, claimed);
    }

    private static Dictionary<string, HandInstalledFile> OverrideFiles(HandInstalledSnapshot snapshot)
    {
        var owned = ModLayout.OverridePrefix + ModLayout.OwnedOverrideFolder + "/";
        return snapshot.OverrideFolders.Values.SelectMany(f => f)
            .Concat(snapshot.LooseOverrideFiles)
            .Where(f => !f.Path.StartsWith(owned, StringComparison.OrdinalIgnoreCase))
            .DistinctBy(f => f.Path, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(f => f.Path, StringComparer.OrdinalIgnoreCase);
    }
}
