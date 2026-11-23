using System.Collections.Concurrent;
using DAORework.Application.Abstractions;
using DAORework.Core.Mods.Combining;
using DAORework.Core.Mods.Library;

namespace DAORework.Application.Mods.Combining;

/// <summary>
/// What each library mod adds to character creation (P2.10-03, ADR-0050 §4): each mod's <c>chargenmorphcfg.xml</c>,
/// counted against the game's own copy where it can be read (ADR-0025, amended 2026-11-22). A library mod never
/// changes, so its copy is read once; the game's copy is read once for each install folder.
/// </summary>
public sealed class CountChargenMorphAdds(IModLibrary library, IGameChargenMorphSource game)
{
    private readonly ConcurrentDictionary<string, ChargenMorphEntries?> _mods = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, ChargenMorphEntries?> _games = new(StringComparer.OrdinalIgnoreCase);

    /// <param name="mods">The library's mods.</param>
    /// <param name="installDirectory">The profile's install folder, or null when it has none.</param>
    /// <param name="cancellationToken">Stops the reads.</param>
    public async Task<ChargenMorphTally> ExecuteAsync(IReadOnlyList<LibraryMod> mods, string? installDirectory, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(mods);
        var copies = new Dictionary<string, ChargenMorphEntries>(StringComparer.Ordinal);
        foreach (var mod in mods)
        {
            if (await ReadModAsync(mod, cancellationToken) is { } entries)
            {
                copies[mod.Id] = entries;
            }
        }

        return new ChargenMorphTally(copies, copies.Count == 0 ? null : await ReadGameAsync(installDirectory, cancellationToken));
    }

    // A mod's last copy is the one it would deploy; one that can't be read or isn't a morph_config adds nothing countable.
    private async Task<ChargenMorphEntries?> ReadModAsync(LibraryMod mod, CancellationToken cancellationToken)
    {
        if (_mods.TryGetValue(mod.Id, out var known))
        {
            return known;
        }

        if (!CombinedFiles.ChargenMorphCopies([mod]).TryGetValue(out var files, out _) || files.Count == 0)
        {
            return _mods[mod.Id] = null;
        }

        var read = await library.ReadTextAsync(mod.Id, files[^1].Source.RelativePath, cancellationToken);
        return read.TryGetValue(out var text, out _)
            ? _mods[mod.Id] = ChargenMorphEntries.Read(text)
            : null; // Not remembered: the file may only have been in use.
    }

    private async Task<ChargenMorphEntries?> ReadGameAsync(string? installDirectory, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(installDirectory))
        {
            return null;
        }

        if (_games.TryGetValue(installDirectory, out var known))
        {
            return known;
        }

        var read = await game.ReadAsync(installDirectory, cancellationToken);
        return read.TryGetValue(out var text, out _)
            ? _games[installDirectory] = text is null ? null : ChargenMorphEntries.Read(text)
            : null; // Without the game's copy, mods are counted against what their copies share.
    }
}
