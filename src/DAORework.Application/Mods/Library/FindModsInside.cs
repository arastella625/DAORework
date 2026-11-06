using System.Collections.Concurrent;
using DAORework.Application.Abstractions;
using DAORework.Core.Mods.Library;

namespace DAORework.Application.Mods.Library;

/// <summary>
/// Finds the mods whose files are all in a bigger one (ADR-0041), looking inside their archives. A library mod never
/// changes, so what an archive holds is read once, and a reload of the list doesn't touch the disk again.
/// </summary>
public sealed class FindModsInside
{
    private readonly IErfContentsReader _archives;
    private readonly ConcurrentDictionary<(string ModId, string Path), IReadOnlyList<string>> _read = new();

    public FindModsInside(IErfContentsReader archives)
    {
        _archives = archives;
    }

    public IReadOnlyDictionary<string, ModInside> Execute(IReadOnlyList<LibraryEntry> entries) => ModsInside.Find(entries, NamesIn);

    /// <summary>An archive that can't be read is tried again next time: it may only have been in use.</summary>
    private IReadOnlyList<string>? NamesIn(LibraryMod mod, LibraryModFile file)
    {
        var key = (mod.Id, file.RelativePath);
        if (_read.TryGetValue(key, out var known))
        {
            return known;
        }

        return _archives.ReadNames(mod.Id, file.RelativePath).TryGetValue(out var names, out _) ? _read[key] = names : null;
    }
}
