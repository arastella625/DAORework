using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Mods.Library;
using DAORework.Core.Mods.Safety;

namespace DAORework.Core.Mods.Conflicts;

/// <summary>
/// Works out what activating an ordered list of mods deploys and where they overlap (ROADMAP P2-02, ADR-0012). The
/// game finds override files by name in any subfolder, so overlaps are by file name there: the mod later in the list
/// wins and only its copy is deployed. Outside override, two mods writing the same path is a collision. A program file
/// is never deployed (ADR-0018): it is left out before any of that and listed instead. Pure: the caller supplies the
/// library mods and the paths of the override files the user placed by hand.
/// </summary>
public static class ConflictScanner
{
    private static readonly StringComparer _names = StringComparer.OrdinalIgnoreCase;

    /// <param name="orderedMods">The mods to activate, lowest priority first.</param>
    /// <param name="unmanagedOverridePaths">Files under the override folder that DAORework did not place, any separators.</param>
    public static Result<ConflictReport> Scan(IReadOnlyList<LibraryMod> orderedMods, IReadOnlyList<string> unmanagedOverridePaths)
    {
        ArgumentNullException.ThrowIfNull(orderedMods);
        ArgumentNullException.ThrowIfNull(unmanagedOverridePaths);

        var duplicate = orderedMods.GroupBy(m => m.Id, _names).FirstOrDefault(g => g.Count() > 1);
        if (duplicate is not null)
        {
            return Result.Failure<ConflictReport>(new Error(DiagnosticCodes.ModListDuplicate, $"Mod '{duplicate.Key}' is in the list more than once."));
        }

        var mapped = new List<DeployedFile>();
        foreach (var mod in orderedMods)
        {
            if (!ModLayout.Map(mod).TryGetValue(out var files, out var error))
            {
                return Result.Failure<ConflictReport>(error);
            }

            mapped.AddRange(files);
        }

        var programs = mapped.Where(f => ProgramFiles.IsProgram(f.Source)).ToList();
        var data = mapped.Where(f => !ProgramFiles.IsProgram(f.Source)).ToList();
        return Result.Success(Report(data, unmanagedOverridePaths) with { ProgramFiles = programs });
    }

    private static ConflictReport Report(List<DeployedFile> mapped, IReadOnlyList<string> unmanagedOverridePaths)
    {
        var overrideConflicts = mapped
            .Where(f => f.IsOverride)
            .GroupBy(f => f.FileName, _names)
            .Where(g => g.Count() > 1)
            .Select(g => new OverrideConflict(g.Key, g.Last(), g.SkipLast(1).ToList()))
            .ToList();
        var losers = overrideConflicts.SelectMany(c => c.Overridden).ToHashSet();
        var deploy = mapped.Where(f => !losers.Contains(f)).ToList();

        var collisions = deploy
            .Where(f => !f.IsOverride)
            .GroupBy(f => f.TargetPath, _names)
            .Where(g => g.Select(f => f.ModId).Distinct(_names).Count() > 1)
            .Select(g => new PathCollision(g.Key, g.ToList()))
            .ToList();

        return new ConflictReport(deploy, overrideConflicts, collisions, Shadows(deploy, unmanagedOverridePaths), AddInFilesOverridden(deploy));
    }

    private static List<UnmanagedShadow> Shadows(List<DeployedFile> deploy, IReadOnlyList<string> unmanagedOverridePaths)
    {
        var unmanaged = unmanagedOverridePaths.ToLookup(p => p[(p.LastIndexOfAny(['/', '\\']) + 1)..], _names);
        return deploy
            .Where(f => f.IsOverride && unmanaged.Contains(f.FileName))
            .Select(f => new UnmanagedShadow(f, unmanaged[f.FileName].ToList()))
            .ToList();
    }

    private static List<AddInFileOverridden> AddInFilesOverridden(List<DeployedFile> deploy)
    {
        var addInPrefix = GamePaths.AddInsDirectoryName + "/";
        var addInFiles = deploy
            .Where(f => !f.IsOverride && f.TargetPath.StartsWith(addInPrefix, StringComparison.OrdinalIgnoreCase))
            .ToLookup(f => f.FileName, _names);
        return deploy
            .Where(f => f.IsOverride && addInFiles.Contains(f.FileName))
            .SelectMany(f => addInFiles[f.FileName].Select(a => new AddInFileOverridden(f, a)))
            .ToList();
    }
}
