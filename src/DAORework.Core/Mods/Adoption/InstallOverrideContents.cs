namespace DAORework.Core.Mods.Adoption;

/// <summary>
/// What is in the install folder's override (ROADMAP P2.5-22): each file's path, and the same files by the folder they
/// sit in, with the loose ones last. DAORework reads this folder and never changes it.
/// </summary>
public sealed record InstallOverrideContents(IReadOnlyList<string> Paths, IReadOnlyList<InstallOverrideMod> Mods)
{
    public const string LooseFilesName = "Loose files";

    public static InstallOverrideContents Empty { get; } = new([], []);

    public static InstallOverrideContents From(IReadOnlyList<InstallOverrideFile> files)
    {
        ArgumentNullException.ThrowIfNull(files);
        var mods = files.Where(f => f.Path.Contains('/', StringComparison.Ordinal))
            .GroupBy(f => f.Path[..f.Path.IndexOf('/', StringComparison.Ordinal)], StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
            .Select(g => Mod(g.Key, [.. g]))
            .ToList();
        var loose = files.Where(f => !f.Path.Contains('/', StringComparison.Ordinal)).ToList();
        if (loose.Count > 0)
        {
            mods.Add(Mod(LooseFilesName, loose));
        }

        return new InstallOverrideContents([.. files.Select(f => f.Path)], mods);
    }

    private static InstallOverrideMod Mod(string name, List<InstallOverrideFile> files) => new(name, files.Count, files.Sum(f => f.SizeBytes));
}
