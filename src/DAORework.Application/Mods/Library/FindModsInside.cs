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
    private readonly ConcurrentDictionary<(string ModId, string Path), IReadOnlyList<string>?> _read = new();

    public FindModsInside(IErfContentsReader archives)
    {
        _archives = archives;
    }

    public IReadOnlyDictionary<string, ModInside> Execute(IReadOnlyList<LibraryEntry> entries) =>
        ModsInside.Find(entries, (mod, file) => _read.GetOrAdd((mod.Id, file.RelativePath), key => _archives.ReadNames(key.ModId, key.Path).TryGetValue(out var names, out _) ? names : null));
}
