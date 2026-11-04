using DAORework.Application.Abstractions;
using DAORework.Core.Mods.Conflicts;
using DAORework.Core.Mods.Library;
using DAORework.Core.Mods.Safety;

namespace DAORework.Application.Mods.Changes;

/// <summary>
/// Reads what the load order shows beside its plan (ROADMAP P3-07). An archive that can't be read is left out: it hides
/// its contents from this list and changes nothing else.
/// </summary>
public sealed class ReadLoadOrderDetails
{
    private readonly IModLibrary _library;
    private readonly IErfContentsReader _erf;

    public ReadLoadOrderDetails(IModLibrary library, IErfContentsReader erf)
    {
        _library = library;
        _erf = erf;
    }

    public async Task<LoadOrderDetails> ExecuteAsync(IReadOnlyList<string> activeModIds, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(activeModIds);
        var entries = await _library.ListAsync(cancellationToken);
        var active = activeModIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var archives = new List<PackedArchive>();
        var loose = new List<(string ModId, string FileName)>();
        foreach (var mod in entries.Select(e => e.Mod).Where(m => active.Contains(m.Id)))
        {
            cancellationToken.ThrowIfCancellationRequested();
            Sort(mod, archives, loose);
        }

        return new LoadOrderDetails(
            entries.ToDictionary(e => e.Mod.Id, e => e.Mod.Name, StringComparer.OrdinalIgnoreCase),
            PackedConflicts.Find(archives, loose));
    }

    private void Sort(LibraryMod mod, List<PackedArchive> archives, List<(string ModId, string FileName)> loose)
    {
        if (!ModLayout.Map(mod).TryGetValue(out var files, out _))
        {
            return;
        }

        foreach (var file in files.Where(f => f.IsOverride && !ProgramFiles.IsProgram(f.Source)))
        {
            if (!file.FileName.EndsWith(".erf", StringComparison.OrdinalIgnoreCase))
            {
                loose.Add((mod.Id, file.FileName));
            }
            else if (_erf.ReadNames(mod.Id, file.Source.RelativePath).TryGetValue(out var names, out _))
            {
                archives.Add(new PackedArchive(mod.Id, names));
            }
        }
    }
}
