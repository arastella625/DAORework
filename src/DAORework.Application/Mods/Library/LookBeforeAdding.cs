using DAORework.Application.Abstractions;
using DAORework.Core.Mods.Adoption;
using DAORework.Core.Mods.Library;

namespace DAORework.Application.Mods.Library;

/// <summary>
/// Which of these downloads look like something already held (ADR-0039). Reads names only: nothing is extracted, hashed
/// or scanned. A download that can't be listed isn't reported, so it is added as usual.
/// </summary>
public sealed class LookBeforeAdding
{
    private readonly IDownloadsFolder _downloads;
    private readonly IModLibrary _library;

    public LookBeforeAdding(IDownloadsFolder downloads, IModLibrary library)
    {
        _downloads = downloads;
        _library = library;
    }

    /// <param name="paths">The downloads, or extracted folders, about to be added.</param>
    /// <param name="found">What was found in the game.</param>
    /// <param name="cancellationToken">Stops the look.</param>
    public async Task<IReadOnlyList<HeldDownload>> ExecuteAsync(IReadOnlyList<string> paths, IReadOnlyList<AdoptionCandidate> found, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(paths);
        ArgumentNullException.ThrowIfNull(found);
        var library = (await _library.ListAsync(cancellationToken)).Select(e => e.Mod).ToList();
        var held = new List<HeldDownload>();
        foreach (var path in paths)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_downloads.ReadEntryNames(path).TryGetValue(out var names, out _) && AlreadyHeld.Find(names, found, library) is { } match)
            {
                held.Add(new HeldDownload(path, ModIds.NameFromFileName(Path.GetFileNameWithoutExtension(path)), match));
            }
        }

        return held;
    }
}
